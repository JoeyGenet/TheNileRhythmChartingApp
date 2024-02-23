using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;
public class WheelFlick : Note
{
	public readonly LeftRight Dir;

	public WheelFlick(int time, int id) : base(time, id) {}
	
	public WheelFlick(int time, LeftRight dir) : base(time)
	{
		Dir = dir;
	}
}
