using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Assertions;

public interface IReadOnlyChart
{
	public uint BPM { get; }
	public int Offset { get; }
	public IReadOnlyList<Note> Notes { get; }
}

public class Chart : IReadOnlyChart
{
	public uint BPM { get; set; }
	public int Offset { get; set; }
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
}