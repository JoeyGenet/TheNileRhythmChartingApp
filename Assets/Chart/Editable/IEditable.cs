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
public delegate void NoteOrderChangedEvent();

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
	public event NoteOrderChangedEvent OnOrderChanged;

	bool AddNote(NoteType note, bool notify = true);
	bool RemoveNote(NoteType note, bool notify = true);
    bool ContainsNote(NoteType note);
}

public static class IEditableExtensions
{
    public static bool RegisterChange<T>(this IEditable editable, ref T value, T newValue,
                                        bool notify = true, Action notifyAction = null)
    {
        if (newValue.Equals(value)) return false;
        value = newValue;
        if (notify)
        {
            editable.SetChanged();
            notifyAction?.Invoke();
        }
        return true;
    }

    public static bool RegisterSortingChange<T, E, N>(this E editable, IEditableNoteContainer<N> container,
                                        ref T value, T newValue,
                                        bool notify = true, Action notifyAction = null)
    where N : Note
    where E : IEditable, N
    {
        if (newValue.Equals(value)) return false;
        container.RemoveNote(editable, notify);
        value = newValue;
        container.AddNote(editable, notify);
        if (notify)
        {
            editable.SetChanged();
            notifyAction?.Invoke();
        }
        return true;
    }

	public static bool RegisterChange(this IEditable editable, Func<bool> changeAction, bool notify = true, Action notifyAction = null)
	{
		if (!changeAction()) return false;
		if (notify)
		{
			editable.SetChanged();
			notifyAction?.Invoke();
		}
		return true;
	}

    public static bool RemoveSelf<N>(this IEditableNote<IEditableNoteContainer<N>, N> note)
    where N : Note
        => note.Container.RemoveNote(note as N);
    
    // public static bool RemoveSelf<T>(this Note note)

}