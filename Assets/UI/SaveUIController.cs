using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SFB;
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
        loadButton.onClick.AddListener(LoadChart);
        saveButton.onClick.AddListener(SaveChart);
        saveAsButton.onClick.AddListener(SaveChartAs);
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

    void LoadChart()
    {
        StandaloneFileBrowser.OpenFilePanelAsync("Load Chart", SaveManager.Instance.ChartDir, "", false, paths =>
        {
            if (paths.Length == 0 || string.IsNullOrEmpty(paths[0])) return; // User pressed cancel. Don't load
            if (!File.Exists(paths[0]))
            {
                UIManager.Instance.ShowAlert("File Path Invalid or Does Not Exist", "No file will be loaded.");
                return;
            }

            try 
            {
                SaveManager.Instance.LoadChart(paths[0]);
            }
            catch (ChartParser.ChartParseException e)
            {
                UIManager.Instance.ShowAlert("Error Loading Chart", $"Loading from {paths[0]}.\n{e.Message}");
                Debug.LogException(e.Cause);
            }
            catch (Exception e)
            {
                UIManager.Instance.ShowAlert("Error Loading Chart", $"Loading from {paths[0]}.\n{e.Message}");
                Debug.LogException(e);
            }
        });
    }

    void TrySaveChart(string path)
    {
        try 
        {
            SaveManager.Instance.SaveChart(path);
        }
        catch (Exception e)
        {
            UIManager.Instance.ShowAlert("Error Saving Chart", $"File {path} was not saved.\n{e.Message}");
            Debug.LogException(e);
        }
    }

    void SaveChart()
    {
        if (string.IsNullOrEmpty(SaveManager.Instance.ChartPath))
        {
            UIManager.Instance.ShowAlert("Cannot Save Chart", "Load a chart first before saving it.");
            return;
        }
        TrySaveChart(SaveManager.Instance.ChartPath);
    }

    void SaveChartAs()
    {
        StandaloneFileBrowser.SaveFilePanelAsync("Save Chart As", SaveManager.Instance.ChartDir, "MyChart", "txt", path => 
        {
            if (string.IsNullOrEmpty(path)) return; // User pressed cancel. Don't save.
            TrySaveChart(path);
        });
    }
}
