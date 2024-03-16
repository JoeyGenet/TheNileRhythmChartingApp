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
	public virtual int EndTime
	{
		get => StartTime;
		set => throw new NotImplementedException();
	}
		

	protected Note(int id)
	{
		Id = id;
	}

	protected Note() : this(currentId++) { }

	public static void ResetNoteIds()
	{
		currentId = 0;
	}

	public virtual int CompareTo(Note other)
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
				Container.RemoveNote((T) (Note) this, notify: false);
				startTime = value;
				SaveManager.Instance.HasChanges = true;
				Container.AddNote((T) (Note) this, notify: false);
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