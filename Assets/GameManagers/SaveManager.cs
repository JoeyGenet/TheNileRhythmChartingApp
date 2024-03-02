// Handles loading and saving the chart

using System;
using System.IO;
using System.Linq;
using SFB;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public delegate void ChartLoadedEvent(string path);
    public delegate void ChartSavedEvent(string path);

    public static event ChartLoadedEvent OnChartLoaded;
    public static event ChartSavedEvent OnChartSaved;

    public static SaveManager Instance;

    public Chart Chart = new()
    {
        // new WheelFlick(500, LeftRight.Left),
        // new WheelFlick(1000, LeftRight.Right),
        // new ButtonNote(1500, 50, null),
        // new ButtonNote(2000, 25, 75),
    };
    public string ChartPath { get; private set; } = null;
    public string ChartDir => ChartPath == null ? null : Path.GetDirectoryName(ChartPath);

    public bool HasChanges = true;

    void Awake()
    {
        Instance = this;
    }

    public void LoadChart(string path)
    {
        Chart newChart = new ChartParser(path).Run();
        ChartPath = path;
        HasChanges = false;
        Chart = newChart;
        OnChartLoaded?.Invoke(path);
    }

    public void SaveChart(string path)
    {
        ChartParser.Write(Chart, path);
        ChartPath = path;
        HasChanges = false;
        OnChartSaved?.Invoke(path);
    }

    public static bool RegisterChange<T>(ref T value, T newValue)
    {
        if (!newValue.Equals(value))
        {
            value = newValue;
            if (Instance != null) Instance.HasChanges = true;
			return true;
        }
		return false;
    }
}
