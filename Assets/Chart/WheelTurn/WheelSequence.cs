using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;

public class WheelSequence : Note
{
	public class WheelTurn : Note
	{
		public override int EndTime { get; }

		public readonly int startAngle;
		public readonly int deltaAngle;
		public WheelTurn(int startTime, int endTime, int startAngle, int deltaAngle) : base(startTime)
		{
			EndTime = endTime;
			this.startAngle = startAngle;
			this.deltaAngle = deltaAngle;
		}
	}

	public override bool AffectsScore => false;

	public readonly SortedSet<WheelTurn> Turns;
	public readonly int Pos;

	public override int EndTime {get;}

	public WheelSequence(int time, int id) : base(time, id) { }

	public WheelSequence(int time, int pos, List<int> angles, List<int> endTimes) : base(time)
	{
		Pos = pos;
		Turns = new SortedSet<WheelTurn>();
		int prevTime = time;
		int prevAngle = pos * 180 / 100;
		for (int i = 0; i < angles.Count; i++)
		{
			Turns.Add(new WheelTurn(prevTime, endTimes[i], prevAngle, angles[i]));
			prevTime = endTimes[i];
			prevAngle += angles[i];
		}
		EndTime = Turns.Max.EndTime;
	}
}
