using System.Collections.Generic;

public class LeverSequence : Note<Chart, Note>, INoteContainer<LeverNote>
{
	public override bool AffectsScore => false;

	protected LeftRight dir;
	public LeftRight Dir => dir;

    protected readonly SortedSet<LeverNote> notes;
	public IReadOnlyCollection<LeverNote> Notes => notes;

    public LeverNote FirstNote => notes.Min;
    public LeverNote LastNote => notes.Max;

	public override int StartTime => notes.Min.StartTime;
	public override int EndTime => notes.Max.EndTime;

    public LeverSequence(int time, int id) : base(id)
	{
		notes = new() { new LeverNote(time, -1) };
	}

	public LeverSequence(Chart chart, LeftRight dir, List<(int time, LeverPos pos)> levers) : base(chart)
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

	public IReadOnlyCollection<LeverNote> GetNotesBefore(int time, bool inclusive = true) 
		=> notes.GetViewBetween(DummyLeverNote.Min, new DummyLeverNote(time, inclusive ? int.MaxValue : int.MinValue));
}
