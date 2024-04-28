using System.Drawing;

public class EnvironmentChangeEvent
{
    public Chart Chart;

    protected int startTime;
    public int StartTime => startTime;

    protected int endTime;
    public int EndTime => endTime;

    protected Environment environment;
    public Environment Environment => environment;

    public EnvironmentChangeEvent PreviousEnvironmentChange;
    public EnvironmentChangeEvent NextEnvironmentChange;

    public Environment PreviousEnvironment => PreviousEnvironmentChange != null 
        ? PreviousEnvironmentChange.Environment
        : Chart.Environment;

    public EnvironmentChangeEvent(Chart chart, int startTime, int endTime, Environment environment)
    {
        Chart = chart;
        this.startTime = startTime;
        this.endTime = endTime;
        this.environment = environment;
    }
}
