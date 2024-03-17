
public class LeverNote : TimedNote<LeverSequence, LeverNote>
{
    LeverPos pos;
    public LeverPos Pos
    {
        get => pos;
        set => SaveManager.RegisterChange(ref pos, value);
    }

    // Commented out because not fully implemented yet (not updated when new lever note is inserted)
	// public LeverNote PreviousNote;
	// public LeverNote NextNote;

    public LeverNote(int time, int id) : base(time, id) { }
    public LeverNote(LeverSequence seq, int time, LeverPos pos) : base(seq, time)
    {
        this.pos = pos;
    }
}

public class DummyLeverNote : LeverNote
{
	public static DummyLeverNote Min = new(int.MinValue, int.MinValue);
	public static DummyLeverNote Max = new(int.MaxValue, int.MaxValue);

    public DummyLeverNote(int time, int id) : base(time, id) {}
}