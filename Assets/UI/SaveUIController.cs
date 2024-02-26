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

        SaveManager.OnChartPathChanged += SetChartPath;
    }

    void Start()
    {
        loadButton.onClick.AddListener(() => SaveManager.Instance.LoadChart());
        saveButton.onClick.AddListener(() => SaveManager.Instance.SaveChart());
        saveAsButton.onClick.AddListener(() => SaveManager.Instance.SaveChartAs());
        // clearButton.onClick.AddListener(SaveManager.Instance.ClearChart);
    }

    void OnDestroy()
    {
        SaveManager.OnChartPathChanged -= SetChartPath;
    }

    void SetChartPath(string path)
    {
        chartPathText.text = string.IsNullOrEmpty(path) ? defaultChartPathText : path;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
