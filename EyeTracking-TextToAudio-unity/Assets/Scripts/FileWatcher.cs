using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Tobii.XR;

public class FileWatcher : MonoBehaviour
{
    public string folderPath = "Assets/Monitering";
    public GameObject itemPrefab;
    public Transform content;

    private FileSystemWatcher fileSystemWatcher;

    //public Button refreshButton;
    [SerializeField] private UITriggerGazeButton refreshButton;

    // Start is called before the first frame update
    void Start()
    {
        if (refreshButton != null)
        {
            //refreshButton.onClick.AddListener(LoadFiles);
            refreshButton.OnButtonClicked.AddListener(LoadFiles);
        }

        InitializeFileSystemWatcher();
        LoadFiles(refreshButton.gameObject);
    }

    void InitializeFileSystemWatcher()
    {
        string fullPath = Path.GetFullPath(folderPath);

        fileSystemWatcher = new FileSystemWatcher
        {
            Path = fullPath,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };

        fileSystemWatcher.Created += OnChanged;
        fileSystemWatcher.Changed += OnChanged;
        fileSystemWatcher.Deleted += OnChanged;
        fileSystemWatcher.Renamed += OnRenamed;
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        Debug.Log($"File: {e.FullPath} {e.ChangeType}");
        UnityMainThreadDispatcher.Instance().Enqueue(() => 
        {
            Debug.Log("Enqueueing LoadFiles");
            LoadFiles(refreshButton.gameObject);
        });
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        Debug.Log($"File: {e.OldFullPath} renamed to {e.FullPath}");
        UnityMainThreadDispatcher.Instance().Enqueue(() => LoadFiles(refreshButton.gameObject));
    }

    void LoadFiles(GameObject button)
    {
        Debug.Log("LoadFiles method called");
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        if (Directory.Exists(folderPath))
        {
            string[] files = Directory.GetFiles(folderPath, "*.txt");

            foreach (string file in files)
            {
                GameObject item = Instantiate(itemPrefab, content);
                Text itemText = item.GetComponentInChildren<Text>();
                if (itemText != null)
                {
                    itemText.text = Path.GetFileName(file);

                    //string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                    //itemText.text = fileNameWithoutExtension;
                }
            }
        }
        else
        {
            Debug.Log("폴더가 존재하지 않습니다: " + folderPath);
        }
    }

    private void OnDestroy()
    {
        if (fileSystemWatcher != null)
        {
            fileSystemWatcher.Created -= OnChanged;
            fileSystemWatcher.Changed -= OnChanged;
            fileSystemWatcher.Deleted -= OnChanged;
            fileSystemWatcher.Renamed -= OnRenamed;
            fileSystemWatcher.Dispose();
        }
    }
}
