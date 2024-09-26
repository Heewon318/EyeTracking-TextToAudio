using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using Unity.VisualScripting;


public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private List<EventInstance> eventInstances;
    private EventInstance sfxEventInstance;
    private EventInstance bgmEventInstance;

    private List<AudioClip> audioClips = new List<AudioClip>();

    public UnityEvent OnSoundComplete;
    
    private bool isPlaying = false;
    private string audioFilePath;

    public float minVolume = 0f;
    public float maxVolume = 1f;

    public string mixerParameter0;
    public string mixerParameter1;
    public string mixerParameter2;
    public string mixerParameter3;
    public string exposedParameterName = "TotalVolume";
    

    private float[] allWeights;
    private int[] sentencesTracked = new int[4];
    private float[] volumes = new float[4];


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }

        //eventInstances = new List<EventInstance>();


    }

    // Start is called before the first frame update
    void Start()
    {
        allWeights = new float[audioClips.Count];
        for (int i = 0; i < audioClips.Count; i++)
            allWeights[i] = 0;

        for (int i = 0; i < 4; i++)
        {
            volumes[i] = 0;
            sentencesTracked[i] = -1;
        }

        //InitializeSFX(FMODEvents.instance.sfx);
        //InitializeTest();
        //InitializeBGM(FMODEvents.instance.bgm);
    }

    // Update is called once per frame
    void Update()
    {

    }

/*    /// <summary>
    /// Fmod default methods below
    /// </summary>
    /// <param name="sfxEventReference"></param>
    private void InitializeSFX(EventReference sfxEventReference)
    {
        sfxEventInstance = CreateInstance(sfxEventReference);
    }

    private void InitializeBGM(EventReference bgmEventReference)
    {
        bgmEventInstance = CreateInstance(bgmEventReference);
    }

    public void SetSFXNumber(int number)
    {
        sfxEventInstance.setParameterByName("wordNumber", number);
    }

    public void PlaySFX()
    {
        sfxEventInstance.start();
    }

    public EventInstance CreateInstance(EventReference eventReference)
    {
        EventInstance eventInstance = RuntimeManager.CreateInstance(eventReference);
        eventInstances.Add(eventInstance);
        return eventInstance;
    }

    private void CleanUp()
    {
        foreach (EventInstance eventInstance in eventInstances)
        {
            eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            eventInstance.release();
        }
    }*/

    /// <summary>
    /// FMOD test scripts below
    /// </summary>
    public string eventPath = "event:/Test";
    private EventInstance eventInstance;
    private Dictionary<(int sentenceIndex, int wordIndex), FMOD.Sound> soundDictionary = new Dictionary<(int, int), FMOD.Sound>();
    private FMOD.Channel channel;
    private ChannelGroup channelGroup;
    private FMOD.VECTOR position;
    private FMOD.VECTOR velocity;
    public async Task<bool> ApplyAudioAsync(string absoluteFilePath)
    {
        try
        {
            eventInstance = RuntimeManager.CreateInstance(eventPath);
            FMOD.System system = RuntimeManager.CoreSystem;
            var soundTasks = new List<Task>();

            using (StreamReader sr = new StreamReader(absoluteFilePath))
            {
                string line;
                while ((line = await sr.ReadLineAsync()) != null)
                {
                    var parts = line.Split(',');
                    if (parts.Length == 3 &&
                        int.TryParse(parts[0], out int sentenceIndex) &&
                        int.TryParse(parts[1], out int wordIndex) &&
                        File.Exists(parts[2]))
                    {
                        var key = (sentenceIndex, wordIndex);
                        if (!soundDictionary.ContainsKey(key))
                        {
                            soundTasks.Add(LoadSoundAsync(system, parts[2], key));
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning($"[AudioManager.ApplyAudio] Sound already exists for sentence {sentenceIndex} and word {wordIndex}.");
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"[AudioManager.ApplyAudio] Invalid line foramt or file not found: {line}");
                    }
                }
            }

            await Task.WhenAll(soundTasks);

            //Setup3DAttributes(Vector3.zero);

            return true;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[AudioManager.ApplyAudio] An exception occurred in ApplyAudio: {ex.Message}");
            return false;
        }
    }

    private async Task LoadSoundAsync(FMOD.System system, string soundPath, (int sentenceIndex, int wordIndex) key)
    {
        await Task.Run(() =>
        {
            // Test
            int randNum = (key.sentenceIndex + key.wordIndex) % 4 + 1;
            soundPath = $"Assets/StreamingAssets/Test{randNum}.wav";
            // Test
            var result = system.createSound(soundPath, FMOD.MODE._3D, out FMOD.Sound sound);
            if (result == FMOD.RESULT.OK)
            {
                lock (soundDictionary)
                {
                    if (!soundDictionary.ContainsKey(key))
                    {
                        soundDictionary[key] = sound;
                        UnityEngine.Debug.Log($"[AudioManager.LoadSound] Sound loaded and added to dictionary: {soundPath}");
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"[AudioManager.LoadSound] Sound already exists for sentence {key.sentenceIndex} and word {key.wordIndex}.");
                    }
                }
            }
            else
            {
                UnityEngine.Debug.LogError($"Failed to load sound from {soundPath}: {result}");
            }
        });
    }

    private void Setup3DAttributes(Vector3 initialPosition)
    {
        eventInstance.getChannelGroup(out channelGroup);
        FMOD.ATTRIBUTES_3D attributes = new FMOD.ATTRIBUTES_3D()
        {
            position = ConvertVector(initialPosition),
            forward = ConvertVector(Vector3.forward),
            up = ConvertVector(Vector3.up)
        };
        eventInstance.set3DAttributes(attributes);
        SetPosition(initialPosition);
    }

    public void SetPosition(Vector3 newPosition)
    {
        position = ConvertVector(new Vector3(newPosition.x, 0, newPosition.y - 10));
    }

    public void PlayTest(int sentenceIndex, int wordIndex)
    {
        if (isPlaying)
        {
            UnityEngine.Debug.Log($"[AudioManager] Ignoring PlayTest request: audio already playing");
            return;
        }

        if (soundDictionary.TryGetValue((sentenceIndex, wordIndex), out FMOD.Sound sound))
        {
            var result = RuntimeManager.CoreSystem.playSound(sound, channelGroup, true, out channel);
            if (result == FMOD.RESULT.OK)
            {
                channel.set3DAttributes(ref position, ref velocity);
                channel.setPaused(false);
                isPlaying = true;

                channel.getPaused(out bool paused);
                if (paused)
                {
                    channel.setPaused(false);
                }

                UnityEngine.Debug.Log($"[AudioManager.PlayTest] Successfully played sound for sentence {sentenceIndex} and word {wordIndex}");
                StartCoroutine(WaitForSoundToFinish());
            }
            else
            {
                UnityEngine.Debug.LogError($"[AudioManager.PlayTest] Failed to play sound: {result}");
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning($"[AudioManager.PlayTest] Sound not found for sentence {sentenceIndex} and word {wordIndex}.");
        }
    }

    private System.Collections.IEnumerator WaitForSoundToFinish()
    {
        while (true)
        {
            if (!channel.IsUnityNull() && channel.isPlaying(out bool playing) == FMOD.RESULT.OK && !playing)
            {
                isPlaying = false;
                break;
            }
            yield return null;
        }
    }

    public int GetSoundDictionaryCount()
    {
        return soundDictionary.Count;
    }

    private void CleanUpTest()
    {
        foreach (var sound in soundDictionary.Values)
        {
            sound.release();
        }
        
        eventInstance.release();
        soundDictionary.Clear();
    }

    private void OnDestroy()
    {
        //CleanUp();

        CleanUpTest();
    }

    private FMOD.VECTOR ConvertVector(Vector3 fromVector)
    {
        return new FMOD.VECTOR
        {
            x = fromVector.x,
            y = fromVector.y,
            z = fromVector.z
        };
    }
}



