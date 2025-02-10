using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Tobii.XR;
using System.Collections.Generic;

public class ProjectManager : MonoBehaviour
{
    private static ProjectManager _instance = null;
    public static ProjectManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ProjectManager>();

                if (_instance == null)
                {
                    GameObject go = new GameObject("ProjectManager");
                    _instance = go.AddComponent<ProjectManager>();
                }
            }
            return _instance;
        }
    }

    [Header("References")]
    public Requester requester;
    public FileReader fileReader;
    public AudioManager audioManager;
    public FocusibleText focusibleText;
    public Text debuggingText;

    [Header("Path")]
    private string absoluteBaseDirectoryPath = Application.dataPath;
    private const string rawTextDirectory = "Monitering;";
    private const string textDirectory = "Texts";
    private const string audioDirectory = "StreamingAssets";
    private const string gazeDataDirectory = "GazeDatas";
    private string fullFileName = "";

    [Header("Option Buttons")]
    public UITriggerGazeButton initSettingButton;
    public UITriggerGazeButton setUserIdButton;

    [Header("Temp")]
    [SerializeField] private string fileNameWithExtension = "TheEgg.txt";
    [SerializeField] private Dictionary<(int, int), float> fixationThreshold = new Dictionary<(int, int), float>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        if (requester == null)
            Debug.LogWarning("[ProjectManager] Not requester");
        else if (fileReader == null)
            Debug.LogWarning("[ProjectManager] Not file reader");
        else if (audioManager == null)
            Debug.LogWarning("[ProjectManager] Not audio manager");
        else if (focusibleText == null)
            Debug.LogWarning("[ProjectManager] Not focisible text");
        else if (debuggingText == null)
            Debug.LogWarning("[ProjectManager] Not Debugging Text");


    }

    // Start is called before the first frame update
    void Start()
    {
        if (initSettingButton != null && setUserIdButton != null)
        {
            initSettingButton.OnButtonClicked.AddListener(InitSetting);
            setUserIdButton.OnButtonClicked.AddListener(SetUserId);
        }
        else
        {
            Debug.LogError("Init setting button is not assigned.");
        }

        InitSetting();
    }

    // Update is called once per frame
    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            RequestText(fileNameWithExtension);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            LoadText(fileNameWithExtension);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            GenerateAudio(fileNameWithExtension);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4)) 
        {
            ApplyAudio(fileNameWithExtension);
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            InitSetting();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            fileReader.ShowPreviousPage();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            fileReader.ShowNextPage();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            QuitGame();
        }


        if (ControllerManager.Instance.GetButtonPressDown(ControllerButton.Touchpad))
        {
            Debug.Log("[ProjectManager] Touchpad pressed.");
            Vector2 touchpadAxis = ControllerManager.Instance.GetTouchpadAxis();
            if (touchpadAxis.x < 0.3f)
                fileReader.ShowPreviousPage();
            else if (touchpadAxis.x > 0.7f)
                fileReader.ShowNextPage();
            audioManager.PlayTest(fileReader.GetCurrentPageStartSentenceIndex(), -2);
        }
        else if (ControllerManager.Instance.GetButtonPressDown(ControllerButton.Menu) || Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("[ProjectManager] Menu button pressed.");
            if (focusibleText.IsLogging())
            {
                focusibleText.StopLogging();
                Debug.Log("Logging stopped!");
            }
            else
            {
                focusibleText.StartLogging();
                Debug.Log("Logging started!");
            }
        }

        //HandleMovement();
    }

    public void ChangeTextProperty(TMPPropertySlider.TMPProperty option, float value)
    {
        fileReader.ChangeProperty(option, value);
    }
    public float GetPropertyValue(TMPPropertySlider.TMPProperty option)
    {
        return fileReader.GetPropertyValue(option);
    }

    public string GetTextFileName()
    {
        return Path.GetFileNameWithoutExtension(fullFileName);
    }

    public string GetGazeDataDirectory()
    {
        return Path.Combine(absoluteBaseDirectoryPath, gazeDataDirectory);
    }

    public int GetCurrentPageStartSentenceIndex()
    {
        return fileReader.GetCurrentPageStartSentenceIndex();
    }

    public int GetSentencesLength()
    {
        return fileReader.GetSentencesLength();
    }

    public float GetFixationThreshold(int sIdx, int wIdx)
    {
        if (fixationThreshold.TryGetValue((sIdx, wIdx), out float value))  
            return value;
        return -1;
    }

    public async void RequestText(string fullFileName)
    {
        string request = "VIEW:" + fullFileName;

        try
        {
            debuggingText.text = $"Waiting for text processing: '{fullFileName}'";
            string absoluteFilePath = await requester.RequestPathAsync(request);
            debuggingText.text = $"Text processing completed for: '{fullFileName}'";
        }
        catch (Exception ex)
        {
            debuggingText.text = $"Error processing text: {ex.Message}";
            Debug.LogError($"Exception occurred: {ex.Message}");
        }
    }

    public void LoadText(string fullFileName)
    {
        this.fullFileName = fullFileName;
        string relativeFilePath = Path.Combine(textDirectory, fullFileName);
        string absoluteFilePath = Path.Combine(absoluteBaseDirectoryPath, relativeFilePath);
        string debuggingMessage;

        if (string.IsNullOrEmpty(fullFileName))
        {
            debuggingMessage = "[ProjectManager.LoadText] The file_name is null or empty. Cannot load text.";
            Debug.LogWarning(debuggingMessage);
            debuggingText.text = debuggingMessage;
            return;
        }

        if (!File.Exists(absoluteFilePath))
        {
            debuggingMessage = $"[ProjectManager.LoadText] The file does not exist at path: {absoluteFilePath}";
            Debug.LogWarning(debuggingMessage);
            return;
        }

        debuggingMessage = $"[ProjectManager.LoadText] Attempting to load text from path: {absoluteFilePath}";
        Debug.Log(debuggingMessage);
        debuggingText.text = debuggingMessage;
        try
        {
            fileReader.LoadText(absoluteFilePath);



            string filePath = $"Assets/GazeDatas/{Path.GetFileNameWithoutExtension(fullFileName)}_threshold.csv";
            using (StreamWriter writer = new StreamWriter(filePath, false))
            {
                writer.WriteLine("SentenceIndex,WordIndex,Token,Threshold");
                for (int i = 0; i < fileReader.GetSentencesLength(); i++)
                {
                    string sentence = fileReader.GetSentence(i);
                    string[] words = sentence.Split(' ');
                    for (int j = 0; j < words.Length; j++)
                    {
                        fixationThreshold[(i, j)] = Mathf.Clamp01((float)words[j].Length / 10f);
                        writer.WriteLine($"{i},{j},{words[j]},{fixationThreshold[(i, j)]}");
                    }
                }
            }



            debuggingMessage = $"[ProjectManager.LoadText] Successfully initiated text loading for path: {absoluteFilePath}";
            Debug.Log(debuggingMessage);
            debuggingText.text = debuggingMessage;
        }
        catch (Exception ex)
        {
            debuggingMessage = $"[ProjectManager.LoadText] Failed to load text from path: {absoluteFilePath}. Error: {ex.Message}";
            Debug.LogError(debuggingMessage);
            debuggingText.text = debuggingMessage;
        }

    }

    public async void GenerateAudio(string fullFileName)
    {
        string request = "GENERATE:" + fullFileName;
        var response = await requester.RequestPathAsync(request);
        Debug.Log($"[ProjectManager.GenerateAudio] Response by GenerateAudio: {response}");
    }

    public async void ApplyAudio(string fullFileName)
    {
        string audioListTextFileName = Path.GetFileNameWithoutExtension(fullFileName);
        string audioListTextFilePath = Path.Combine(textDirectory, $"{audioListTextFileName}_audio.txt");
        string absoluteFilePath = Path.Combine(absoluteBaseDirectoryPath, audioListTextFilePath); 
        if (File.Exists(absoluteFilePath))
        {
            bool result = await audioManager.ApplyAudioAsync(absoluteFilePath);
            if (result)
            {
                Debug.Log($"[ProjectManager.ApplyAudio] Successfully initiated audio Applying for path: {absoluteFilePath}");
                focusibleText.SetAudioApplied(true);
                Debug.Log(audioManager.GetSoundDictionaryCount().ToString());
            }
            else
                Debug.LogError("[ProjectManager.ApplyAudio] Failed audio applying");
        }
        else
        {
            Debug.LogWarning($"[ProjectManager.ApplyAudio] The file does not exist at path: {absoluteFilePath}");
        }
    }

    // 오디오를 저장하거나 불러올 때 문장번호, 단어 번호가 제대로 매겨지고 있는가?
    // 시간 간격없이 계속해서 오디오 요청을 보내는 문제
    // 로그 기능을 켜야지 오디오 요청이 보내지는 것도 어색하지 않은가?


    public void PlayAudio(int sentenceIndex, int wordIndex)
    {
        UnityEngine.Debug.Log($"[ProjectManager] PlayAudio called with sentenceIndex: {sentenceIndex}, wordIndex: {wordIndex}");
        audioManager.PlayTest(sentenceIndex, wordIndex);
    }


    [Header("Transform References")]
    public Transform playerTransform;
    public Transform screenTransform;

    [Header("Screen Settings")]
    public Vector3 positionOffset = new Vector3(0, -0.5f, -2f);
    public Vector3 rotationOffset = new Vector3(5f, 0f, 0f);

    public float fixedXRotation = 10f;
    public float fixedZRotation = 0f;

    public void InitSetting(GameObject button=null)
    {
        Quaternion desiredRotation = Quaternion.Euler(rotationOffset) * playerTransform.rotation;
        screenTransform.rotation = Quaternion.Euler(fixedXRotation, desiredRotation.eulerAngles.y, fixedZRotation);

        Vector3 desiredPosition = playerTransform.position + playerTransform.TransformDirection(positionOffset);
        screenTransform.position = new Vector3(0, playerTransform.position.y + positionOffset.y, desiredPosition.z);

        fileReader.InitTextSetting();
    }

    private float moveSpeed = 1f;
    private void HandleMovement()
    {
        Vector3 moveDirection = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            moveDirection.y += 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            moveDirection.y -= 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            moveDirection.x += 1;
        }
        if (Input.GetKey(KeyCode.A))
        {
            moveDirection.x -= 1;
        }

        moveDirection = moveDirection.normalized * moveSpeed * Time.deltaTime;

        Transform canvasTransform = focusibleText.GetComponentInParent<Canvas>().transform;
        canvasTransform.Translate(moveDirection);
    }


    private int userId = 0;
    private void SetUserId(GameObject button=null)
    {
        //userId = new System.Random().Next(1000, 10000);
        userId += 1;
        if (userId > 9999)
        {
            userId = 1;
        }

        Text buttonText = setUserIdButton.GetComponentInChildren<Text>();
        buttonText.text = userId.ToString("D4");
    }
    public int GetUserID()
    {
        return userId;
    }

    public string GetSentenceFromIndex(int index)
    {
        return fileReader.GetSentence(index);
    }

    public async void SendGazeData(int sentenceIndex, int wordIndex, float duration, string sentence)
    {
        if (requester == null) return;

        string request = $"GAZE:{sentenceIndex},{wordIndex},{duration},{sentence}";
        await requester.SendGazeDataAsync(request);
    }


    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
