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

    Chart chart = new();
    public IReadOnlyChart Chart => chart;
    public string ChartPath { get; private set; } = null;
    public string ChartDir => ChartPath == null ? null : Path.GetDirectoryName(ChartPath);

    public bool HasChanges { get; private set; } = true;

    public uint BPM
    {
        get => chart.BPM;
        set
        {
            if (value != chart.BPM)
            {
                HasChanges = true;
                chart.BPM = value;
            }
        }
    }

    public int Offset
    {
        get => chart.Offset;
        set
        {
            if (value != chart.Offset)
            {
                HasChanges = true;
                chart.Offset = value;
            }
        }
    }

    void Awake()
    {
        Instance = this;
    }

    public void LoadChart(string path)
    {
        chart = new ChartParser(path).Run();
        ChartPath = path;
        HasChanges = false;
        OnChartLoaded?.Invoke(path);
    }

    public void SaveChart(string path)
    {
        ChartParser.Write(chart, path);
        ChartPath = path;
        HasChanges = false;
        OnChartLoaded?.Invoke(path);
    }
}
