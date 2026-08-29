using System.ComponentModel;

public class BpmChangeEvent
{
    public Chart Chart;

    protected int startTime;
    public int StartTime => startTime;

    protected uint bpm;
    public uint BPM => bpm;

    public BpmChangeEvent PreviousBpmChange;
    public BpmChangeEvent NextBpmChange;

    public uint PreviousBpm => PreviousBpmChange != null 
        ? PreviousBpmChange.BPM
        : Chart.Package.BPM;

    public BpmChangeEvent(Chart chart, int startTime, uint bpm)
    {
        Chart = chart;
        this.startTime = startTime;
        this.bpm = bpm;
    }
}
