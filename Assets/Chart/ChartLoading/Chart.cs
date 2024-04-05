using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class Chart : INoteContainer<Note>
{
	public readonly ChartPackage Package;

    readonly SortedSet<Note> notes = new();
	public IReadOnlyCollection<Note> Notes => notes;

    public Note FirstNote => notes.Min;
    public Note LastNote => notes.Max;
	
	public int DifficultyRating { get; private set; } = -1;
	public string DifficultyName { get; private set; } = "";

	public Chart(ChartPackage package)
	{
		Package = package;
	}

	public bool ContainsNote(Note note) => notes.Contains(note);

    public bool AddNote(Note note, bool notify = true)
		=> SaveManager.RegisterNoteAdd(this, note, notes.Add, notify);

    public bool RemoveNote(Note note, bool notify = true)
		=> SaveManager.RegisterNoteRemove(this, note, notes.Remove, notify);

	public static Chart Load(ChartPackage package, Stream stream)
	{
		var chart = new Chart(package);

        using var parser = new FileParser(stream);

		parser["Difficulty"] = (line, args) => parser.KeyValue(new()
		{
			["Rating"] = val => chart.DifficultyRating = int.Parse(val),
			["Name"] = val => chart.DifficultyName = val,
		});

		parser["Notes"] = (line, args) =>
		{
			parser.AssertArgCount(minCount: 1);

			Func<Note> noteParser = args[0].ToUpper() switch
			{
				"0" or "B" => parseButton,
				"1" or "F" => parseWheelFlick,
				"2" or "W" => parseWheelSequence,
				"3" or "L" => parseLeverSequence,
				_ => throw new ArgumentException($"Invalid note type: \"{args[0]}\""),
			};
			chart.AddNote(noteParser());

			ButtonNote parseButton()
			{
				parser.AssertArgCount(minCount: 3, maxCount: 4);
				
				int time = int.Parse(args[1]);
				int pos = int.Parse(args[2]);
				int? pos2 = null;
				if (args.Length == 4)
				{
					pos2 = int.Parse(args[3]);
				}
				return new ButtonNote(chart, time, pos, pos2);
			}
			WheelFlick parseWheelFlick()
			{
				parser.AssertArgCount(3);

				int time = int.Parse(args[1]);
				LeftRight dir = parseLeftRight(args[2]);
				return new WheelFlick(chart, time, dir);
			}
			WheelSequence parseWheelSequence()
			{
				parser.AssertArgCount(minCount: 5);
				parser.AssertArgCount(c => c % 2 == 1, "an odd number of");

				int time = int.Parse(args[1]);
				int pos = int.Parse(args[2]);
				List<(int deltaAngle, int endTime)> turns = new();
				for (int i = 3; i < args.Length; i += 2)
				{
					turns.Add((int.Parse(args[i]), int.Parse(args[i + 1])));
				}
				return new WheelSequence(chart, time, pos, turns);
			}
			LeverSequence parseLeverSequence()
			{
				parser.AssertArgCount(minCount: 4);
				parser.AssertArgCount(c => c % 2 == 0, "Lever sequence");
				
				LeftRight dir = parseLeftRight(args[1]);
				List<(int time, LeverPos pos)> levers = new();
				for (int i = 2; i < args.Length; i += 2)
				{
					levers.Add((int.Parse(args[i]), parseLeverPos(args[i+1])));
				}
				return new LeverSequence(chart, dir, levers);
			}
			LeftRight parseLeftRight(string arg)
			{
				return arg.ToUpper() switch
				{
					"L" => LeftRight.Left,
					"R" => LeftRight.Right,
					_ => throw new ArgumentException($"Invalid left/right argument: \"{arg}\""),
				};
			}
			LeverPos parseLeverPos(string arg)
			{
				return arg.ToUpper() switch
				{
					"<" or "L" => LeverPos.Left,
					"^" or "U" => LeverPos.Up,
					">" or "R" => LeverPos.Right,
					"V" or "D" => LeverPos.Down,
					"." or "M" => LeverPos.Middle,
					_ => throw new ArgumentException($"Invalid lever position: \"{arg}\""),
				};
			}
		};

		parser.Run();

		return chart;
	}

	public void Save(string path)
	{
        using var writer = new StreamWriter(path);

		writer.WriteLine("# Metadata");
		// writer.WriteLine($"BPM {chart.BPM}");
		// writer.WriteLine($"Offset {chart.Offset}");
		writer.WriteLine();

		writer.WriteLine("# Notes");

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



// using System.Collections.Generic;
// using UnityEngine;

// public class Chart : INoteContainer<Note>
// {
// 	public delegate void NoteAddedEvent(Note note);
// 	public delegate void NoteRemovedEvent(Note note);

// 	public static event NoteAddedEvent OnNoteAdded;
// 	public static event NoteRemovedEvent OnNoteRemoved;

// 	uint bpm;
//     public uint BPM
//     {
//         get => bpm;
//         set => SaveManager.RegisterChange(ref bpm, value);
//     }

// 	int offset;
//     public int Offset
//     {
//         get => offset;
//         set => SaveManager.RegisterChange(ref offset, value);
//     }
	
//     readonly SortedSet<Note> notes;
// 	public IReadOnlyCollection<Note> Notes => notes;

//     public Note FirstNote => notes.Min;
//     public Note LastNote => notes.Max;


// 	public Chart(uint bpm = 120, int offset = 0)
// 	{
// 		notes = new();
// 		BPM = bpm;
// 		Offset = offset;
// 	}


// 	public int BeatToTime(float beat) => (int) (beat * 60000f / BPM) + Offset;
// 	public float TimeToBeat(int time) => (time - Offset) * BPM / 60000f;

// 	public bool ContainsNote(Note note) => notes.Contains(note);

//     public bool AddNote(Note note, bool notify = true) => SaveManager.ChangeWithNotify(notify, 
// 		() => notes.Add(note), 
// 		() => OnNoteAdded?.Invoke(note)
// 	);

//     public bool RemoveNote(Note note, bool notify = true) => SaveManager.ChangeWithNotify(notify,
// 		() => notes.Remove(note), 
// 		() => OnNoteRemoved?.Invoke(note)
// 	);

// 	// Get all notes that start before or at the given time
// 	public IReadOnlyCollection<Note> GetNotesBefore(int time, bool inclusive = true) 
// 		=> notes.GetViewBetween(DummyNote.Min, new DummyNote(time, inclusive ? int.MaxValue : int.MinValue));
// }