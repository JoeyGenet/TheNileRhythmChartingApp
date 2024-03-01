using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;

public class WheelFlick : TimedNote<Chart, Note>
{
	LeftRight dir;
	public LeftRight Dir
	{
		get => dir;
		set => SaveManager.RegisterChange(ref dir, value);
	}

	public WheelFlick(int time, int id) : base(time, id) {}
	public WheelFlick(Chart chart, int time, LeftRight dir) : base(chart, time)
	{
		this.dir = dir;
	}
}
