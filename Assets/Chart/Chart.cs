using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using UnityEngine;

public class Chart
{
	public uint BPM = 100;
	public int Offset = 0;
	public List<Note> Notes;

	public Chart(List<Note> notes)
	{
		Notes = notes;
		Debug.Log("notes in chart (not including notes inside sequences): " + Notes.Count);
	}

	public Chart() : this(new List<Note>()) { }

	public static Chart ParseChart(TextAsset textAsset)
	{
		List<Note> notes = new();
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
				Func<string[], Note> noteParser = char.ToUpper(args[0][0]) switch
				{
					'0' or 'B' => ParseButton,
					'1' or 'F' => ParseWheelFlick,
					'2' or 'W' => ParseWheelSequence,
					'3' or 'L' => ParseLeverSequence,
					_ => throw new ArgumentException("Invalid note type: \"" + args[0][0] + "\""),
				};
				notes.Add(noteParser(args));
			}
			catch (Exception e)
			{
				throw new ArgumentException("Exception caused by line " + lineNum + " of chart. Error message: " + e.Message);
			}
		}
		// reader.Close();
		notes.Sort();
		return new Chart(notes);
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
        return char.ToUpper(arg[0]) switch
        {
            'L' => LeftRight.Left,
            'R' => LeftRight.Right,
            _ => throw new ArgumentException("Invalid left/right argument: \"" + arg[0] + "\""),
        };
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
        return char.ToUpper(arg[0]) switch
        {
            '<' or 'L' => LeverPos.Left,
            '^' or 'U' => LeverPos.Up,
            '>' or 'R' => LeverPos.Right,
            'V' or 'D' => LeverPos.Down,
            '.' or 'M' => LeverPos.Middle,
            _ => throw new ArgumentException("Invalid lever position: \"" + arg[0] + "\""),
        };
    }
}