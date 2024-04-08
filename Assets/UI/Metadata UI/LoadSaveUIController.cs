using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using SFB;
using TMPro;
using UnityEngine;

public class LoadSaveUIController : MonoBehaviour
{
    [SerializeField] TMP_Text pathText;
    
    [SerializeField] EventRelay loadEvent;
    [SerializeField] EventRelay saveEvent;
    [SerializeField] EventRelay saveAsEvent;

    string defaultPathText;

    void Awake()
    {
        defaultPathText = pathText.text;

        loadEvent.Event.AddListener(LoadChart);
        saveEvent.Event.AddListener(SaveChart);
        saveAsEvent.Event.AddListener(SaveChartAs);
    }

    void Update()
    {
        // Update path text
        string path = ChartManager.ChartPath;
        bool hasPath = !string.IsNullOrEmpty(path);
        if (!hasPath) pathText.text = defaultPathText;
        else pathText.text = path + (ChartManager.ChartPackage.HasChanges ? "*" : "");
    }

    void LoadChart()
    {
        StandaloneFileBrowser.OpenFilePanelAsync("Load Chart", ChartManager.ChartDir, "", false, paths =>
        {
            if (paths.Length == 0 || string.IsNullOrEmpty(paths[0])) return; // User pressed cancel. Don't load
            if (!File.Exists(paths[0]))
            {
                UIManager.Instance.ShowAlert("File Path Invalid or Does Not Exist", "No file will be loaded.");
                return;
            }

            try 
            {
                ChartManager.LoadChart(paths[0]);
            }
            catch (FileParseException e)
            {
                UIManager.Instance.ShowAlert("Error Loading Chart", $"Loading from {paths[0]}.\n{e.Message}");
                Debug.LogException(e);
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
            ChartManager.SaveChart(path);
        }
        catch (Exception e)
        {
            UIManager.Instance.ShowAlert("Error Saving Chart", $"File {path} was not saved.\n{e.Message}");
            Debug.LogException(e);
        }
    }

    void SaveChart()
    {
        if (string.IsNullOrEmpty(ChartManager.ChartPath)) SaveChartAs();
        else TrySaveChart(ChartManager.ChartPath);
    }

    void SaveChartAs()
    {
        StandaloneFileBrowser.SaveFilePanelAsync("Save Chart As", ChartManager.ChartDir, "MyChart", "zip", path => 
        {
            if (string.IsNullOrEmpty(path)) return; // User pressed cancel. Don't save.
            TrySaveChart(path);
        });
    }
}
