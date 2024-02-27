using System.Collections.Generic;
using UnityEngine;

public class Chart
{
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
}