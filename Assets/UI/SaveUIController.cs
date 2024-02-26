using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveUIController : MonoBehaviour
{
    [SerializeField] TMP_Text chartPathText;
    [SerializeField] Button loadButton;
    [SerializeField] Button saveButton;
    [SerializeField] Button saveAsButton;
    [SerializeField] Button clearButton;

    string defaultChartPathText;

    void Awake()
    {
        defaultChartPathText = chartPathText.text;
    }

    void Start()
    {
        loadButton.onClick.AddListener(() => SaveManager.Instance.LoadChart());
        saveButton.onClick.AddListener(() => SaveManager.Instance.SaveChart());
        saveAsButton.onClick.AddListener(() => SaveManager.Instance.SaveChartAs());
        clearButton.interactable = false; // Not implemented yet. This button will reset the editor to a blank state
        // clearButton.onClick.AddListener(SaveManager.Instance.ClearChart);
    }

    void Update()
    {
        // Update path text
        string path = SaveManager.Instance.ChartPath;
        bool hasPath = !string.IsNullOrEmpty(path);
        if (!hasPath) chartPathText.text = defaultChartPathText;
        else chartPathText.text = path + (SaveManager.Instance.HasChanges ? "*" : "");

        saveButton.interactable = hasPath;
    }
}
