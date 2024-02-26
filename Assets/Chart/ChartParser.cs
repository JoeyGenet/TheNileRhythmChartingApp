using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public class ChartParser
{
	uint bpm = 100;
	int offset = 0;
	readonly List<Note> notes = new();

    readonly StreamReader reader;
	uint lineNum = 0; // 0 == haven't started parsing yet
	string line; // Current line
	string[] args; // Args of the current line

	public ChartParser(string filepath)
	{
		reader = new(filepath);
	}

	public ChartParser(TextAsset textAsset)
	{
		Stream stream = new MemoryStream();
		reader = new(stream);
		stream.Write(Encoding.Unicode.GetBytes(textAsset.text));
	}

	public Chart Run()
	{
		Note.ResetNoteIds();

		while ((line = reader.ReadLine()) != null)
		{
			lineNum++;
			line = line.Trim();
			if (line == "" || line.StartsWith('#')) continue;
			args = Regex.Split(line, " +");

			try
			{
				switch (args[0].ToLower())
				{
					case "bpm":
						AssertArgCount(2, "BPM");
						bpm = uint.Parse(args[1]);
						break;
					case "offset":
						AssertArgCount(2, "Offset");
						offset = int.Parse(args[1]);
						break;
					default:
						notes.Add(ParseNote());
						break;
				}
			}
			catch (Exception e)
			{
				throw new ArgumentException($"Exception caused by line {lineNum} of chart. Error message: {e.Message}");
			}
		}
		reader.Close();

		notes.Sort();
		return new Chart(notes, bpm, offset);
	}


	void Assert(bool invariant, string message)
	{
		if (!invariant) throw new ArgumentException(message);
	}
	void AssertArgCount(bool invariant, string name, string expected) => 
		Assert(invariant, $"{name} has {args.Length} arguments. Expected {expected} arguments.");

	void AssertArgCount(int count, string name) => 
		AssertArgCount(args.Length == count, name, $"{count}");

	void AssertArgCount(int? minCount, int? maxCount, string name)
	{
		int count = args.Length;
		if (maxCount == null && minCount == null) throw new ArgumentException("minCount and maxCount cannot both be null");
		if (minCount == null) AssertArgCount(count <= maxCount, name, $"{maxCount} or fewer");
		if (maxCount == null) AssertArgCount(count >= minCount, name, $"at least {minCount}");
		else AssertArgCount(count >= minCount && count <= maxCount, name, $"{minCount} to {maxCount}");
	}
	void AssertIsCharacter(string str, string message) => Assert(str.Length == 1, message);
	

	Note ParseNote()
	{
		string errorMsg = $"Invalid note type: \"{args[0]}\"";
		AssertIsCharacter(args[0], errorMsg);
		Func<Note> noteParser = char.ToUpper(args[0][0]) switch
		{
			'0' or 'B' => ParseButton,
			'1' or 'F' => ParseWheelFlick,
			'2' or 'W' => ParseWheelSequence,
			'3' or 'L' => ParseLeverSequence,
			_ => throw new ArgumentException(errorMsg),
		};
		return noteParser();
	}
	ButtonNote ParseButton()
	{
		AssertArgCount(3, 4, "Button note");
		
		int time = int.Parse(args[1]);
		int pos = int.Parse(args[2]);
		int? pos2 = null;
		if (args.Length == 4)
		{
			pos2 = int.Parse(args[3]);
		}
		return new ButtonNote(time, pos, pos2);
	}
	WheelFlick ParseWheelFlick()
	{
		AssertArgCount(3, "Wheel flick");

		int time = int.Parse(args[1]);
		LeftRight dir = ParseLeftRight(args[2]);
		return new WheelFlick(time, dir);
	}
	LeftRight ParseLeftRight(string arg)
	{
		string errorMsg = $"Invalid left/right argument: \"{arg}\"";
		AssertIsCharacter(arg, errorMsg);
        return char.ToUpper(arg[0]) switch
        {
            'L' => LeftRight.Left,
            'R' => LeftRight.Right,
            _ => throw new ArgumentException(errorMsg),
        };
    }
	WheelSequence ParseWheelSequence()
	{
		AssertArgCount(5, null, "Wheel sequence");
		AssertArgCount(args.Length % 2 == 1, "Wheel sequence", "an odd number of");

		int time = int.Parse(args[1]);
		int pos = int.Parse(args[2]);
		List<int> angles = new();
		List<int> endTimes = new();
		for (int i = 3; i < args.Length; i += 2)
		{
			angles.Add(int.Parse(args[i]));
			endTimes.Add(int.Parse(args[i + 1]));
		}
		return new WheelSequence(time, pos, angles, endTimes);
	}
	LeverSequence ParseLeverSequence()
	{
		AssertArgCount(4, null, "Lever sequence");
		AssertArgCount(args.Length % 2 == 0, "Lever sequence", "an even number of");
		
		LeftRight lever = ParseLeftRight(args[1]);
		int time = int.Parse(args[2]);
		List<int> times = new();
		List<LeverPos> positions = new();
		for (int i = 2; i < args.Length; i += 2)
		{
			times.Add(int.Parse(args[i]));
			positions.Add(ParseLeverPos(args[i + 1]));
		}
		return new LeverSequence(lever, time, times, positions);
	}
	LeverPos ParseLeverPos(string arg)
	{
		string errorMsg = $"Invalid lever position: \"{arg}\"";
		AssertIsCharacter(arg, errorMsg);
        return char.ToUpper(arg[0]) switch
        {
            '<' or 'L' => LeverPos.Left,
            '^' or 'U' => LeverPos.Up,
            '>' or 'R' => LeverPos.Right,
            'V' or 'D' => LeverPos.Down,
            '.' or 'M' => LeverPos.Middle,
            _ => throw new ArgumentException(errorMsg),
        };
    }
}