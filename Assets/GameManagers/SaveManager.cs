// Handles loading and saving the chart

using System;
using System.IO;
using System.Linq;
using SFB;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public delegate void ChartLoadedEvent(string path);
    public delegate void ChartPathChangedEvent(string newPath);

    public static event ChartLoadedEvent OnChartLoaded;
    public static event ChartPathChangedEvent OnChartPathChanged;

    public static SaveManager Instance;

    Chart chart = new();
    public string ChartPath { get; private set; } = null;
    public string ChartDir => ChartPath == null ? null : Path.GetDirectoryName(ChartPath);

    public bool HasChanges { get; private set; } = true;

    public uint BPM
    {
        get => chart.BPM;
        set
        {
            HasChanges = true;
            chart.BPM = value;
        }
    }

    public int Offset
    {
        get => chart.Offset;
        set
        {
            HasChanges = true;
            chart.Offset = value;
        }
    }

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
                chart = new ChartParser(path).Run();
                ChartPath = path;
                HasChanges = false;
                OnChartPathChanged?.Invoke(path);
                OnChartLoaded?.Invoke(path);
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
        HasChanges = false;
    }

    public void SaveChartAs()
    {
        Debug.Log(ChartPath == null);
        StandaloneFileBrowser.SaveFilePanelAsync("Save Chart As", ChartDir, "MyChart", "txt", path => 
        {
            if (string.IsNullOrEmpty(path)) return;
            WriteChart(path);
            ChartPath = path;
            HasChanges = false;
            OnChartPathChanged?.Invoke(path);
        });
    }

    void WriteChart(string path)
    {
        using StreamWriter writer = new(path);

        writer.WriteLine("# Metadata");
        writer.WriteLine($"BPM {chart.BPM}");
        writer.WriteLine($"Offset {chart.Offset}");
        writer.WriteLine();

        writer.WriteLine("# Notes");

        static char lrToString(LeftRight lr) => lr switch
        {
            LeftRight.Left => 'L',
            LeftRight.Right => 'R',
            _ => throw new Exception($"Unable to serialize LeftRight {lr}"),
        };
        static char leverPosToString(LeverPos pos) => pos switch
        {
            LeverPos.Left => '<',
            LeverPos.Up => '^',
            LeverPos.Right => '>',
            LeverPos.Down => 'V',
            LeverPos.Middle => '.',
            _ => throw new Exception($"Unable to serialize LeverPos {pos}"),
        };

        foreach (Note note in chart.Notes)
        {
            string line = note switch
            {
                ButtonNote b => $"B {b.StartTime} {b.LeftPos}" + (b.RightPos == null ? "" : $" {b.RightPos}"),
                WheelFlick f => $"F {f.StartTime} {lrToString(f.Dir)}",
                WheelSequence w => $"W {w.StartTime} {w.Pos}" +
                    w.Turns.Aggregate("", (s, t) => s + $" {t.deltaAngle} {t.EndTime}"),
                LeverSequence l => $"L {lrToString(l.Dir)}" +
                    l.Notes.Aggregate("", (s, n) => s + $" {n.StartTime} {leverPosToString(n.pos)}"),
                _ => throw new Exception($"Unable to serialize note {note}"),
            };
            writer.WriteLine(line);
        }
    }
}
