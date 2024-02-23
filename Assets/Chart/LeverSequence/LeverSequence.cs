using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

public class LeverSequence : Note
{
	public class LeverNote : Note
	{
		public readonly LeverPos pos;
		public readonly LeverPos previousPos;
		public LeverNote(int time, LeverPos pos, LeverPos previousPos) : base(time)
		{
			this.pos = pos;
			this.previousPos = previousPos;
		}
	}

	public override bool AffectsScore => false;

	public readonly LeftRight Dir;
	public readonly SortedSet<LeverNote> Notes;

	public override int EndTime { get; }

	public LeverSequence(int time, int id) : base(time, id) { }

	public LeverSequence(LeftRight dir, int time, List<int> times, List<LeverPos> positions) : base(time)
	{
		Dir = dir;
		Notes = new SortedSet<LeverNote>();
		LeverPos previousPos = LeverPos.Middle;
		for (int i = 0; i < times.Count; i++)
		{
			Notes.Add(new LeverNote(times[i], positions[i], previousPos));
			previousPos = positions[i];
		}
		EndTime = Notes.Max.EndTime;
	}
}
