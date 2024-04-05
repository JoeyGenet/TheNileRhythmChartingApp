
public class WheelFlick : TimedNote<Chart, Note>
{
	protected LeftRight dir;
	public LeftRight Dir => dir;

	public WheelFlick(int time, int id) : base(time, id) {}
	public WheelFlick(Chart chart, int time, LeftRight dir) : base(chart, time)
	{
		this.dir = dir;
	}
}
