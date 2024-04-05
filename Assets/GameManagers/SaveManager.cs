// Handles loading, saving, and registering changes to the chart

using System;
using System.IO;
using System.Linq;
using SFB;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public delegate void ChartLoadedEvent(string path);
    public delegate void ChartSavedEvent(string path);
	public delegate void NoteAddedEvent<C, N>(C container, N note) where N : Note where C : INoteContainer<N>;
	public delegate void NoteRemovedEvent<C, N>(C container, N note) where N : Note where C : INoteContainer<N>;

    public static event ChartLoadedEvent OnChartLoaded;
    public static event ChartSavedEvent OnChartSaved;
	public static event NoteAddedEvent<Chart, Note> OnNoteAdded;
	public static event NoteRemovedEvent<Chart, Note> OnNoteRemoved;
	public static event NoteAddedEvent<WheelSequence, WheelTurn> OnWheelTurnAdded;
	public static event NoteRemovedEvent<WheelSequence, WheelTurn> OnWheelTurnRemoved;
	public static event NoteAddedEvent<LeverSequence, LeverNote> OnLeverNoteAdded;
	public static event NoteRemovedEvent<LeverSequence, LeverNote> OnLeverNoteRemoved;

    public static SaveManager Instance;

    public ChartPackage ChartPackage = new();
    public string ChartPath { get; private set; } = null;
    public string ChartDir => ChartPath == null ? null : Path.GetDirectoryName(ChartPath);

    public bool HasChanges = true;

    void Awake()
    {
        Instance = this;
        // Chart.AddNote(new WheelSequence(Chart, 1000, 50, new() { 180, -180 }, new() { 2000, 3000 }));
        // Chart.AddNote(new WheelSequence(null, 1000, 25, new() { 90, 0, -90 }, new() { 2000, 3000, 4000 }));
        // Chart.AddNote(new WheelSequence(null, 1000, 100, new() { -180, -180, -180 }, new() { 2000, 3000, 4000 }));
        // W 115159 50 -45 115553 90 115948 -45 116343 89 116738 -44 117132 90 117527 -45 117922
        // Chart.AddNote(new WheelSequence(null, 1000, 75, new() { (90, 2000), (-45, 2000) }));
    }

    public void LoadChart(string path)
    {
        ChartPackage newChart = ChartPackage.Load(path);;
        ChartPath = path;
        HasChanges = false;
        ChartPackage = newChart;
        OnChartLoaded?.Invoke(path);
    }

    public void SaveChart(string path)
    {
        ChartPackage.Save(path);
        ChartPath = path;
        HasChanges = false;
        OnChartSaved?.Invoke(path);
    }

    public static bool RegisterChange<T>(ref T value, T newValue, bool notify = true, Action notifyAction = null)
    {
        if (newValue.Equals(value)) return false;
        value = newValue;
        if (notify)
        {
            if (Instance != null) Instance.HasChanges = true;
            notifyAction?.Invoke();
        }
        return true;
    }

	public static bool RegisterChange(Func<bool> changeAction, bool notify = true, Action notifyAction = null)
	{
		if (!changeAction()) return false;
		if (notify)
		{
			if (Instance != null) Instance.HasChanges = true;
			notifyAction?.Invoke();
		}
		return true;
	}

	public static bool RegisterNoteAdd<C, N>(C container, N note, Func<N, bool> addAction, bool notify = true, Action notifyAction = null)
    where N : Note where C : INoteContainer<N> => RegisterChange(
        () => addAction(note),
        notify,
        () =>
        {
            notifyAction?.Invoke();

            if (container is Chart chart)
                OnNoteAdded?.Invoke(chart, note);
            if (container is WheelSequence wheelSeq && note is WheelTurn turn)
                OnWheelTurnAdded?.Invoke(wheelSeq, turn);
            if (container is LeverSequence leverSeq && note is LeverNote leverNote)
                OnLeverNoteAdded?.Invoke(leverSeq, leverNote);
        }
    );

	public static bool RegisterNoteRemove<C, N>(C container, N note, Func<N, bool> removeAction, bool notify = true, Action notifyAction = null)
    where N : Note where C : INoteContainer<N> => RegisterChange(
        () => removeAction(note),
        notify,
        () =>
        {
            notifyAction?.Invoke();
            
            if (container is Chart chart)
                OnNoteRemoved?.Invoke(chart, note);
            if (container is WheelSequence wheelSeq && note is WheelTurn turn)
                OnWheelTurnRemoved?.Invoke(wheelSeq, turn);
            if (container is LeverSequence leverSeq && note is LeverNote leverNote)
                OnLeverNoteRemoved?.Invoke(leverSeq, leverNote);
        }
    );
}
