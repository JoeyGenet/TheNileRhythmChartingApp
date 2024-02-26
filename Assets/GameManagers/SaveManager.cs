using System;
using System.IO;
using System.Windows.Forms;
using SFB;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public delegate void ChartLoadedEvent(Chart chart, string path);
    public delegate void ChartPathChangedEvent(string newPath);

    public static event ChartLoadedEvent OnChartLoaded;
    public static event ChartPathChangedEvent OnChartPathChanged;

    public static SaveManager Instance;

    public Chart Chart { get; private set; } = new Chart();
    public string ChartPath { get; private set; } = null;
    public string ChartDir => ChartPath == null ? null : Path.GetDirectoryName(ChartPath);

    void Awake()
    {
        Instance = this;
    }

    public void LoadChart()
    {
        StandaloneFileBrowser.OpenFilePanelAsync("Load Chart", ChartDir, "", false, paths =>
        {
            if (paths.Length == 0 || string.IsNullOrEmpty(paths[0])) return;

            try 
            {
                string path = paths[0];
                Chart = Chart.ParseChart(path);
                ChartPath = path;
                OnChartPathChanged?.Invoke(path);
                OnChartLoaded?.Invoke(Chart, path);
            }
            catch (Exception e)
            {
                UIManager.Instance.ShowAlert("Exception While Loading Chart", e.Message);
                Debug.LogException(e);
            }
        });
    }

    public void SaveChart()
    {
        if (ChartPath == null) throw new Exception("Cannot save chart. No path is selected.");

        WriteChart(ChartPath);
        Debug.Log($"Saved to {ChartPath}");
    }

    public void SaveChartAs()
    {
        Debug.Log(ChartPath == null);
        StandaloneFileBrowser.SaveFilePanelAsync("Save Chart As", ChartDir, "MyChart", "txt", path => 
        {
            if (string.IsNullOrEmpty(path)) return;
            WriteChart(path);
            ChartPath = path;
            OnChartPathChanged?.Invoke(path);
        });

        // NativeFilePicker.ExportFile(path, success =>
        // {
        //     if (success) Debug.Log("File successfully exported");
        //     else Debug.Log("File export failed");
        // });

        // // Write the string array to a new file named "WriteLines.txt".
        // using (StreamWriter outputFile = new(path))
        // {
        //     outputFile.WriteLine("Hello world");
        // }
        // Debug.Log($"Saved to {path}");
    }

    void WriteChart(string path)
    {
        using (StreamWriter outputFile = new(path))
        {
            outputFile.WriteLine("Hello world");
        }
    }
}
