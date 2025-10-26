// Handles loading, saving, and registering changes to the chart

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public static class ChartManager
{
    public delegate void ChartPackageChangedEvent(EditableChartPackage prevPackage, EditableChartPackage newPackage);
    public delegate void ChartChangedEvent(EditableChart prevChart, EditableChart newChart);
    public delegate void ChartAddedEvent(EditableChart chart);
    public delegate void ChartRemovedEvent(EditableChart chart);
    public delegate void NoteAddedEvent(Note note);
    public delegate void NoteRemovedEvent(Note note);
    public delegate void EventAddedEvent(EnvironmentChangeEvent evt);
    public delegate void EventRemovedEvent(EnvironmentChangeEvent evt);

    public static event ChartPackageChangedEvent OnChartPackageChanged;
    public static event ChartChangedEvent OnChartChanged; // When the currently selected chart changes
    public static event ChartAddedEvent OnChartAdded; // When a chart is added to the current chart package
    public static event ChartRemovedEvent OnChartRemoved; // When a chart is removed from the current chart package
	public static event NoteAddedEvent OnNoteAdded; // When a note is added to the currently selected chart
	public static event NoteRemovedEvent OnNoteRemoved; // When a note is removed from the currently selected chart
    public static event EventAddedEvent OnEventAdded;
    public static event EventRemovedEvent OnEventRemoved;

    public static string ChartPath { get; private set; } = null;
    public static string ChartDir => ChartPath == null ? null : Path.GetDirectoryName(ChartPath);

    static EditableChartPackage chartPackage;
    public static EditableChartPackage ChartPackage
    {
        get => chartPackage;
        private set
        {
            var pkg = value;
            if (pkg == chartPackage || pkg == null) return;

            var oldPackage = chartPackage;
            chartPackage = pkg;
            if (pkg.Charts.Count == 0)
            {
                SelectedChart = chartPackage.AddChart();
            }
            else if (SelectedChart != null) // Find the chart that most closely matches the current chart's difficulty
            {
                var newSelectedChart = pkg.Charts.FirstOrDefault(c => c.DifficultyName == SelectedChart.DifficultyName);
                newSelectedChart ??= pkg.Charts.OrderBy(c => Math.Abs(c.DifficultyRating - SelectedChart.DifficultyRating)).FirstOrDefault();
                newSelectedChart ??= chartPackage.Charts.First;
                SelectedChart = newSelectedChart;
            }
            else
            {
                SelectedChart = chartPackage.Charts.First;
            }

            if (oldPackage != null)
            {
                oldPackage.OnChartAdded -= SendOnChartAdded;
                oldPackage.OnChartRemoved -= SendOnChartRemoved;
            }

            chartPackage.OnChartAdded += SendOnChartAdded;
            chartPackage.OnChartRemoved += SendOnChartRemoved;

            OnChartPackageChanged?.Invoke(oldPackage, chartPackage);
        }
    }

    static EditableChart selectedChart;
    public static EditableChart SelectedChart
    {
        get => selectedChart;
        set
        {
            var chart = value;
            if (chart == selectedChart || chart == null) return;

            var oldChart = selectedChart;
            Assert.IsTrue(ChartPackage.Charts.Contains(chart));
            selectedChart = chart;

            if (oldChart != null)
            {
                oldChart.OnNoteAdded -= SendOnNoteAdded;
                oldChart.OnNoteRemoved -= SendOnNoteRemoved;
                oldChart.OnEventAdded -= SendOnEventAdded;
                oldChart.OnEventRemoved -= SendOnEventRemoved;
            }

            selectedChart.OnNoteAdded += SendOnNoteAdded;
            selectedChart.OnNoteRemoved += SendOnNoteRemoved;
            selectedChart.OnEventAdded += SendOnEventAdded;
            selectedChart.OnEventRemoved += SendOnEventRemoved;

            OnChartChanged?.Invoke(oldChart, selectedChart);
        }
    }

    static ChartManager()
    {
        ChartPackage = new EditableChartPackage();
    }

    public static async Task LoadChart(string path)
    {
        var pkg = new EditableChartPackage();
        using (var stream = File.OpenRead(path))
        {
            await pkg.Load(stream);
        }
        ChartPackage = pkg;
        ChartPath = path;
        // OnChartLoaded?.Invoke(path);
    }

    // restore a chart state but do not change the selected chart
    public static async Task LoadChartState(string path)
    {
        var pkg = new EditableChartPackage();
        using (var stream = File.OpenRead(path))
        {
            await pkg.Load(stream);
        }
        // TODO: what does changing the chart package really do?
        // trigger reload of notes to restore previous state
        ChartPackage = pkg;
        // OnChartChanged?.Invoke(null, pkg.Charts.ElementAt(0));
        // OnChartPackageChanged?.Invoke(null, pkg);
    }

    public static void SaveChart(string path)
    {
        using (var stream = File.Open(path, FileMode.Create))
        {
            ChartPackage.Save(stream);
        }
        ChartPath = path;
        // HasChanges = false;
        // OnChartSaved?.Invoke(path);
    }

    // record the current state of the chart but do not update the chart path.
    public static void SaveChartState(string path)
    {
        using (var stream = File.Open(path, FileMode.Create))
        {
            ChartPackage.Save(stream);
        }
    }

    static void SendOnChartAdded(EditableChart chart) => OnChartAdded?.Invoke(chart);
    static void SendOnChartRemoved(EditableChart chart) => OnChartRemoved?.Invoke(chart);
    static void SendOnNoteAdded(Note note) => OnNoteAdded?.Invoke(note);
    static void SendOnNoteRemoved(Note note) => OnNoteRemoved?.Invoke(note);
    static void SendOnEventAdded(EnvironmentChangeEvent evt) => OnEventAdded?.Invoke(evt);
    static void SendOnEventRemoved(EnvironmentChangeEvent evt) => OnEventRemoved?.Invoke(evt);
}
