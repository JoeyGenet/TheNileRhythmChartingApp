using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public interface INoteContainer<out NoteType> where NoteType : Note
{
	public IReadOnlyCollection<NoteType> Notes { get; }

	public NoteType FirstNote { get; }
	public NoteType LastNote { get; }
}
