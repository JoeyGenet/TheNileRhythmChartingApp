
public class WheelTurn : Note<WheelSequence, WheelTurn>
{
	public override int StartTime => PreviousTurn.EndTime;
	public override int EndTime { get; }

	public readonly int startAngle; // TODO: remove this. calculate in wheel sequence
	public readonly int deltaAngle;

	public WheelTurn PreviousTurn;
	public WheelTurn NextTurn;

    public WheelTurn(int id) : base(id) { }
	public WheelTurn(WheelSequence seq, int endTime, int startAngle, int deltaAngle) : base(seq)
	{
		EndTime = endTime;
		this.startAngle = startAngle;
		this.deltaAngle = deltaAngle;
	}


}

public class DummyWheelTurn : WheelTurn
{
	public override int StartTime { get; set; }

	public DummyWheelTurn(int time, int id) : base(id)
	{
		StartTime = time;
	}
}