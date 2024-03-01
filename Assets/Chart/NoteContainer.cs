using System.Collections.Generic;

public interface INoteContainer<NoteType> where NoteType : Note
{
	public IReadOnlyCollection<NoteType> Notes { get; }

	bool AddNoteWithoutNotify(NoteType note);
	bool RemoveNoteWithoutNotify(NoteType note);
	// bool AddNote(NoteType note);
	// bool RemoveNote(NoteType note);
    bool ContainsNote(NoteType note);

    void OnNoteAdded(NoteType note);
	void OnNoteRemoved(NoteType note);
}

public static class NoteContainerExtensions
{
	public static bool AddNote<C, N>(this INoteContainer<C> container, N note) 
	where N : C
	where C : Note
	{
		if (container.AddNoteWithoutNotify(note))
		{
			container.OnNoteAdded(note);
			return true;
		}
		return false;
	}

	public static bool RemoveNote<C, N>(this INoteContainer<C> container, N note) 
	where N : C
	where C : Note
	{
		if (container.RemoveNoteWithoutNotify(note))
		{
			container.OnNoteRemoved(note);
			return true;
		}
		return false;
	}

	// public static bool AddNoteUnsafe<C, N>(this INoteContainer<C> container, N note) 
	// where N : C
	// where C : Note
    // {
    //     return 
    // }
	// public static bool RemoveNoteUnsafe<C, N>(this INoteContainer<C> container, N note) 
	// where N : C
	// where C : Note
    // {
        
    // }
	// bool AddNoteUnsafe(Note note) => MyAddNote((NoteType) note); 
	// bool RemoveNoteUnsafe(Note note) => MyRemoveNote((NoteType) note);

	// public static bool UpdateNote<C, N>(this INoteContainer<C> container, N note) 
	// where N : C
	// where C : Note
	// {
	// 	if (container.RemoveNoteWithoutNotify(note))
	// 	{
	// 		container.AddNoteWithoutNotify(note);
	// 		return true;
	// 	}
	// 	return false;
	// }
}