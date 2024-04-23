using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditableEnvironmentChangeEvent : EnvironmentChangeEvent, IEditable
{
    new EditableChart Chart => (EditableChart) base.Chart;

    public bool HasChanges => false;
    public void SetChanged() => Chart.SetChanged();

    public new int StartTime
    {
        get => base.StartTime;
        set => this.RegisterChange(ref startTime, Math.Clamp(value, 0, EndTime));
    }
    public new int EndTime
    {
        get => base.EndTime;
        set => this.RegisterChange(ref endTime, Math.Clamp(value, StartTime, (int) Chart.Package.SongLength));
    }
    public new EnvironmentOption Environment
    {
        get => base.Environment;
        set => this.RegisterChange(ref environment, value);
    }

    public int Duration
    {
        get => EndTime - StartTime;
        set => EndTime = StartTime + value;
    }

    public EditableEnvironmentChangeEvent(Chart chart, int startTime, int endTime, EnvironmentOption environment)
        : base(chart, startTime, endTime, environment) {}
}
