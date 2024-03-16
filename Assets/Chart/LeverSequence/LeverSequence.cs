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

    public LeverNote FirstNote => notes.Min;
    public LeverNote LastNote => notes.Max;

	public override int StartTime => notes.Min.StartTime;
	public override int EndTime => notes.Max.EndTime;

    public LeverSequence(int time, int id) : base(id)
	{
		notes = new() { new LeverNote(time, -1) };
	}

	public LeverSequence(Chart container, LeftRight dir, List<(int time, LeverPos pos)> levers) : base(container)
	{
		this.dir = dir;
		notes = new();
		foreach (var (time, pos) in levers)
		{
			notes.Add(new LeverNote(this, time, pos));
		}

		LeverNote prevNote = null;
		foreach (var note in notes)
		{
			note.PreviousNote = prevNote;
			if (prevNote != null) prevNote.NextNote = note;
			prevNote = note;
		}
	}

    // public bool AddNoteWithoutNotify(LeverNote note) => notes.Add(note);
    // public bool RemoveNoteWithoutNotify(LeverNote note) => notes.Remove(note);
	public bool ContainsNote(LeverNote note) => notes.Contains(note);

    public bool AddNote(LeverNote note, bool notify = true) => SaveManager.ChangeWithNotify(notify,
		() => notes.Add(note),
		() => OnLeverNoteAdded?.Invoke(this, note)
	);

    public bool RemoveNote(LeverNote note, bool notify = true) => SaveManager.ChangeWithNotify(notify,
		() =>
		{
			// If last note, just remove the entire sequence
			if (notes.Count == 1 && notes.Contains(note))
			{
				Container.RemoveNote(this);
				return true;
			}
			return notes.Remove(note);
		},
		() => OnLeverNoteRemoved?.Invoke(this, note)
	);
}
