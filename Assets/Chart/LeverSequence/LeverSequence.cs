using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class LeverSequence : Note<Chart, Note>, INoteContainer<LeverNote>
{
	public delegate void LeverNoteAddedEvent(LeverSequence sequence, LeverNote note);
	public delegate void LeverNoteRemovedEvent(LeverSequence sequence, LeverNote note);

	public static event LeverNoteAddedEvent OnLeverNoteAdded;
	public static event LeverNoteAddedEvent OnLeverNoteRemoved;

	public override bool AffectsScore => false;

	LeftRight dir;
	public LeftRight Dir
	{
		get => dir;
		set => SaveManager.RegisterChange(ref dir, value);
	}

    readonly SortedSet<LeverNote> notes;
	public IReadOnlyCollection<LeverNote> Notes => notes;

	public override int StartTime => notes.Min.StartTime;
	public override int EndTime => notes.Max.EndTime;

	public LeverNote FirstNote => notes.Min;
	public LeverNote LastNote => notes.Max;

    public LeverSequence(int time, int id) : base(id)
	{
		notes = new() { new LeverNote(time, -1) };
	}

	public LeverSequence(Chart container, LeftRight dir, List<(int time, LeverPos pos)> levers) : 
		base(container)
	{
		this.dir = dir;
		notes = new();
		foreach (var (time, pos) in levers)
		{
			notes.Add(new LeverNote(this, time, pos));
		}
	}

    public bool AddNoteWithoutNotify(LeverNote note) => notes.Add(note);
    public bool RemoveNoteWithoutNotify(LeverNote note) => notes.Remove(note);
	public bool ContainsNote(LeverNote note) => notes.Contains(note);

    void INoteContainer<LeverNote>.OnNoteAdded(LeverNote note)
    {
		SaveManager.Instance.HasChanges = true;
		OnLeverNoteAdded?.Invoke(this, note);
    }

    void INoteContainer<LeverNote>.OnNoteRemoved(LeverNote note)
    {
		SaveManager.Instance.HasChanges = true;
		OnLeverNoteRemoved?.Invoke(this, note);

		// Remove this entire sequence if last lever note was removed
		if (notes.Count == 0)
		{
			SaveManager.Instance.Chart.RemoveNote(this);
		}
    }
}
