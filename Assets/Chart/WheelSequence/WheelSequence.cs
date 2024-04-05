using System.Collections.Generic;
using System.Linq;

public class WheelSequence : TimedNote<Chart, Note>, INoteContainer<WheelTurn>
{
	public override bool AffectsScore => false;

	protected int pos;
	public int Pos => pos;

	protected readonly SortedSet<WheelTurn> notes;
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

	public IReadOnlyCollection<WheelTurn> GetNotesBefore(int time, bool inclusive = true) 
		=> notes.GetViewBetween(DummyWheelTurn.Min, new DummyWheelTurn(time, inclusive ? int.MaxValue : int.MinValue));

	// Get notes that end after the given time
	public IReadOnlyCollection<WheelTurn> GetNotesAfter(int time, bool inclusive = true) 
		=> notes.GetViewBetween(new DummyWheelTurn(time, inclusive ? int.MinValue : int.MaxValue), DummyWheelTurn.Max);

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
		return lines;
	}

    public override string ToString() => $"WheelSequence<time {StartTime}, pos {Pos}> turns:\n  " + string.Join("\n  ", Notes);
}
