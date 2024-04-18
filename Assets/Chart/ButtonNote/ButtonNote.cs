using System;
using UnityEngine;

public class ButtonNote : TimedNote<Chart, Note>
{
	protected int leftPos;
	protected int? rightPos;

	public int LeftPos => leftPos;
	public int? RightPos => rightPos;
	
	public bool IsSingle => RightPos == null;

	public IReadOnlyLeftRightDictionary<int> Pos => new LeftRightGetter<int>(lr => lr switch
	{
		LeftRight.Left => LeftPos,
		LeftRight.Right => RightPos ?? LeftPos,
		_ => throw new Exception($"Button {lr} not supported.")
	});

	public ButtonNote(int time, int id) : base(time, id) {}
	public ButtonNote(Chart chart, int time, int pos1, int? pos2 = null) : base(chart, time)
	{
		// If 2 different positions given, set the left and right positions
		if ((pos2 is int pos2NotNull) && pos1 != pos2)
		{
			leftPos = Mathf.Min(pos1, pos2NotNull);
			rightPos = Mathf.Max(pos1, pos2NotNull);
		}
		else // if only 1 unique position given, only set LeftPos
		{
			leftPos = pos1;
			rightPos = null;
		}
	}
}