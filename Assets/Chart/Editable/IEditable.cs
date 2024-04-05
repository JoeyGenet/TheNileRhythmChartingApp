using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEditable
{
    public bool HasChanges { get; }

    public void SetChanged();
}

public interface IEditableNoteContainer<NoteType> : INoteContainer<NoteType>
where NoteType : Note
{
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
        container.RemoveNote(editable, notify: false);
        value = newValue;
        container.AddNote(editable, notify: false);
        if (notify)
        {
            editable.SetChanged();
            notifyAction?.Invoke();
        }
        return true;
    }

    public static bool RegisterSortingChange<T, E, N>(this E editable, N note,
                                        ref T value, T newValue,
                                        bool notify = true, Action notifyAction = null)
    where N : Note
    where E : IEditable, IEditableNoteContainer<N>
    {
        if (newValue.Equals(value)) return false;
        editable.RemoveNote(note, notify: false);
        value = newValue;
        editable.AddNote(note, notify: false);
        if (notify)
        {
            editable.SetChanged();
            notifyAction?.Invoke();
        }
        return true;
    }
}