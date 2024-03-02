
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WheelTurn : Note<WheelSequence, WheelTurn>
{
	public override int StartTime { get; set; } // TODO: use PreviousTurn.EndTime
	public override int EndTime { get; }

	public readonly int StartAngle; // TODO: remove this. calculate in wheel sequence
	public readonly int DeltaAngle;

	public WheelTurn PreviousTurn;
	public WheelTurn NextTurn;

    public WheelTurn(int id) : base(id) { }
	public WheelTurn(WheelSequence seq, int endTime, int startAngle, int deltaAngle) : base(seq)
	{
		EndTime = endTime;
		StartAngle = startAngle;
		DeltaAngle = deltaAngle;
	}

	// Converts an angle between 0-180 to a track position between 0-100
	public static int AngleToPos(int angle) => (int)(angle / 180f * 100);

	// Wraps an angle that may be <0 or >180 so that it is between 0-180
	public static int WrapAngle(int angle) => angle - Mathf.FloorToInt((angle - Math.Sign(angle)) / 180f) * 180;

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
}