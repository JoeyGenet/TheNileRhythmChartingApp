using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class EditableChart : Chart, IEditable, IEditableNoteContainer<Note>
{
    public bool HasChanges { get; private set; }
    public void SetChanged() => HasChanges = true;
	
	public new int DifficultyRating
    {
        get => difficultyRating;
        set => this.RegisterChange(ref difficultyRating, value);
    }
	public new string DifficultyName
    {
        get => difficultyName;
        set => this.RegisterChange(ref difficultyName, value.Trim());
    }

    public EditableChart(ChartPackage package) : base(package) {}
    public EditableChart(ChartPackage package, Stream stream) : base(package, stream) {}

	public bool ContainsNote(Note note) => notes.Contains(note);

    public new bool AddNote(Note note) => AddNote(note, true);
    public bool AddNote(Note note, bool notify)
		=> SaveManager.RegisterNoteAdd(this, note, notes.Add, notify);

    public bool RemoveNote(Note note, bool notify = true)
		=> SaveManager.RegisterNoteRemove(this, note, notes.Remove, notify);

	public void Save(Stream stream)
	{
        using var writer = new StreamWriter(stream);

		writer.WriteLine("[Difficulty]");
		writer.WriteLine($"Rating: {DifficultyRating}");
		writer.WriteLine($"Name: {DifficultyName}");
		writer.WriteLine();

		writer.WriteLine("[Notes]");

		static char lrToString(LeftRight lr) => lr switch
		{
			LeftRight.Left => 'L',
			LeftRight.Right => 'R',
			_ => throw new Exception($"Unable to serialize LeftRight {lr}"),
		};
		static char leverPosToString(LeverPos pos) => pos switch
		{
			LeverPos.Left => '<',
			LeverPos.Up => '^',
			LeverPos.Right => '>',
			LeverPos.Down => 'V',
			LeverPos.Middle => '.',
			_ => throw new Exception($"Unable to serialize LeverPos {pos}"),
		};

		foreach (Note note in notes)
		{
			string line = note switch
			{
				ButtonNote b => $"B {b.StartTime} {b.LeftPos}" + (b.RightPos == null ? "" : $" {b.RightPos}"),
				WheelFlick f => $"F {f.StartTime} {lrToString(f.Dir)}",
				WheelSequence w => $"W {w.StartTime} {w.Pos}" +
					w.Notes.Aggregate("", (s, t) => s + $" {t.DeltaAngle} {t.EndTime}"),
				LeverSequence l => $"L {lrToString(l.Dir)}" +
					l.Notes.Aggregate("", (s, n) => s + $" {n.StartTime} {leverPosToString(n.Pos)}"),
				_ => throw new Exception($"Unable to serialize note {note}"),
			};
			writer.WriteLine(line);
		}
	}
}
