using System.Collections.Generic;
using System.Linq;

public class LeverSequence : Note<Chart, Note>, INoteContainer<LeverNote>
{
	public override bool AffectsScore => false;

	protected LeftRight dir;
	public LeftRight Dir => dir;

    protected SortedSet<LeverNote> notes;
	public IReadOnlySortedSet<LeverNote> Notes => (ReadOnlySortedSet<LeverNote>) notes;

    // public LeverNote FirstNote => notes.Min;
    // public LeverNote LastNote => notes.Max;

	public override int StartTime => notes.Min.StartTime;
	public override int EndTime => notes.Max.EndTime;

    public LeverSequence(int time, int id) : base(id)
	{
		notes = new() { new LeverNote(time, -1) };
	}

	public LeverSequence(Chart chart, LeftRight dir, List<(int time, LeverPos pos)> levers) : base(chart)
	{
		this.dir = dir;
		InitializeNotes(levers.Select(lever => new LeverNote(this, lever.time, lever.pos)));
	}

	protected void InitializeNotes(IEnumerable<LeverNote> levers)
	{
		notes = new SortedSet<LeverNote>(levers);
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

	public IReadOnlyCollection<LeverNote> GetNotesBetween(int minTime, int maxTime, bool inclusive = true) 
		=> notes.GetViewBetween(new DummyLeverNote(minTime, inclusive ? int.MinValue : int.MaxValue), new DummyLeverNote(maxTime, inclusive ? int.MaxValue : int.MinValue));

}
