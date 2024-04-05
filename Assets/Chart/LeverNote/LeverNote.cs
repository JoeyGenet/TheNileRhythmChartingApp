
public class LeverNote : TimedNote<LeverSequence, LeverNote>
{
	protected LeverPos pos;
	public LeverPos Pos => pos;

	public LeverNote PreviousNote;
	public LeverNote NextNote;

	public LeverNote(int time, int id) : base(time, id) { }
	public LeverNote(LeverSequence seq, int time, LeverPos pos) : base(seq, time)
	{
		this.pos = pos;
	}
	public LeverPos PreviousPos()
	{
		if (PreviousNote == null)
		{
			return LeverPos.Middle;
		}
		else
		{
			return PreviousNote.Pos;
		}
	}
}

public class DummyLeverNote : LeverNote
{
	public static DummyLeverNote Min = new(int.MinValue, int.MinValue);
	public static DummyLeverNote Max = new(int.MaxValue, int.MaxValue);

    public DummyLeverNote(int time, int id) : base(time, id) {}
}