using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using UnityEngine;

public class Chart
{
	private List<Note> notes;
	public IReadOnlyList<Note> Notes => notes.AsReadOnly();
	private int WheelTurnJudgementInterval;

	public Chart(List<Note> Notes)
	{
		this.notes = Notes;
		Debug.Log("notes in chart: " + notes.Count);
		Debug.Log("Computed notes in chart: " + GetNoteCount());
	}
	public Chart()
	{
		notes = new List<Note>();
	}
	[DoesNotReturn]
	private static void ThrowChartException(int lineNum, Exception e)
	{
		throw new ArgumentException("Exception caused by line " + lineNum + " of chart. Error message: " + e.Message);
	}
	public static Chart ParseChart(TextAsset textAsset)
	{
		// TODO: error catching/throwing
		List<Note> notes = new List<Note>();
		Note.ResetNoteIds();
		// StreamReader reader = new StreamReader(filePath);
		// string line;
		// while ((line = reader.ReadLine()) != null)
		int lineNum = 0;
		foreach (string lineUntrimmed in textAsset.text.Split("\n"))
		{
			lineNum++;
			string line = lineUntrimmed.Trim();
			if (line == "") continue;
			string[] args = Regex.Split(line, " +");
			try
			{
				if (args[0].Length != 1)
				{
					throw new ArgumentException("Invalid note type: \"" + args[0] + "\"");
				}
				switch (args[0][0])
				{
					case '0':
					case 'B':
					case 'b':
						notes.Add(ParseButton(args));
						break;
					case '1':
					case 'F':
					case 'f':
						notes.Add(ParseWheelFlick(args));
						break;
					case '2':
					case 'W':
					case 'w':
						notes.Add(ParseWheelSequence(args));
						break;
					case '3':
					case 'L':
					case 'l':
						notes.Add(ParseLeverSequence(args));
						break;
					default:
						throw new ArgumentException("Invalid note type: \"" + args[0][0] + "\"");
				}
			}
			catch (Exception e)
			{
				ThrowChartException(lineNum, e);
			}
		}
		// reader.Close();
		notes.Sort();
		return new Chart(notes);
	}
	int GetNoteCount()
	{
		int count = 0;
		foreach (Note note in notes)
		{
			count += note switch
			{
				ButtonNote buttonNote => buttonNote.IsSingle ? 1 : 2,
				WheelFlick flick => 1,
				WheelSequence wheelSequence => (wheelSequence.EndTime - wheelSequence.StartTime - 1) / WheelTurnJudgementInterval + 1,
				LeverSequence leverSequence => leverSequence.Notes.Count,
				_ => throw new Exception($"Note {note} should not be in top level"),
			};
		}
		return count;
	}
	private static ButtonNote ParseButton(string[] args)
	{
		if (args.Length < 3 || args.Length > 4)
		{
			throw new ArgumentException("Button note has " + args.Length + " arguments. Button notes must have 3 or 4 arguments.");
		}
		int time = int.Parse(args[1]);
		int pos = int.Parse(args[2]);
		int? pos2 = null;
		if (args.Length == 4)
		{
			pos2 = int.Parse(args[3]);
		}
		return new ButtonNote(time, pos, pos2);
	}
	private static WheelFlick ParseWheelFlick(string[] args)
	{
		if (args.Length != 3)
		{
			throw new ArgumentException("Wheel flick has " + args.Length + " arguments. Wheel flicks must have 3 arguments.");
		}
		int time = int.Parse(args[1]);
		LeftRight dir = ParseLeftRight(args[2]);
		return new WheelFlick(time, dir);
	}
	private static LeftRight ParseLeftRight(string arg)
	{
		if (arg.Length != 1)
		{
			throw new ArgumentException("Invalid left/right argument: \"" + arg + "\"");
		}
		switch (char.ToUpper(arg[0]))
		{
			case 'L':
				return LeftRight.Left;
			case 'R':
				return LeftRight.Right;
			default:
				throw new ArgumentException("Invalid left/right argument: \"" + arg[0] + "\"");
		}
	}
	private static WheelSequence ParseWheelSequence(string[] args)
	{
		if (args.Length < 5 || args.Length % 2 == 0)
		{
			throw new ArgumentException("Wheel sequence has " + args.Length + " arguments. Wheel sequences must have at least 5 arguments and an odd number of arguments.");
		}
		int time = int.Parse(args[1]);
		int pos = int.Parse(args[2]);
		List<int> angles = new List<int>();
		List<int> endTimes = new List<int>();
		for (int i = 3; i < args.Length; i += 2)
		{
			angles.Add(int.Parse(args[i]));
			endTimes.Add(int.Parse(args[i + 1]));
		}
		return new WheelSequence(time, pos, angles, endTimes);
	}
	private static LeverSequence ParseLeverSequence(string[] args)
	{
		if (args.Length < 4 || args.Length % 2 == 1)
		{
			throw new ArgumentException("Lever sequence has " + args.Length + " arguments. Lever sequences must have at least 4 arguments and an even number of arguments.");
		}
		LeftRight lever = ParseLeftRight(args[1]);
		int time = int.Parse(args[2]);
		List<int> times = new List<int>();
		List<LeverPos> positions = new List<LeverPos>();
		for (int i = 2; i < args.Length; i += 2)
		{
			times.Add(int.Parse(args[i]));
			positions.Add(ParseLeverPos(args[i + 1]));
		}
		return new LeverSequence(lever, time, times, positions);
	}
	private static LeverPos ParseLeverPos(string arg)
	{
		if (arg.Length > 1)
		{
			throw new ArgumentException("Invalid lever position: \"" + arg + "\"");
		}
		switch (arg[0])
		{
			case '<':
			case 'L':
			case 'l':
				return LeverPos.Left;
			case '^':
			case 'U':
			case 'u':
				return LeverPos.Up;
			case '>':
			case 'R':
			case 'r':
				return LeverPos.Right;
			case 'v':
			case 'V':
			case 'D':
			case 'd':
				return LeverPos.Down;
			case '.':
			case 'm':
			case 'M':
				return LeverPos.Middle;
			default:
				throw new ArgumentException("Invalid lever position: \"" + arg[0] + "\"");
		}
	}

	public void RemoveNotes(ISet<Note> notesToRemove)
	{
		notes.RemoveAll(note => notesToRemove.Contains(note));
		foreach (Note note in notesToRemove)
		{
			// GameManager.Instance.HandleNoteHit(note, JudgementGrade.Perfect);
			// GameManager.Instance.HandleNoteCompleted(note);
		}
	}
}