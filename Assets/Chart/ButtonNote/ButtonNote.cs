using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OVR.OpenVR;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;

public class ButtonNote : Note
{
	public readonly int LeftPos;
	public readonly int? RightPos;

	private readonly Dictionary<LeftRight, bool> isHit = new Dictionary<LeftRight, bool>
	{
		[LeftRight.Left] = false,
		[LeftRight.Right] = false,
	};

	public ReadOnlyDictionary<LeftRight, bool> IsHit => new ReadOnlyDictionary<LeftRight, bool>(isHit);

	public bool IsAllHit => isHit[LeftRight.Left] && isHit[LeftRight.Right];
	public bool IsAnyHit => isHit[LeftRight.Left] || isHit[LeftRight.Right];
	
	public bool IsSingle => RightPos == null;

	public int GetPosition(LeftRight lr) => lr switch
	{
		LeftRight.Left => LeftPos,
		LeftRight.Right => RightPos ?? LeftPos,
		_ => throw new Exception($"Button {lr} not supported.")
	};

	public ButtonNote(int time, int id) : base(time, id) {}
	public ButtonNote(int time, int pos1, int? pos2 = null) : base(time)
	{
		// If 2 different positions given, set the left and right positions
		if ((pos2 is int pos2NotNull) && pos1 != pos2)
		{
			LeftPos = Mathf.Min(pos1, pos2NotNull);
			RightPos = Mathf.Max(pos1, pos2NotNull);
		}
		else // if only 1 unique position given, only set LeftPos
		{
			LeftPos = pos1;
			RightPos = null;
		}
	}

	public void Hit()
	{
		Assert.IsTrue(IsSingle);
		isHit[LeftRight.Left] = true;
		isHit[LeftRight.Right] = true;
	}
	public void Hit(LeftRight dir)
	{
		Assert.IsFalse(IsSingle);
		isHit[dir] = true;
	}
}
