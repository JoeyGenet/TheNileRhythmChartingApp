using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;

public class WheelSequence : TimedNote<Chart, Note>, INoteContainer<WheelTurn>
{
	public override bool AffectsScore => false;

	readonly SortedSet<WheelTurn> notes;
    public IReadOnlyCollection<WheelTurn> Notes => notes;
	public readonly int Pos;

	public override int EndTime {get;}


    public WheelSequence(int time, int id) : base(time, id) { }

	public WheelSequence(Chart chart, int time, int pos, List<int> angles, List<int> endTimes) : base(chart, time)
	{
		Pos = pos;
		notes = new SortedSet<WheelTurn>();
		int prevTime = time;
		int prevAngle = pos * 180 / 100;
		for (int i = 0; i < angles.Count; i++)
		{
			notes.Add(new WheelTurn(this, endTimes[i], prevAngle, angles[i]));
			// TODO: setup linked list
			prevTime = endTimes[i];
			prevAngle += angles[i];
		}
		EndTime = notes.Max.EndTime;
	}

	public bool AddNoteWithoutNotify(WheelTurn note) => notes.Add(note);
	public bool RemoveNoteWithoutNotify(WheelTurn note) => notes.Remove(note);
	public bool ContainsNote(WheelTurn note) => notes.Contains(note);

    void INoteContainer<WheelTurn>.OnNoteAdded(WheelTurn note)
    {
		SaveManager.Instance.HasChanges = true;
		// OnLeverNoteAdded?.Invoke(this, note);
    }

    void INoteContainer<WheelTurn>.OnNoteRemoved(WheelTurn note)
    {
		SaveManager.Instance.HasChanges = true;
		// OnLeverNoteRemoved?.Invoke(this, note);

		// Remove this entire sequence if last lever note was removed
		// if (notes.Count == 0)
		// {
		// 	SaveManager.Instance.Chart.RemoveNote(this);
		// }
    }
}
