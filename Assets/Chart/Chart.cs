using System.Collections.Generic;
using UnityEngine;

public class Chart : INoteContainer<Note>
{
	public delegate void NoteAddedEvent(Note note);
	public delegate void NoteRemovedEvent(Note note);

	public static event NoteAddedEvent OnNoteAdded;
	public static event NoteRemovedEvent OnNoteRemoved;

	uint bpm;
    public uint BPM
    {
        get => bpm;
        set => SaveManager.RegisterChange(ref bpm, value);
    }

	int offset;
    public int Offset
    {
        get => offset;
        set => SaveManager.RegisterChange(ref offset, value);
    }
	
    readonly SortedSet<Note> notes;
	public IReadOnlyCollection<Note> Notes => notes;

    public Note FirstNote => notes.Min;
    public Note LastNote => notes.Max;


	public Chart(uint bpm = 120, int offset = 0)
	{
		notes = new();
		BPM = bpm;
		Offset = offset;
	}


	public int BeatToTime(float beat) => (int) (beat * 60000f / BPM) + Offset;
	public float TimeToBeat(int time) => (time - Offset) * BPM / 60000f;

	public bool ContainsNote(Note note) => notes.Contains(note);

    public bool AddNote(Note note, bool notify = true) => SaveManager.ChangeWithNotify(notify, 
		() => notes.Add(note), 
		() => OnNoteAdded?.Invoke(note)
	);

    public bool RemoveNote(Note note, bool notify = true) => SaveManager.ChangeWithNotify(notify,
		() => notes.Remove(note), 
		() => OnNoteRemoved?.Invoke(note)
	);

	// Get all notes that start before or at the given time
	public IReadOnlyCollection<Note> GetNotesBefore(int time, bool inclusive = true) 
		=> notes.GetViewBetween(DummyNote.Min, new DummyNote(time, inclusive ? int.MaxValue : int.MinValue));
}