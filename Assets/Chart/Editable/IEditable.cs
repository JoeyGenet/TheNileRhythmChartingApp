using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        public int saveStateSlot;
        public string saveStatePath;

        public EditLogEntry(int saveStateSlot, string saveStatePath)
        {
            this.saveStateSlot = saveStateSlot;
            this.saveStatePath = saveStatePath;
        }
    }

    private static List<EditLogEntry> editHistory = new List<EditLogEntry>();
    // -1 signifies empty edit history
    private static int currentEditHistoryHead = -1;
    private const int maxSaveStateCount = 20;
    // TODO: make this dynamic with maxSaveStateCount
    private static Queue<int> freeSaveStateSlots = new Queue<int>(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
    private static List<int> usedSaveStateSlots = new List<int>();

    public static void ResetEditHistory()
    {
        // TODO: implement this
        editHistory = new List<EditLogEntry>();
        currentEditHistoryHead = -1;
        new Queue<int>(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
        usedSaveStateSlots = new List<int>();
        AddEditHistoryEntry();
    }

    // TODO: make sure old chart states are properly deleted/cleared
    public static void AddEditHistoryEntry()
    {
        // remove any history entries above the current head when we make a new change
        if (editHistory.Count != 0 && currentEditHistoryHead + 1 < editHistory.Count)
        {
            int firstOverwrittenIndex = currentEditHistoryHead + 1;
            int numberOfEntriesToRemove = editHistory.Count - firstOverwrittenIndex;
            editHistory.RemoveRange(firstOverwrittenIndex, numberOfEntriesToRemove);
            // mark undone slots as free
            for(int i = 0; i < numberOfEntriesToRemove; i++)
            {
                int freedindex = usedSaveStateSlots.Count - 1;
                int freedSaveSlot = usedSaveStateSlots.ElementAt(freedindex);
                usedSaveStateSlots.RemoveAt(freedindex);
                freeSaveStateSlots.Enqueue(freedSaveSlot);
            }
        }

        // add new entry
        currentEditHistoryHead = Mathf.Clamp(currentEditHistoryHead + 1, 0, maxSaveStateCount - 1);
        int saveStateSlot;
        string saveStatePath;
        // if we have free save state slots, use the oldest one and then add it to the back of the used list
        if (freeSaveStateSlots.Count > 0)
        {
            saveStateSlot = freeSaveStateSlots.Dequeue();
        }
        // if no free save state slots available, use the oldest used slot and move it to back of the list
        else
        {
            saveStateSlot = usedSaveStateSlots.ElementAt(0);
            usedSaveStateSlots.RemoveAt(0);
        }
        // mark the save slot as used and set up new path
        usedSaveStateSlots.Add(saveStateSlot);
        saveStatePath = "Assets/Utils/ChartSaveStates/ChartState" + saveStateSlot + ".zip";

        // add new entry -- if we go over the limit, remove the oldest entry
        editHistory.Add(new EditLogEntry(saveStateSlot, saveStatePath));
        if (editHistory.Count > maxSaveStateCount)
        {
            editHistory.RemoveAt(0);
        }

        // create new backup chart with path
        ChartManager.SaveChartState(saveStatePath);

        Debug.Log("AddEditHistoryEntry() | currentEditHistoryHead: "+currentEditHistoryHead+", saveStateSlot: " + saveStateSlot + ", saveStatePath: " + saveStatePath);
    }

    public static async void Undo()
    {
        currentEditHistoryHead = Mathf.Clamp(currentEditHistoryHead - 1, 0, editHistory.Count - 1);
        await ChartManager.LoadChartState(editHistory[currentEditHistoryHead].saveStatePath);
        // mark selected chart as changed to that we can save the changes after undo
        ChartManager.SelectedChart.SetChanged();

        Debug.Log("Undo() | currentEditHistoryHead: "+currentEditHistoryHead+", saveStateSlot: " + editHistory[currentEditHistoryHead].saveStateSlot + ", saveStatePath: " + editHistory[currentEditHistoryHead].saveStatePath);
    }
    
    public static async void Redo()
    {
        currentEditHistoryHead = Mathf.Clamp(currentEditHistoryHead + 1, 0, editHistory.Count - 1);
        await ChartManager.LoadChartState(editHistory[currentEditHistoryHead].saveStatePath);
        // mark selected chart as changed to that we can save the changes after redo
        ChartManager.SelectedChart.SetChanged();

        Debug.Log("Redo() | currentEditHistoryHead: "+currentEditHistoryHead+", saveStateSlot: " + editHistory[currentEditHistoryHead].saveStateSlot + ", saveStatePath: " + editHistory[currentEditHistoryHead].saveStatePath);
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