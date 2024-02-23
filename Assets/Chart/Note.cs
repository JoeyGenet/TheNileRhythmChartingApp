using System;
using System.Collections;
using System.Collections.Generic;

public abstract class Note : IComparable<Note>
{
	public virtual bool AffectsScore => true;
	
	public readonly int StartTime;
	public virtual int EndTime => StartTime;
	public readonly int Id;
	static private int currentId;

	protected Note(int startTime, int id)
	{
		StartTime = startTime;
		Id = id;
	}

	protected Note(int startTime) : this(startTime, currentId++) { }

	public int CompareTo(Note other)
	{
		if (StartTime == other.StartTime)
		{
			return Id.CompareTo(other.Id);
		}
		else
		{
			return StartTime.CompareTo(other.StartTime);
		}
	}

	public static void ResetNoteIds()
	{
		currentId = 0;
	}
}