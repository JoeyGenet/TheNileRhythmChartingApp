using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public interface INoteContainer<out NoteType> where NoteType : Note
{
	public IReadOnlyCollection<NoteType> Notes { get; }

	public NoteType FirstNote { get; }
	public NoteType LastNote { get; }
}


public interface IEditableNoteContainer<NoteType> : INoteContainer<NoteType>
where NoteType : Note
{
	bool AddNote(NoteType note, bool notify = true);
	bool RemoveNote(NoteType note, bool notify = true);
    bool ContainsNote(NoteType note);
}