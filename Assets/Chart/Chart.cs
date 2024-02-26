using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Assertions;

public class Chart
{
	public uint BPM;
	public int Offset;
	public List<Note> Notes;

	public Chart(List<Note> notes, uint bpm = 100, int offset = 0)
	{
		Notes = notes;
		BPM = bpm;
		Offset = offset;
		Debug.Log("notes in chart (not including notes inside sequences): " + Notes.Count);
	}

	public Chart() : this(new List<Note>()) { }
}