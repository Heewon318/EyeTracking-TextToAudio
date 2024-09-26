using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;

public class FileReader : MonoBehaviour
{
    private List<string> words;
    private List<string> sentences;
    private List<string> soundPaths;
    private int currentPage = 0;
    private int totalCharacters = 0;
    public Text pageText;

    public TextMeshProUGUI textMesh;
    [SerializeField] private int fontSize = 60;
    [SerializeField] private int charactersPerPageDefault = 100;
    [SerializeField] private int charactersPerPage;
    [SerializeField] private float lineSpacing = 1.5f;
    [SerializeField] private float characterSpacing = 1.5f;

    private string[] currentWords;
    private Vector3[] wordPositions;
    public int wordIndex = 0;
    private Camera mainCamera; // 메인 카메라

    private int intValue;

    private int currentPageStartSentenceIndex;
    static public string originalText;

    void Start()
    {
        mainCamera = Camera.main;

        textMesh.fontSize = fontSize;
        textMesh.lineSpacing = lineSpacing;
        textMesh.characterSpacing = characterSpacing;
        charactersPerPage = charactersPerPageDefault;


        pageText.text = currentPage.ToString();
    }

    private void Update()
    {

    }

/*    public void ChangeTextLength(int newCharactersPerPage)
    {
        *//*        int newCharactersPerPage;
                if (int.TryParse(inputField.text, out newCharactersPerPage))
                {
                    charactersPerPage = newCharactersPerPage;
                    ShowCurrentPage();
                }*//*
        charactersPerPage = newCharactersPerPage;
        ShowCurrentPage();
    }
*/
    public void ChangeProperty(TMPPropertySlider.TMPProperty propertyName, float value)
    {
        switch (propertyName)
        {
            case TMPPropertySlider.TMPProperty.CharacterSpacing:
                textMesh.characterSpacing = value;
                break;
            case TMPPropertySlider.TMPProperty.LineSpacing:
                textMesh.lineSpacing = value;
                break;
            case TMPPropertySlider.TMPProperty.FontSize:
                textMesh.fontSize = (int)value;
                break;
            case TMPPropertySlider.TMPProperty.CharactersPerPage:
                charactersPerPage = (int)value;
                ShowCurrentPage();
                break;
            default:
                break;
        }
    }
    public float GetPropertyValue(TMPPropertySlider.TMPProperty propertyName)
    {
        switch (propertyName)
        {
            case TMPPropertySlider.TMPProperty.CharacterSpacing:
                return textMesh.characterSpacing;
            case TMPPropertySlider.TMPProperty.LineSpacing:
                return textMesh.lineSpacing;
            case TMPPropertySlider.TMPProperty.FontSize:
                return (float)textMesh.fontSize;
            case TMPPropertySlider.TMPProperty.CharactersPerPage:
                return (float)charactersPerPage;
            default:
                throw new ArgumentOutOfRangeException(nameof(propertyName), $"Unhandled property: {propertyName}");
        }
    }

    public int GetSentencesLength()
    {
        return sentences.Count;
    }


    public void LoadText(string path)
    {
        sentences = new List<string>();

        StreamReader reader = new StreamReader(path);
        while (!reader.EndOfStream)
        {
            string line = reader.ReadLine();
            sentences.Add(line);
        }
        reader.Close();

        TotalCharacters();
        ShowCurrentPage();
    }

    void ShowCurrentPage()
    {
        pageText.text = currentPage.ToString();
        textMesh.text = "";
        int startIndex = currentPage * charactersPerPage;

        int totalCharacters = 0;
        int sentenceIndex = 0;

        // 현재 페이지의 시작 위치를 찾음
        while (sentenceIndex < sentences.Count && totalCharacters < startIndex)
        {
            totalCharacters += sentences[sentenceIndex].Length + 1; // 현재 문장의 길이와 공백을 더하여 총 문자 수를 계산
            sentenceIndex++;
        }

        currentPageStartSentenceIndex = sentenceIndex;

        // 현재 페이지의 텍스트를 표시함
        while (sentenceIndex < sentences.Count && totalCharacters < startIndex + charactersPerPage)
        {
            textMesh.text += sentences[sentenceIndex] + " ";
            totalCharacters += sentences[sentenceIndex].Length + 1; // 현재 문장의 길이와 공백을 더하여 총 문자 수를 계산
            sentenceIndex++;
        }

        originalText = textMesh.text;
    }

    public void ShowPreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            ShowCurrentPage();
        }
    }

    public void ShowNextPage()
    {
        int maxPage = Mathf.CeilToInt((float)totalCharacters / (float)charactersPerPage) - 1;
        if (currentPage < maxPage)
        {
            currentPage++;
            ShowCurrentPage();
        }
    }

    void TotalCharacters()
    {
        totalCharacters = 0;
        foreach (string sentence in sentences)
        {
            totalCharacters += sentence.Length;
        }
    }

    void OnInputFieldValueChanged(string newValue)
    {
        // 사용자의 입력을 정수로 변환하여 intValue 변수에 저장합니다.
        if (int.TryParse(newValue, out intValue))
            charactersPerPage = intValue;
        else
            Debug.Log("Invalid Input: " + newValue); // 잘못된 입력이 있을 경우 경고 메시지를 출력합니다.
    }

    // 슬라이더 값이 변경될 때 호출되는 함수입니다.
    public void OnSliderValueChanged(float newValue)
    {
        // 슬라이더의 값이 변경될 때마다 정수로 변환하여 변수에 저장합니다.
        charactersPerPage = (int)newValue;
    }

    public int GetCurrentPageStartSentenceIndex()
    {
        return currentPageStartSentenceIndex;
    }

    public string GetSentence(int sentenceIndex)
    {
        return sentences[sentenceIndex];
    }

    public void InitTextSetting()
    {
        /*
         * Line height (line spacing) to at least 1.5 times the font size
         * Spacing following paragraphs to at least 2 times the font size
         * Letter spacing (tracking) to at least 0.12 times the font size
         * Word spacing to at least 0.16 times the font size
         */
        textMesh.characterSpacing = characterSpacing;
        textMesh.lineSpacing = lineSpacing;
        textMesh.fontSize = fontSize;
        charactersPerPage = charactersPerPageDefault;
        if (sentences != null)
            ShowCurrentPage();
    }
}
