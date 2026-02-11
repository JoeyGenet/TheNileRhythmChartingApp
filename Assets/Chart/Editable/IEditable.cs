using System;
using System.Collections;
using System.Collections.Generic;
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

    // if notify is true, then return whether or not the add/removal was successful, otherwise return false
	bool AddNote(NoteType note, bool notify = true, bool updateEditHistory = true);
	bool RemoveNote(NoteType note, bool notify = true, bool updateEditHistory = true);
    void NotifyMoved(NoteType note);
	// bool UpdateNote(NoteType note, Func<bool> update); // Updates the note's sorting position
    bool ContainsNote(NoteType note);
}

public static class IEditableExtensions
{
    public static bool RegisterChange<T>(this IEditable editable, ref T value, T newValue, Action onChanged = null)
    {
        if (newValue.Equals(value)) return false;
        value = newValue;
        editable.SetChanged();
        onChanged?.Invoke();
        return true;
    }

    public static bool RegisterSortingChange<T, E, N>(this E editable, IEditableNoteContainer<N> container,
                                        ref T value, T newValue, Action onChanged = null)
    where N : Note
    where E : IEditable, N
    {
        if (newValue.Equals(value)) return false;
        // if notify is true, then return whether or not the add/removal was successful, otherwise return false
        container?.RemoveNote(editable, notify: false, updateEditHistory: false);
        value = newValue;
        container?.AddNote(editable, notify: false, updateEditHistory: false);
        container?.NotifyMoved(editable);
        // container.UpdateNote(editable, () => value = newValue);
        editable.SetChanged();
        onChanged?.Invoke();
        return true;
    }

	public static bool RegisterChange(this IEditable editable, Func<bool> changeAction, Action onChanged = null)
	{
		if (!changeAction()) return false;
        editable.SetChanged();
        onChanged?.Invoke();
		return true;
	}

    public static bool RemoveSelf<N>(this IEditableNote<IEditableNoteContainer<N>, N> note)
    where N : Note
        => note.Container.RemoveNote(note as N);
}