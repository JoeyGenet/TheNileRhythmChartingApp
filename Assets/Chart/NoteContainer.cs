using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public interface IReadonlyNoteContainer<out NoteType>
where NoteType : Note
{
	public IReadOnlyCollection<NoteType> Notes { get; }

	public NoteType FirstNote { get; }
	public NoteType LastNote { get; }
}

public interface INoteContainer<NoteType> : IReadonlyNoteContainer<NoteType>
where NoteType : Note
{
	bool AddNote(NoteType note, bool notify = true);
	bool RemoveNote(NoteType note, bool notify = true);
    bool ContainsNote(NoteType note);
}
