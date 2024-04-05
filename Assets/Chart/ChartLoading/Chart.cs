using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class Chart : INoteContainer<Note>
{
	public readonly ChartPackage Package;

    protected readonly SortedSet<Note> notes = new();
	public IReadOnlyCollection<Note> Notes => notes;

    public Note FirstNote => notes.Min;
    public Note LastNote => notes.Max;
	
	protected int difficultyRating = -1;
	protected string difficultyName = "";

	public int DifficultyRating => difficultyRating;
	public string DifficultyName => difficultyName;


	public bool AddNote(Note note) => notes.Add(note);


	public Chart(ChartPackage package)
	{
		Package = package;
	}

	public Chart(ChartPackage package, Stream stream) : this(package)
	{
        var parser = new FileParser();

		parser["Difficulty"] = (line, args) => parser.KeyValue(new()
		{
			["Rating"] = val => difficultyRating = int.Parse(val),
			["Name"] = val => difficultyName = val,
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
			AddNote(noteParser());

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
				return new ButtonNote(this, time, pos, pos2);
			}
			WheelFlick parseWheelFlick()
			{
				parser.AssertArgCount(3);

				int time = int.Parse(args[1]);
				LeftRight dir = parseLeftRight(args[2]);
				return new WheelFlick(this, time, dir);
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
				return new WheelSequence(this, time, pos, turns);
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
				return new LeverSequence(this, dir, levers);
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

		parser.Run(stream);
	}
}