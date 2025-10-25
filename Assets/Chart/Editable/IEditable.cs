using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public interface IEditable
{
    public bool HasChanges { get; }

    public void SetChanged();
}

public delegate void NoteAddedEvent<N>(N note) where N : Note;
public delegate void NoteRemovedEvent<N>(N note) where N : Note;
public delegate void NoteMovedEvent<N>(N note) where N : Note;

public interface IEditableNote<out C, N> : IEditable
where N : Note
where C : IEditableNoteContainer<N>
{
    public C Container { get; }
}

public interface IEditableNoteContainer<NoteType> : INoteContainer<NoteType>, IEditable
where NoteType : Note
{
	public event NoteAddedEvent<NoteType> OnNoteAdded;
	public event NoteRemovedEvent<NoteType> OnNoteRemoved;
	public event NoteMovedEvent<NoteType> OnNoteMoved;

	bool AddNote(NoteType note, bool notify = true);
	bool RemoveNote(NoteType note, bool notify = true);
    void NotifyMoved(NoteType note);
	// bool UpdateNote(NoteType note, Func<bool> update); // Updates the note's sorting position
    bool ContainsNote(NoteType note);
}

public static class IEditableExtensions
{
    private class EditLogEntry
    {
        // public object value;
        // public object newValue;
        public EditableChart chartState;

        public EditLogEntry(EditableChart chartState)
        {
            this.chartState = chartState;
        }
    }

    private static List<EditLogEntry> editHistory = new List<EditLogEntry>();
    // -1 signifies empty edit history
    private static int currentEditHistoryHead = -1;

    // TODO: make sure old chart states are properly deleted/cleared
    public static void AddEditHistoryEntry()
    {
        // remove any history entries above the current head when we 
        // make a new change
        Debug.Log("AddEditHistoryEntry() called");
        if (editHistory.Count != 0 && currentEditHistoryHead + 1 < editHistory.Count)
        {
            int firstOverwrittenIndex = currentEditHistoryHead + 1;
            editHistory.RemoveRange(firstOverwrittenIndex, editHistory.Count - firstOverwrittenIndex);
        }
        // add new entry
        editHistory.Add(new EditLogEntry(ChartManager.SelectedChart));
        currentEditHistoryHead += 1;
        // TODO: create new backup chart with path
        ChartManager.SaveChartState("Assets/Utils/ChartSaveStates/ChartState"+currentEditHistoryHead+".zip");
    }

    public static async void Undo()
    {
        Debug.Log("Undo called!");
        ChartManager.SelectedChart.SetChanged();
        currentEditHistoryHead = Mathf.Clamp(currentEditHistoryHead - 1, 0, editHistory.Count-1);
        // ChartManager.SelectedChart = editHistory[currentEditHistoryHead].chartState;
        await ChartManager.LoadChartState("Assets/Utils/ChartSaveStates/ChartState"+currentEditHistoryHead+".zip");
    }
    
    public static async void Redo()
    {
        Debug.Log("Redo called!");
        ChartManager.SelectedChart.SetChanged();
        currentEditHistoryHead = Mathf.Clamp(currentEditHistoryHead + 1, 0, editHistory.Count - 1);
        // ChartManager.SelectedChart = editHistory[currentEditHistoryHead].chartState;
        await ChartManager.LoadChartState("Assets/Utils/ChartSaveStates/ChartState"+currentEditHistoryHead+".zip");
    }
    
    /* 
        TODO:
        have list/stack of changes. edit the list when IEditable.RegisterChange or IEditable.RegisterSortingChange are called
        each item in the stack includes:
        - the IEditable object
        - the old value
        - the new value

        the stack maintains an index of the current head of the stack.
        -when you undo, return the top IEditable object to old value, then move top pointer down.
        -when you redo, move top pointer up, change the IEditable to the stored new value.

        also would need to be able to represent groups of changes from multi select and copy/paste
    */
    // TODO: when modifying note position at same timestamp, this is called (or if you make a non-note related change)
    public static bool RegisterChange<T>(this IEditable editable, ref T value, T newValue, Action onChanged = null)
    {
        if (newValue.Equals(value)) return false;
        value = newValue;
        editable.SetChanged();
        onChanged?.Invoke();
        // AddEditHistoryEntry();
        return true;
    }

    // TODO: when modifying a note, it is removed and then re-added? link in the undo log tracking here
    // this is only triggered if the start time is changed
    public static bool RegisterSortingChange<T, E, N>(this E editable, IEditableNoteContainer<N> container,
                                        ref T value, T newValue, Action onChanged = null)
    where N : Note
    where E : IEditable, N
    {
        if (newValue.Equals(value)) return false;
        container?.RemoveNote(editable, notify: false);
        value = newValue;
        container?.AddNote(editable, notify: false);
        container?.NotifyMoved(editable);
        // container.UpdateNote(editable, () => value = newValue);
        editable.SetChanged();
        onChanged?.Invoke();
        // AddEditHistoryEntry();
        return true;
    }

	public static bool RegisterChange(this IEditable editable, Func<bool> changeAction, Action onChanged = null)
	{
		if (!changeAction()) return false;
        editable.SetChanged();
        onChanged?.Invoke();
        AddEditHistoryEntry();
		return true;
	}

    public static bool RemoveSelf<N>(this IEditableNote<IEditableNoteContainer<N>, N> note)
    where N : Note
        => note.Container.RemoveNote(note as N);
}