using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Tobii.G2OM;
using Tobii.XR;
using TMPro;
using UnityEngine.UIElements;

public class FocusibleText : MonoBehaviour, IGazeFocusable
{
    private TextMeshProUGUI textMeshProUGUI;
    int wordIndex = -1;
    int lineIndex = -1;

    private string logDirectory;
    private string absoluteGazeDataPath;

    private bool isLogging = false;
    private bool isAudioApplied = false;

    private StreamWriter writer;
    private float logWriteInterval = 0.5f;
    private float lastLogWriteTime = 0f;

    private List<(int, int)> trackingTargetIndices = new List<(int, int)>();
    private const int maxTrackingTargets = 5;
    private Dictionary<(int, int), float> gazeDurations = new Dictionary<(int, int), float>();
    private float maxFixationDuration = 1f;
    private float decayRate = 0.1f;


    public enum LogMode
    {
        None,
        Eye,
        Mouse
    }

    [SerializeField] private LogMode logMode = LogMode.None;

    void Awake()
    {
        textMeshProUGUI = GetComponent<TextMeshProUGUI>();
    }

    // Start is called before the first frame update
    void Start()
    {
        logDirectory = ProjectManager.Instance.GetGazeDataDirectory();
        Debug.Log($"Log Directory: {logDirectory}");
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!isLogging) return;

        var eyeTrackingData = TobiiXR.GetEyeTrackingData(TobiiXR_TrackingSpace.World);

        // GazeRay가 유효한지 확인
        if (eyeTrackingData.GazeRay.IsValid && logMode == LogMode.Eye)
        {
            // GazeRay의 원점(시선의 위치)과 방향 벡터 가져오기
            Vector3 gazePos = GetGazePosition(eyeTrackingData);
            lineIndex = TMP_TextUtilities.FindIntersectingLine(textMeshProUGUI, gazePos, null);
            wordIndex = TMP_TextUtilities.FindIntersectingWord(textMeshProUGUI, gazePos, null);
            if (lineIndex != -1 && wordIndex != -1)
            {
                int sentenceIndex = lineIndex + ProjectManager.Instance.GetCurrentPageStartSentenceIndex();
                Fix(sentenceIndex, wordIndex);
                LogEveryFrame(eyeTrackingData, gazePos, sentenceIndex, wordIndex);
            }
        }
        else if (logMode == LogMode.Mouse)
        {
            Vector3 mousePos = Input.mousePosition;
            lineIndex = TMP_TextUtilities.FindIntersectingLine(textMeshProUGUI, mousePos, Camera.main);
            wordIndex = TMP_TextUtilities.FindIntersectingWord(textMeshProUGUI, mousePos, Camera.main);
            if (lineIndex != -1 && wordIndex != -1)
            {
                Debug.Log(textMeshProUGUI.textInfo.wordInfo[wordIndex].GetWord());
                int sentenceIndex = lineIndex + ProjectManager.Instance.GetCurrentPageStartSentenceIndex();
                Fix(sentenceIndex, wordIndex);
            }
        }

        if (Time.time - lastLogWriteTime >= logWriteInterval)
        {
            writer?.Flush();
            SendGazeDataToProjectManager();
            lastLogWriteTime = Time.time;
        }
    }
    private Vector3 GetGazePosition(TobiiXR_EyeTrackingData eyeTrackingData)
    {
        var rayOrigin = eyeTrackingData.GazeRay.Origin;
        var rayDirection = eyeTrackingData.GazeRay.Direction;
/*        RectTransform rectTransform = textMeshProUGUI.rectTransform;
        Vector3 localPos = rectTransform.localPosition;
        Vector3 worldPos = rectTransform.TransformPoint(localPos);*/
        Vector3 worldPos = textMeshProUGUI.rectTransform.position;
        return rayOrigin + rayDirection * (worldPos.z - rayOrigin.z) / rayDirection.z;
    }

    public bool IsLogging()
    {
        return isLogging;
    }

    public void GazeFocusChanged(bool hasFocus)
    {
        if (hasFocus)
        {

        }
        else
        {

        }
    }

    private void Fix(int sIdx, int wIdx)
    {
        foreach (var trackedTargetIdx in trackingTargetIndices)
        {
            gazeDurations[trackedTargetIdx] = Mathf.Max(0, gazeDurations[trackedTargetIdx] - decayRate * Time.deltaTime);
        }

        if (sIdx < 0 || sIdx >= ProjectManager.Instance.GetSentencesLength() || wIdx < 0 || wIdx >= textMeshProUGUI.textInfo.wordCount)
            return;

        var curTargetIdx = (sIdx, wIdx);
        var nullIdx = (-1, -1);

        if (gazeDurations.ContainsKey(curTargetIdx))
        {
            gazeDurations[curTargetIdx] = Mathf.Min(gazeDurations[curTargetIdx] + Time.deltaTime, maxFixationDuration);
        }
        else
        {
            gazeDurations[curTargetIdx] = Time.deltaTime;
        }
        
        if (!trackingTargetIndices.Contains(curTargetIdx))
        {
            if (trackingTargetIndices.Count >= maxTrackingTargets)
            {
                var minFixationDurationTargetIdx = nullIdx;
                float minFixationDuration = float.MaxValue;

                foreach (var trackedTargetIdx in trackingTargetIndices)
                {
                    if (gazeDurations[trackedTargetIdx] < minFixationDuration)
                    {
                        minFixationDuration = gazeDurations[trackedTargetIdx];
                        minFixationDurationTargetIdx = trackedTargetIdx;
                    }
                }

                if (minFixationDurationTargetIdx != nullIdx && gazeDurations[curTargetIdx] > minFixationDuration)
                {
                    trackingTargetIndices.Remove(minFixationDurationTargetIdx);
                    gazeDurations[minFixationDurationTargetIdx] = 0f;

                    trackingTargetIndices.Add(curTargetIdx);
                }
            }
            else
            {
                trackingTargetIndices.Add(curTargetIdx);
            }
        }

        float threshold = ProjectManager.Instance.GetFixationThreshold(sIdx, wIdx);
        Debug.Log($"Threshold: {threshold}");
        if (gazeDurations.TryGetValue(curTargetIdx, out float gazeDuration) && gazeDuration > threshold)
        {
            if (threshold == -1)
            {
                return;
            }

            if (curTargetIdx.wIdx >= 0 && curTargetIdx.wIdx < textMeshProUGUI.textInfo.wordInfo.Length)
            {
                Debug.Log(textMeshProUGUI.textInfo.wordInfo[curTargetIdx.wIdx]);
                ProjectManager.Instance.PlayAudio(curTargetIdx.sIdx, curTargetIdx.wIdx);
            }
        }
    }


    // Eye Tracking Data
    public void StartLogging()
    {
        if (!isLogging)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string textFileName = ProjectManager.Instance.GetTextFileName();
            int user_id = ProjectManager.Instance.GetUserID();
            string fileName = $"{textFileName}_{user_id.ToString("D4")}_{timestamp}.csv";
            absoluteGazeDataPath = Path.Combine(logDirectory, fileName);

            writer = new StreamWriter(absoluteGazeDataPath, true);
            writer.WriteLine(
                "Timestamp," +
                "Convergence distance," +
                "Convergence distance validity," +
                "Gaze origin X,Gaze origin Y,Gaze origin Z," +
                "Gaze direction X,Gaze direction Y,Gaze direction Z," +
                "Left eye blink,Right eye blink," +
                "Sentence Index,Line Index,Word Index," +
                "Word," +
                "Word X,Word Y,Word Z," +
                "Gaze target X,Gaze target Y,Gaze target Z," +
                "Duration"
            );

            isLogging = true;
        }
    }

    public void LogEveryFrame(TobiiXR_EyeTrackingData eyeTrackingData, Vector3 gazeTargetWorldPos, int sentenceIndex, int wordIndex)
    {
        if (writer == null) return;

        //string timestamp = DateTime.Now.ToString("HH:mm:ss.ffff");
        var gazeRay = eyeTrackingData.GazeRay;
        TMP_TextInfo textInfo = textMeshProUGUI.textInfo;
        TMP_WordInfo wordInfo = textInfo.wordInfo[wordIndex];
        TMP_CharacterInfo firstCharInfo = textInfo.characterInfo[wordInfo.firstCharacterIndex];
        TMP_CharacterInfo lastCharInfo = textInfo.characterInfo[wordInfo.lastCharacterIndex];
        Vector3 wordWorldPos = (firstCharInfo.bottomLeft + lastCharInfo.topRight) / 2;
        Vector3 wordScreenPos = Camera.main.WorldToScreenPoint(wordWorldPos);
        Vector3 gazeTargetScreenPos = Camera.main.WorldToScreenPoint(gazeTargetWorldPos);

        writer.WriteLine(
            $"{eyeTrackingData.Timestamp}," +
            $"{eyeTrackingData.ConvergenceDistance}," +
            $"{eyeTrackingData.ConvergenceDistanceIsValid}," +
            $"{gazeRay.Origin.x},{gazeRay.Origin.y},{gazeRay.Origin.z}," +
            $"{gazeRay.Direction.x},{gazeRay.Direction.y},{gazeRay.Direction.z}," +
            $"{eyeTrackingData.IsLeftEyeBlinking},{eyeTrackingData.IsRightEyeBlinking}," +
            $"{sentenceIndex},{lineIndex},{wordIndex}," +
            $"{wordInfo.GetWord()}," +
            $"{wordScreenPos.x},{wordScreenPos.y},{wordScreenPos.z}," +
            $"{gazeTargetScreenPos.x},{gazeTargetScreenPos.y},{gazeTargetScreenPos.z},"+
            $"{Time.deltaTime}"
        );
    }

    public void StopLogging()
    {
        writer?.Close();
        writer = null;
        isLogging = false;
    }

    public void SetAudioApplied(bool value)
    {
        isAudioApplied = value;
    }
    private void ApplyAudio(int sentenceIndex, int wordIndex)
    {
        ProjectManager.Instance.PlayAudio(sentenceIndex, wordIndex);
    }

    public int GetWordIndex()
    {
        return wordIndex;
    }

    private void SendGazeDataToProjectManager()
    {
        int sentenceIndex = lineIndex + ProjectManager.Instance.GetCurrentPageStartSentenceIndex();
        float duration = gazeDurations.TryGetValue((sentenceIndex, wordIndex), out float d) ? d : 0f;
        string sentence = ProjectManager.Instance.GetSentenceFromIndex(sentenceIndex);

        ProjectManager.Instance.SendGazeData(sentenceIndex, wordIndex, duration, sentence);
    }

    private void OnApplicationQuit()
    {
        if (writer != null)
        {
            writer.Close();
        }
    }
    
    void OnDestroy()
    {

    }

}
