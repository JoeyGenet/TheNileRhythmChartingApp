using System;
using System.Linq;
using UnityEngine;

public class EditableBpmChangeEvent : BpmChangeEvent, IEditable
{
    public new EditableChart Chart => (EditableChart) base.Chart;

    public bool isBeingDragged = false;
    public bool HasChanges => false;
    public void SetChanged() => Chart.SetChanged();

    public virtual new int StartTime
    {
        get => base.StartTime;
        set => this.RegisterChange(ref startTime, Math.Clamp(value, 0, (int) Chart.Package.SongLength), onChanged: () =>
        {
            // Update linked list
            
            if (PreviousBpmChange != null) PreviousBpmChange.NextBpmChange = NextBpmChange;
            if (NextBpmChange != null) NextBpmChange.PreviousBpmChange = PreviousBpmChange;

            // FIXME: inefficient. weird stuff might happen if 2 events have the same start time
            var prevBpmChange = Chart.BPMChanges.OrderBy(bpm => bpm.StartTime).LastOrDefault(bpm => bpm.StartTime < StartTime);
            var nextBpmChange = prevBpmChange != null ? prevBpmChange.NextBpmChange
                                            : Chart.BPMChanges.OrderBy(bpm => bpm.StartTime).FirstOrDefault(bpm => bpm != this);
            
			if (prevBpmChange != null) prevBpmChange.NextBpmChange = this;
			if (nextBpmChange != null) nextBpmChange.PreviousBpmChange = this;
			PreviousBpmChange = prevBpmChange;
			NextBpmChange = nextBpmChange;

            Debug.Log($"start time -> {StartTime}\n{Chart.EventsAsString()}");
        });
    }

    public virtual new uint BPM
    {
        get => base.BPM;
        set => this.RegisterChange(ref bpm, value);
    }

    public int Duration
    {
        get => NextBpmChange != null ? NextBpmChange.StartTime - StartTime : (int)Chart.Package.SongLength - StartTime;
    }

    public int EndTime
    {
        get => StartTime + Duration;
    }

    public float BeatCount
    {
        get => BPM * Duration / 60000f;
    }

    public EditableBpmChangeEvent(Chart chart, int startTime, uint bpm)
        : base(chart, startTime, bpm) {}

	public EditableBpmChangeEvent(EditableChart chart, EditableBpmChangeEvent evt)
		: base(chart, evt.startTime, evt.BPM) {}
}

public class ChartBpm : EditableBpmChangeEvent
{
    public override int StartTime
    {
        get => 0;
        set {}
    }
    public override uint BPM
    {
        get => Chart.Package.BPM;
        set => Chart.Package.BPM = value;
    }

    public ChartBpm(EditableChart chart) : base(chart, 0, 120) {}
}
