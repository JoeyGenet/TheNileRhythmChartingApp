using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;


public class EditableButtonNote : ButtonNote, IEditableNote<EditableChart, Note>
{
	public new EditableChart Container => base.Container as EditableChart;

    public bool HasChanges => false;
    public void SetChanged() => Container.SetChanged();

	public new int StartTime
	{
		get => time;
		set => this.RegisterSortingChange(Container, ref time, value);
	}
	public new int LeftPos
	{
		get => leftPos;
		set => this.RegisterChange(ref leftPos, value);
	}
	public new int? RightPos
	{
		get => rightPos;
		set => this.RegisterChange(ref rightPos, value);
	}

	public EditableButtonNote(EditableChart chart, int time, int pos1, int? pos2 = null)
		: base(chart, time, pos1, pos2) {}
	
	public EditableButtonNote(EditableChart chart, ButtonNote note)
		: base(chart, note.StartTime, note.LeftPos, note.RightPos) {}
}


public class EditableWheelFlick : WheelFlick, IEditableNote<EditableChart, Note>
{
	public new EditableChart Container => base.Container as EditableChart;

    public bool HasChanges => false;
    public void SetChanged() => Container.SetChanged();

	public new int StartTime
	{
		get => time;
		set => this.RegisterSortingChange(Container, ref time, value);
	}
	public new LeftRight Dir
	{
		get => dir;
		set => this.RegisterChange(ref dir, value);
	}

	public EditableWheelFlick(EditableChart chart, int time, LeftRight dir) : base(chart, time, dir) {}

	public EditableWheelFlick(EditableChart chart, WheelFlick note)
		: base(chart, note.StartTime, note.Dir) {}
}


public class EditableWheelSequence : WheelSequence, IEditableNote<EditableChart, Note>, IEditableNoteContainer<EditableWheelTurn>
{
	public new EditableChart Container => base.Container as EditableChart;

    public bool HasChanges => false;
    public void SetChanged() => Container.SetChanged();

    public event NoteAddedEvent<EditableWheelTurn> OnNoteAdded;
    public event NoteRemovedEvent<EditableWheelTurn> OnNoteRemoved;
    public event NoteMovedEvent<EditableWheelTurn> OnNoteMoved;

    public new int StartTime
	{
		get => time;
		set => this.RegisterSortingChange(Container, ref time, value);
	}
	public new int Pos
	{
		get => pos;
		set => this.RegisterChange(ref pos, value, onChanged: () => notes.Min.RecomputeStartAngle());
	}

    public new IReadOnlySortedSet<EditableWheelTurn> Notes => (ReadOnlySortedSet<EditableWheelTurn, WheelTurn>) notes;

    // public new EditableWheelTurn FirstNote => notes.Min as EditableWheelTurn;
    // public new EditableWheelTurn LastNote => notes.Max as EditableWheelTurn;

	public EditableWheelSequence(EditableChart chart, int time, int pos, List<(int deltaAngle, int endTime)> turns)
		: base(chart, time, pos, new())
	{
		InitializeNotes(turns.Select(turn => new EditableWheelTurn(this, turn.endTime, turn.deltaAngle)));
	}
	public EditableWheelSequence(EditableChart chart, WheelSequence seq)
		: base(chart, seq.StartTime, seq.Pos, new())
	{
		InitializeNotes(seq.Notes.Select(turn => new EditableWheelTurn(this, turn.EndTime, turn.DeltaAngle)));
	}

	public bool AddNote(EditableWheelTurn note, bool notify = true)
	{
		// track undo/redoable note add action
		if (notify)
		{
			AddNoteToWheelSequenceAction addNoteToWheelSequenceAction = new AddNoteToWheelSequenceAction(note, this);
			addNoteToWheelSequenceAction.AddAction();
		}
		return AddNoteToSequence(note, notify);
	}
	// when redoing note add or undoing note remove, only this is called
	public bool AddNoteToSequence(EditableWheelTurn note, bool notify = true) => this.RegisterChange(
		() =>
		{
			if (notes.Contains(note)) return false;

			// Note will be inserted between these 2 notes:
			EditableWheelTurn prevTurn = (EditableWheelTurn) notes.GetViewBetween(DummyWheelTurn.Min, note).Max;
			EditableWheelTurn nextTurn = prevTurn == null ? (EditableWheelTurn) notes.Min : prevTurn.NextTurn;

			// Update linked list
			if (prevTurn != null) prevTurn.NextTurn = note;
			if (nextTurn != null) nextTurn.PreviousTurn = note;
			note.NextTurn = nextTurn;
			note.PreviousTurn = prevTurn;

			if (note.NextTurn != null) note.NextTurn.DeltaAngle -= note.DeltaAngle;
			note.RecomputeStartAngle();

			Assert.IsTrue(notes.Add(note));
			return notify;
		},
		onChanged: () => OnNoteAdded?.Invoke(note)
	);

	public bool RemoveNote(EditableWheelTurn note, bool notify = true)
	{
		// track undo/redoable note remove action
		if (notify)
		{
			RemoveNoteFromWheelSequenceAction removeNoteFromWheelSequenceAction = new RemoveNoteFromWheelSequenceAction(note, this);
			removeNoteFromWheelSequenceAction.AddAction();
		}
		return RemoveNoteFromSequence(note, notify);
	}
	// when redoing note remove or undoing note add, only this is called
	public bool RemoveNoteFromSequence(EditableWheelTurn note, bool notify = true) => this.RegisterChange(
		() =>
		{
			if (!notes.Contains(note)) return false;

			// Update linked list
			if (note.PreviousTurn != null) note.PreviousTurn.NextTurn = note.NextTurn;
			if (note.NextTurn != null)
			{
				note.NextTurn.PreviousTurn = note.PreviousTurn;
				note.NextTurn.DeltaAngle += note.DeltaAngle;
				note.NextTurn.RecomputeStartAngle();
			}

			note.PreviousTurn = note.NextTurn = null;

			Assert.IsTrue(notes.Remove(note));
			return notify;
		},
		onChanged: () => OnNoteRemoved?.Invoke(note)
	);

	public void NotifyMoved(EditableWheelTurn note) => OnNoteMoved?.Invoke(note);

    // public bool UpdateNote(EditableWheelTurn note) => this.RegisterChange(
	// 	() => RemoveNoteWithoutNotify(note) && AddNoteWithoutNotify(note),
	// 	onChanged: () => OnNoteMoved?.Invoke(note)
	// );
	
	public bool ContainsNote(EditableWheelTurn note) => notes.Contains(note);
}


public class EditableWheelTurn : WheelTurn, IEditableNote<EditableWheelSequence, EditableWheelTurn>
{
	public new EditableWheelSequence Container => base.Container as EditableWheelSequence;

    public bool HasChanges => false;
    public void SetChanged() => Container.SetChanged();

	public new int EndTime
	{
		get => endTime;
        set => this.RegisterSortingChange(Container, ref endTime, value);
	}

	public new int DeltaAngle
	{
		get => deltaAngle;
		set => this.RegisterChange(ref deltaAngle, value, onChanged: () => NextTurn?.RecomputeStartAngle());
	}

	public new EditableWheelTurn PreviousTurn
	{
		get => base.PreviousTurn as EditableWheelTurn;
		set => base.PreviousTurn = value;
	}
	public new EditableWheelTurn NextTurn
	{
		get => base.NextTurn as EditableWheelTurn;
		set => base.NextTurn = value;
	}

	public EditableWheelTurn(EditableWheelSequence seq, int endTime, int deltaAngle) : base(seq, endTime, deltaAngle) {}
}



public class EditableLeverSequence : LeverSequence, IEditableNote<EditableChart, Note>, IEditableNoteContainer<EditableLeverNote>
{
	public new EditableChart Container => base.Container as EditableChart;

    public bool HasChanges => false;
    public void SetChanged() => Container.SetChanged();

    public event NoteAddedEvent<EditableLeverNote> OnNoteAdded;
    public event NoteRemovedEvent<EditableLeverNote> OnNoteRemoved;
    public event NoteMovedEvent<EditableLeverNote> OnNoteMoved;

    public new LeftRight Dir
	{
		get => dir;
		set => this.RegisterChange(ref dir, value);
	}

	public new IReadOnlySortedSet<EditableLeverNote> Notes => (ReadOnlySortedSet<EditableLeverNote, LeverNote>) notes;

    // public new EditableLeverNote FirstNote => notes.Min as EditableLeverNote;
    // public new EditableLeverNote LastNote => notes.Max as EditableLeverNote;

	public EditableLeverSequence(EditableChart chart, LeftRight dir, List<(int time, LeverPos pos)> levers) 
		: base(chart, dir, new())
	{
		InitializeNotes(levers.Select(lever => new EditableLeverNote(this, lever.time, lever.pos)));
	}
	public EditableLeverSequence(EditableChart chart, LeverSequence seq)
		: base(chart, seq.Dir, new())
	{
		InitializeNotes(seq.Notes.Select(lever => new EditableLeverNote(this, lever.StartTime, lever.Pos)));
	}
		
	public bool ContainsNote(EditableLeverNote note) => notes.Contains(note);

	// TODO: update linked list
	// TODO: register sorting change when adding/removing a lever note caused start time to change
    public bool AddNote(EditableLeverNote note, bool notify = true) => this.RegisterChange(
		() =>
		{
			if (notes.Contains(note)) return false;

			// Note will be inserted between these 2 notes:
			EditableLeverNote prevNote = (EditableLeverNote) notes.GetViewBetween(DummyLeverNote.Min, note).Max;
			EditableLeverNote nextNote = prevNote == null ? (EditableLeverNote) notes.Min : prevNote.NextNote;

			// Update linked list
			if (prevNote != null) prevNote.NextNote = note;
			if (nextNote != null) nextNote.PreviousNote = note;
			note.NextNote = nextNote;
			note.PreviousNote = prevNote;

			Assert.IsTrue(notes.Add(note));
			return notify;
		},
		onChanged: () => OnNoteAdded?.Invoke(note)
	);

    public bool RemoveNote(EditableLeverNote note, bool notify = true) => this.RegisterChange(
		() => 
		{
			if (!notes.Contains(note)) return false;

			// Update linked list
			if (note.PreviousNote != null) note.PreviousNote.NextNote = note.NextNote;
			if (note.NextNote != null) note.NextNote.PreviousNote = note.PreviousNote;

			note.PreviousNote = note.NextNote = null;

			Assert.IsTrue(notes.Remove(note));
			return notify;
		},
		onChanged: () => OnNoteRemoved?.Invoke(note)
	);

	public void NotifyMoved(EditableLeverNote note) => OnNoteMoved?.Invoke(note);

    // public bool UpdateNote(EditableLeverNote note) => this.RegisterChange(
	// 	() => RemoveNoteWithoutNotify(note) && AddNoteWithoutNotify(note),
	// 	onChanged: () => OnNoteMoved?.Invoke(note)
	// );
}


public class EditableLeverNote : LeverNote, IEditableNote<EditableLeverSequence, EditableLeverNote>
{
	public new EditableLeverSequence Container => base.Container as EditableLeverSequence;

    public bool HasChanges => false;
    public void SetChanged() => Container.SetChanged();

	public new int StartTime
	{
		get => time;
		set => this.RegisterSortingChange(Container, ref time, value);
	}
    public new LeverPos Pos
    {
        get => pos;
        set => this.RegisterChange(ref pos, value);
    }

	public new EditableLeverNote PreviousNote
	{
		get => base.PreviousNote as EditableLeverNote;
		set => base.PreviousNote = value;
	}
	public new EditableLeverNote NextNote
	{
		get => base.NextNote as EditableLeverNote;
		set => base.NextNote = value;
	}

    public EditableLeverNote(EditableLeverSequence seq, int time, LeverPos pos) : base(seq, time, pos) {}
}
