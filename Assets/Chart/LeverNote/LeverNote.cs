
public class LeverNote : TimedNote<LeverSequence, LeverNote>
{
    LeverPos pos;
    public LeverPos Pos
    {
        get => pos;
        set => SaveManager.RegisterChange(ref pos, value);
    }

    public LeverNote(int time, int id) : base(time, id) { }
    public LeverNote(LeverSequence seq, int time, LeverPos pos) : base(seq, time)
    {
        this.pos = pos;
    }
}