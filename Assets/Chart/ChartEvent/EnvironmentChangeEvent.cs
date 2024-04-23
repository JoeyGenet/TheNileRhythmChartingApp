public class EnvironmentChangeEvent
{
    public Chart Chart;

    protected int startTime;
    public int StartTime => startTime;

    protected int endTime;
    public int EndTime => endTime;

    protected EnvironmentOption environment;
    public EnvironmentOption Environment => environment;

    public EnvironmentChangeEvent(Chart chart, int startTime, int endTime, EnvironmentOption environment)
    {
        Chart = chart;
        this.startTime = startTime;
        this.endTime = endTime;
        this.environment = environment;
    }
}
