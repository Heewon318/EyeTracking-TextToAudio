using UnityEngine;
using UnityEngine.UI;
using Tobii.XR;

public class ButtonHandler : MonoBehaviour
{
    [SerializeField] private UITriggerGazeButton cleanBtn;
    [SerializeField] private UITriggerGazeButton viewBtn;
    [SerializeField] private UITriggerGazeButton genBtn;
    [SerializeField] private UITriggerGazeButton applyBtn;
    
    [SerializeField] private Text text;
    private ProjectManager projectManager;

    public float minimumClickInterval = 1f;
    private float lastClickTime;

    // Start is called before the first frame update
    void Start()
    {
        try
        {
            //btn = this.transform.GetComponentInChildren<Button>();
            //text = this.transform.GetComponentInChildren<Text>();
            projectManager = ProjectManager.Instance;
        }
        catch (System.Exception e) 
        {
            Debug.LogError(e.Message);
        }

        if (cleanBtn != null)
            cleanBtn.OnButtonClicked.AddListener(OnCleanButtonClicked);
        if (viewBtn != null)
            viewBtn.OnButtonClicked.AddListener(OnViewButtonClicked);
        if (genBtn != null) 
            genBtn.OnButtonClicked.AddListener(OnGenerateButtonClicked);
        if (applyBtn != null) 
            applyBtn.OnButtonClicked.AddListener(OnApplyButtonClicked);
    }

    private void OnCleanButtonClicked(GameObject button)
    {
        if (Time.time - lastClickTime < minimumClickInterval)
            return;
        lastClickTime = Time.time;
        Debug.Log("[ButtonHandler] CleanButton Clicked");


        string fullFileName = text.text;
        projectManager.RequestText(fullFileName);
    }
    
    private void OnViewButtonClicked(GameObject button)
    {
        if (Time.time - lastClickTime < minimumClickInterval)
            return;
        lastClickTime = Time.time;
        Debug.Log("[ButtonHandler] ViewButton Clicked");

        string fullFileName = text.text;
        projectManager.LoadText(fullFileName);
    }

    private void OnGenerateButtonClicked(GameObject button)
    {
        if (Time.time - lastClickTime < minimumClickInterval)
            return;
        lastClickTime = Time.time;
        Debug.Log("[ButtonHandler] GenerateButton Clicked");

        string fullFileName = text.text;
        projectManager.GenerateAudio(fullFileName);
    }

    private void OnApplyButtonClicked(GameObject button)
    {
        if (Time.time - lastClickTime < minimumClickInterval)
            return;
        lastClickTime = Time.time;
        Debug.Log("[ButtonHandler] ApplyButton Clicked");

        projectManager.ApplyAudio(text.text);
    }
}
