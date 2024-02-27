using System.Collections.Generic;
using UnityEngine;

public class Chart
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
	public IReadOnlyList<Note> Notes => notes;
	public List<Note> notes;

	public Chart(List<Note> notes, uint bpm = 100, int offset = 0)
	{
		this.notes = notes;
		BPM = bpm;
		Offset = offset;
		Debug.Log("notes in chart (not including notes inside sequences): " + Notes.Count);
	}

	public Chart() : this(new List<Note>()) { }

	public int BeatToTime(float beat) => (int) (beat * 60000f / BPM);
	public float TimeToBeat(int time) => time * BPM / 60000f;

	public void AddNote(Note note)
	{
		notes.Add(note);
		SaveManager.Instance.HasChanges = true;
		OnNoteAdded?.Invoke(note);
	}

	public void RemoveNote(Note note)
	{
		if (notes.Remove(note))
		{
			SaveManager.Instance.HasChanges = true;
			OnNoteRemoved?.Invoke(note);
		}
	}
}