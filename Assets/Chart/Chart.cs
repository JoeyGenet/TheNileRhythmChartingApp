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
	public IReadOnlyCollection<Note> Notes => notes;

    readonly SortedSet<Note> notes;

	public Chart(uint bpm = 120, int offset = 0)
	{
		notes = new();
		BPM = bpm;
		Offset = offset;
	}


	public int BeatToTime(float beat) => (int) (beat * 60000f / BPM);
	public float TimeToBeat(int time) => time * BPM / 60000f;

	public bool AddNoteWithoutNotify(Note note) => notes.Add(note);
	public bool RemoveNoteWithoutNotify(Note note) => notes.Remove(note);
	public bool ContainsNote(Note note) => notes.Contains(note);

    void INoteContainer<Note>.OnNoteAdded(Note note)
    {
		SaveManager.Instance.HasChanges = true;
		OnNoteAdded?.Invoke(note);
    }

    void INoteContainer<Note>.OnNoteRemoved(Note note)
    {
		SaveManager.Instance.HasChanges = true;
		OnNoteRemoved?.Invoke(note);
    }
}