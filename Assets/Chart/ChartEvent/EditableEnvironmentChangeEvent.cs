using System;
using System.Linq;
using UnityEngine;

public class EditableEnvironmentChangeEvent : EnvironmentChangeEvent, IEditable
{
    public new EditableChart Chart => (EditableChart) base.Chart;

    public bool HasChanges => false;
    public void SetChanged() => Chart.SetChanged();

    public virtual new int StartTime
    {
        get => base.StartTime;
        set => this.RegisterChange(ref startTime, Math.Clamp(value, 0, EndTime), onChanged: () =>
        {
            // Update linked list
            
            if (PreviousEnvironmentChange != null) PreviousEnvironmentChange.NextEnvironmentChange = NextEnvironmentChange;
            if (NextEnvironmentChange != null) NextEnvironmentChange.PreviousEnvironmentChange = PreviousEnvironmentChange;

            // FIXME: inefficient. weird stuff might happen if 2 events have the same start time
            var prevEnvironmentChange = Chart.Events.OrderBy(env => env.StartTime).LastOrDefault(env => env.StartTime < StartTime);
            var nextEnvironmentChange = prevEnvironmentChange != null ? prevEnvironmentChange.NextEnvironmentChange
                                            : Chart.Events.OrderBy(env => env.StartTime).FirstOrDefault(env => env != this);
            
			if (prevEnvironmentChange != null) prevEnvironmentChange.NextEnvironmentChange = this;
			if (nextEnvironmentChange != null) nextEnvironmentChange.PreviousEnvironmentChange = this;
			PreviousEnvironmentChange = prevEnvironmentChange;
			NextEnvironmentChange = nextEnvironmentChange;

            Debug.Log($"start time -> {StartTime}\n{Chart.EventsAsString()}");
        });
    }
    public virtual new int EndTime
    {
        get => base.EndTime;
        set => this.RegisterChange(ref endTime, Math.Clamp(value, StartTime, (int) Chart.Package.SongLength));
    }
    public virtual new Environment Environment
    {
        get => base.Environment;
        set => this.RegisterChange(ref environment, value);
    }

    public int Duration
    {
        get => EndTime - StartTime;
        set => EndTime = StartTime + value;
    }

    public EditableEnvironmentChangeEvent(Chart chart, int startTime, int endTime, Environment environment)
        : base(chart, startTime, endTime, environment) {}

	public EditableEnvironmentChangeEvent(EditableChart chart, EnvironmentChangeEvent evt)
		: base(chart, evt.StartTime, evt.EndTime, evt.Environment) {}
}

public class ChartEnvironment : EditableEnvironmentChangeEvent
{
    public override int StartTime
    {
        get => 0;
        set {}
    }
    public override int EndTime
    {
        get => 0;
        set {}
    }
    public override Environment Environment
    {
        get => Chart.Environment;
        set => Chart.Environment = value;
    }

    public ChartEnvironment(EditableChart chart) : base(chart, 0, 0, chart.Environment) {}
}