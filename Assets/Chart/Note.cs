using System;
using UnityEngine;

public abstract class Note : IComparable<Note>
{
	static private int currentId;
	
	public readonly int Id;

	public virtual bool AffectsScore => true;
	public virtual int StartTime
	{
		get => throw new NotImplementedException();
		set => throw new NotImplementedException();
	}
	public virtual int EndTime => StartTime;

	protected Note(int id)
	{
		Id = id;
	}

	protected Note() : this(currentId++) { }

	public static void ResetNoteIds()
	{
		currentId = 0;
	}

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
}

public abstract class Note<C, T> : Note
where T : Note
where C : INoteContainer<T>
{
	public readonly C Container;

	protected Note(int id) : base(id) { }

	protected Note(C container) : base()
	{
		Container = container;
	}
}

public abstract class TimedNote<C, T> : Note<C, T>
where T : Note
where C : INoteContainer<T>
{
    int startTime;
    public override int StartTime
    {
        get => startTime;
        set
        {
			if (value != startTime)
			{
				Container.RemoveNoteWithoutNotify((T) (Note) this);
				startTime = value;
				SaveManager.Instance.HasChanges = true;
				Container.AddNoteWithoutNotify((T) (Note) this);
			}
        }
    }


    public TimedNote(int time, int id) : base(id)
    {
        startTime = time;
    }

    public TimedNote(C container, int time) : base(container)
    {
        startTime = time;
    }
}