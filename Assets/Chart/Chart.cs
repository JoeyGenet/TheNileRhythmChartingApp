using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface IChart
{
	public uint BPM { get; }
	public int Offset { get; }
	public IReadOnlyList<Note> Notes { get; }
}

public class Chart : IChart
{
	public uint BPM { get; set; }
	public int Offset { get; set; }
	public IReadOnlyList<Note> Notes => notes.ToList();
	readonly SortedSet<Note> notes;

	public Chart(List<Note> notes, uint bpm = 100, int offset = 0)
	{
		this.notes = new(notes);
		BPM = bpm;
		Offset = offset;
		Debug.Log("notes in chart (not including notes inside sequences): " + Notes.Count);
	}

	public Chart() : this(new List<Note>()) { }
}