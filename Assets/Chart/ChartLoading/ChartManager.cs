// Handles loading, saving, and registering changes to the chart

using System;
using System.IO;
using System.Linq;
using SFB;
using UnityEngine;

public static class ChartManager
{
    public delegate void ChartLoadedEvent(string path);
    public delegate void ChartSavedEvent(string path);
	// public delegate void NoteAddedEvent<C, N>(C container, N note) where N : Note where C : INoteContainer<N>;
	// public delegate void NoteRemovedEvent<C, N>(C container, N note) where N : Note where C : INoteContainer<N>;

    public static event ChartLoadedEvent OnChartLoaded;
    public static event ChartSavedEvent OnChartSaved;
	// public static event NoteAddedEvent<Chart, Note> OnNoteAdded;
	// public static event NoteRemovedEvent<Chart, Note> OnNoteRemoved;
	// public static event NoteAddedEvent<WheelSequence, WheelTurn> OnWheelTurnAdded;
	// public static event NoteRemovedEvent<WheelSequence, WheelTurn> OnWheelTurnRemoved;
	// public static event NoteAddedEvent<LeverSequence, LeverNote> OnLeverNoteAdded;
	// public static event NoteRemovedEvent<LeverSequence, LeverNote> OnLeverNoteRemoved;

    public static EditableChartPackage ChartPackage = new();
    public static string ChartPath { get; private set; } = null;
    public static string ChartDir => ChartPath == null ? null : Path.GetDirectoryName(ChartPath);

    // public bool HasChanges = true;

    // void Awake()
    // {
    //     Instance = this;
    //     // Chart.AddNote(new WheelSequence(Chart, 1000, 50, new() { 180, -180 }, new() { 2000, 3000 }));
    //     // Chart.AddNote(new WheelSequence(null, 1000, 25, new() { 90, 0, -90 }, new() { 2000, 3000, 4000 }));
    //     // Chart.AddNote(new WheelSequence(null, 1000, 100, new() { -180, -180, -180 }, new() { 2000, 3000, 4000 }));
    //     // W 115159 50 -45 115553 90 115948 -45 116343 89 116738 -44 117132 90 117527 -45 117922
    //     // Chart.AddNote(new WheelSequence(null, 1000, 75, new() { (90, 2000), (-45, 2000) }));
    // }

    public static async void LoadChart(string path)
    {
        using (var stream = File.OpenRead(path))
        {
            ChartPackage = new EditableChartPackage();
            await ChartPackage.Load(stream);
        }
        ChartPath = path;
        // HasChanges = false;
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
