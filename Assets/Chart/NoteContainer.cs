
public interface INoteContainer<NoteType> where NoteType : Note
{
	public IReadOnlySortedSet<NoteType> Notes { get; }

	// public NoteType FirstNote { get; }
	// public NoteType LastNote { get; }
}
