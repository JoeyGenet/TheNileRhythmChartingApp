
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WheelTurn : Note<WheelSequence, WheelTurn>, IComparable<WheelTurn>
{
	public override int StartTime => PreviousTurn?.EndTime ?? Container.StartTime;

	int endTime;
	public override int EndTime
	{
		get => endTime;
        set
        {
			if (value != endTime)
			{	// Changing end time changes the sorting, so we remove and add back to the container
				Debug.Log($"Endtime change remove {Container.RemoveNote(this, notify: false)}");
				endTime = value;
				SaveManager.Instance.HasChanges = true;
				Debug.Log($"Endtime change add {Container.AddNote(this, notify: true)}");
				Debug.Log(Container);
			}
        }
	}

	int deltaAngle;
	public int DeltaAngle
	{
		get => deltaAngle;
		set
		{
			if (SaveManager.RegisterChange(ref deltaAngle, value))
			{
				NextTurn?.RecomputeStartAngle();
			}
		}
	}

	public int StartAngle { get; private set; } // Cached start angle

	public WheelTurn PreviousTurn;
	public WheelTurn NextTurn;

    public WheelTurn(int id) : base(id) { }
	public WheelTurn(WheelSequence seq, int endTime, int deltaAngle) : base(seq)
	{
		this.endTime = endTime;
		this.deltaAngle = deltaAngle;
	}

	// Converts an angle between 0-180 to a track position between 0-100
	public static int AngleToPos(int angle) => angle * 100 / 180;

	// Converts an angle between 0-180 to a track position between 0-100
	public static int PosToAngle(int pos) => pos * 180 / 100;

	// Wraps an angle that may be <0 or >180 so that it is between 0-180
	public static int WrapAngle(int angle) => angle - Mathf.FloorToInt((angle - Math.Sign(angle)) / 180f) * 180;

	// Wheel turns are sorted by end time, because start time is not always available
	public int CompareTo(WheelTurn other)
	{
		// Debug.Log($"Compare {other}");
		if (EndTime == other.EndTime)
		{
			return Id.CompareTo(other.Id);
		}
		else
		{
			return EndTime.CompareTo(other.EndTime);
		}
	}

	// Recomputes the cached start angle from the previous angle
	// Also tells the next turn to recompute if they need to recompute
	public void RecomputeStartAngle()
	{
		if (PreviousTurn != null)
		{
			StartAngle = PreviousTurn.StartAngle + PreviousTurn.DeltaAngle;
		}
		else
		{
			StartAngle = PosToAngle(Container.Pos);
		}
		if (NextTurn != null && NextTurn.StartAngle != StartAngle + DeltaAngle)
		{
			NextTurn.RecomputeStartAngle();
		}
	}

    // Get the lines required to render this turn, taking into account wraps
    public List<(int pos1, int time1, int pos2, int time2)> ComputeLines()
	{
		List<(int pos1, int time1, int pos2, int time2)> lines = new();

		int prevAngle = WrapAngle(StartAngle); // angle of start of current line (0-180)
		int prevTime = StartTime; // time of start of current line
		int remainingTurn = DeltaAngle; // remaining angle to turn (can be >180 or <-180)
		do
		{
			int endAngle = prevAngle + remainingTurn; // angle where current line ends if no wrapping
			int nextAngle = Math.Clamp(endAngle, 0, 180); // angle of end of current line (0-180)
			remainingTurn -= nextAngle-prevAngle; // subtract delta angle of current line
			float frac = DeltaAngle == 0
				? 1
				: (float) (DeltaAngle - remainingTurn) / DeltaAngle; // Fraction of turn passed after end of current line
			int nextTime = (int) ((EndTime - StartTime) * frac + StartTime); // time of end of current line

			if (nextTime > prevTime) // Sometimes, we calculate a zero length line, so account for that
			{						 // example: start at 0, then go -90 degrees
				lines.Add((AngleToPos(prevAngle), prevTime, AngleToPos(nextAngle), nextTime));
			}
			
			prevAngle = 180 - nextAngle; // wrap start angle of next line around
			prevTime = nextTime; // next line starts when current line ends
		}
		while (remainingTurn != 0);
		
		return lines;
	}

    public override string ToString() => $"WheelTurn<time {StartTime}-{EndTime}, angle {StartAngle}, delta {DeltaAngle}>";
}

public class DummyWheelTurn : WheelTurn
{
	public static DummyWheelTurn Min = new(int.MinValue, int.MinValue);
	public static DummyWheelTurn Max = new(int.MaxValue, int.MaxValue);

    readonly int startTime;
	public override int StartTime => startTime;

    public DummyWheelTurn(int startTime, int id) : base(id)
	{
		this.startTime = startTime;
	}
}