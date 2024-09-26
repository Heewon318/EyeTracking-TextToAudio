using UnityEngine;

public class TMPPropertySlider : MonoBehaviour
{
    [SerializeField] private Tobii.XR.UITriggerGazeSlider slider;
    [SerializeField] private Tobii.XR.UIGazeSliderGraphics sliderGraphics;

    public enum TMPProperty
    {
        CharacterSpacing,
        LineSpacing,
        FontSize,
        CharactersPerPage
    }
    [SerializeField]
    private TMPProperty property;
    [SerializeField] int minValue = 0;
    [SerializeField] int maxValue = 1;

    private void Start()
    {
    }

    private void OnEnable()
    {
        if (slider != null && sliderGraphics != null)
        {
            slider.OnSliderValueChanged.AddListener(HandleSliderValueChanged);
            float propertyValue = ProjectManager.Instance.GetPropertyValue(property);
            float newValue = (propertyValue - minValue) / (maxValue - minValue);
            sliderGraphics.SetFillAmount(newValue);
        }
    }

    private void OnDisable()
    {
        if (slider != null)
        {
            slider.OnSliderValueChanged.RemoveListener(HandleSliderValueChanged);
        }
    }

    private void HandleSliderValueChanged(GameObject sliderObject, int value)
    {
        float newValue = Mathf.Lerp(minValue, maxValue, (float)value/100);
        ProjectManager.Instance.ChangeTextProperty(property, newValue);
    }
}
