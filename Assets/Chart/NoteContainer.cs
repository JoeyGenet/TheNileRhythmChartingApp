using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public interface INoteContainer<NoteType> where NoteType : Note
{
	public IReadOnlyCollection<NoteType> Notes { get; }

	public NoteType FirstNote { get; }
	public NoteType LastNote { get; }

	// bool AddNoteWithoutNotify(NoteType note);
	// bool RemoveNoteWithoutNotify(NoteType note);
	bool AddNote(NoteType note, bool notify = true);
	bool RemoveNote(NoteType note, bool notify = true);
    bool ContainsNote(NoteType note);

    // void OnNoteAdded(NoteType note);
	// void OnNoteRemoved(NoteType note);
}
