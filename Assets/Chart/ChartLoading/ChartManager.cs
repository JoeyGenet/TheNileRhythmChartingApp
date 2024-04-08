// Handles loading, saving, and registering changes to the chart

using System;
using System.IO;
using System.Linq;
using UnityEngine;

public static class ChartManager
{
    public delegate void ChartLoadedEvent(string path);
    public delegate void ChartSavedEvent(string path);
    public delegate void NoteAddedEvent(Note note);
    public delegate void NoteRemovedEvent(Note note);

    public static event ChartLoadedEvent OnChartLoaded;
    public static event ChartSavedEvent OnChartSaved;
	public static event NoteAddedEvent OnNoteAdded; // When a note is added to the currently selected chart
	public static event NoteRemovedEvent OnNoteRemoved; // When a note is removed from the currently selected chart

    public static EditableChartPackage ChartPackage { get; private set; } = new();
    public static string ChartPath { get; private set; } = null;
    public static string ChartDir => ChartPath == null ? null : Path.GetDirectoryName(ChartPath);
    public static EditableChart SelectedChart { get; private set; }

    static ChartManager()
    {
        SetChartPackage(new EditableChartPackage());
    }

    static void SetChartPackage(EditableChartPackage pkg)
    {
        ChartPackage = pkg;
        if (pkg.Charts.Count == 0)
        {
            SelectedChart = ChartPackage.AddChart();
        }
        else if (SelectedChart != null) // Find the chart that most closely matches the current chart's difficulty
        {
            var newSelectedChart = pkg.Charts.FirstOrDefault(c => c.DifficultyName == SelectedChart.DifficultyName);
            newSelectedChart ??= pkg.Charts.OrderBy(c => Math.Abs(c.DifficultyRating - SelectedChart.DifficultyRating)).FirstOrDefault();
            newSelectedChart ??= ChartPackage.Charts[0];
            SelectedChart = newSelectedChart;
        }
        else
        {
            SelectedChart = ChartPackage.Charts[0];
        }
    }

    public static async void LoadChart(string path)
    {
        var pkg = new EditableChartPackage();
        using (var stream = File.OpenRead(path))
        {
            await pkg.Load(stream);
        }
        SetChartPackage(pkg);

        ChartPath = path;

        OnChartLoaded?.Invoke(path);
    }

    public static void SaveChart(string path)
    {
        using (var stream = File.Open(path, FileMode.Create))
        {
            ChartPackage.Save(stream);
        }
        ChartPath = path;
        // HasChanges = false;
        OnChartSaved?.Invoke(path);
    }
}
