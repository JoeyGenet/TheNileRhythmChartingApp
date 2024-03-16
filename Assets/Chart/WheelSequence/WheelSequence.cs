using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class WheelSequence : TimedNote<Chart, Note>, INoteContainer<WheelTurn>
{
	public delegate void WheelTurnAddedEvent(WheelSequence sequence, WheelTurn note);
	public delegate void WheelTurnRemovedEvent(WheelSequence sequence, WheelTurn note);

	public static event WheelTurnAddedEvent OnWheelTurnAdded;
	public static event WheelTurnAddedEvent OnWheelTurnRemoved;

	public override bool AffectsScore => false;

	int pos;
	public int Pos
	{
		get => pos;
		set
		{
			if (SaveManager.RegisterChange(ref pos, value))
			{
				notes.Min.RecomputeStartAngle();
			}
		}
	}

	readonly SortedSet<WheelTurn> notes;
    public IReadOnlyCollection<WheelTurn> Notes => notes;

	public override int EndTime => notes.Max.EndTime;

    public WheelTurn FirstNote => notes.Min;
    public WheelTurn LastNote => notes.Max;

    public WheelSequence(int time, int id) : base(time, id) { }

	public WheelSequence(Chart chart, int time, int pos, List<(int deltaAngle, int endTime)> turns) : base(chart, time)
	{
		this.pos = pos;
		notes = new SortedSet<WheelTurn>();
		foreach (var (deltaAngle, endTime) in turns)
		{
			notes.Add(new(this, endTime, deltaAngle));
		}

		WheelTurn prevTurn = null;
		foreach (var turn in notes)
		{
			turn.PreviousTurn = prevTurn;
			if (prevTurn != null) prevTurn.NextTurn = turn;
			turn.RecomputeStartAngle();
			prevTurn = turn;
		}
	}

	public bool AddNote(WheelTurn note, bool notify = true) => SaveManager.ChangeWithNotify(notify,
		() =>
		{
			// Note will be inserted between these 2 notes:
			WheelTurn prevTurn = notes.GetViewBetween(DummyWheelTurn.Min, note).Max;
			WheelTurn nextTurn = prevTurn == null ? notes.Min : prevTurn.NextTurn;

			// Update linked list
			if (prevTurn != null) prevTurn.NextTurn = note;
			if (nextTurn != null) nextTurn.PreviousTurn = note;
			note.NextTurn = nextTurn;
			note.PreviousTurn = prevTurn;

			note.RecomputeStartAngle();

			return notes.Add(note);
		},
		() => OnWheelTurnAdded?.Invoke(this, note)
	);

	public bool RemoveNote(WheelTurn note, bool notify = true) => SaveManager.ChangeWithNotify(notify,
		() =>
		{
			// If last note, just remove the entire sequence
			if (notes.Count == 1 && notes.Contains(note))
			{
				Container.RemoveNote(this);
				return true;
			}

			// Update linked list
			if (note.PreviousTurn != null) note.PreviousTurn.NextTurn = note.NextTurn;
			if (note.NextTurn != null)
			{
				note.NextTurn.PreviousTurn = note.PreviousTurn;
				note.NextTurn.RecomputeStartAngle();
			}

			note.PreviousTurn = note.NextTurn = null;

			return notes.Remove(note);
		},
		() => OnWheelTurnRemoved?.Invoke(this, note)
	);
	
	public bool ContainsNote(WheelTurn note) => notes.Contains(note);

	// Get the lines required to render this sequence, taking into account wraps
	public List<List<(int pos, int time)>> ComputeLines()
	{
		List<List<(int pos, int time)>> lines = new();
		int lastPos = -1;
		int lastTime = -1;
		foreach (WheelTurn note in Notes)
		{
			foreach (var (pos1, time1, pos2, time2) in note.ComputeLines())
			{
				// Debug.Log($"{note.Id} {pos1} {time1} {pos2} {time2}");
				if (pos1 != lastPos || time1 != lastTime)
				{	// If curr start position is different, start a new line sequence
					lines.Add(new List<(int pos, int time)> { (pos1, time1) });
				}
				lines.Last().Add((pos2, time2));
				lastPos = pos2;
				lastTime = time2;
			}
		}
		// foreach (var line in lines) Debug.Log(string.Join(", ", line));
		return lines;
	}

    public override string ToString() => $"WheelSequence<time {StartTime}, pos {Pos}> turns:\n  " + string.Join("\n  ", Notes);
}
