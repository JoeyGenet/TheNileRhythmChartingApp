using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class WheelSequenceVertexRenderer : NoteRenderer<EditableWheelTurn, EditableWheelSequence>, IPointerClickHandler
{
    public EditableWheelSequence WheelSequence { get; private set; }

    int dragStartStartAngle;
    int dragStartDeltaAngle;
    int dragStartSeqPos;
    int dragStartSeqTime;
    Dictionary<EditableWheelTurn, int> dragStartEndTimes;

    public void Init(WheelSequenceRenderer sequenceRenderer, EditableWheelTurn turn)
    {
        base.Init(sequenceRenderer.Track, turn, sequenceRenderer.Note);
        WheelSequence = sequenceRenderer.Note;
    }

    protected override void Update()
    {
        // Don't call base.Update() because we don't need OnNoteStarted or OnNoteEnded, 
        // and it will cause exception when Note is null

        int pos;
        int time;
        if (Note == null)
        {
            pos = WheelTurn.AngleToPos(WheelSequence.Notes.Min.StartAngle);
            time = WheelSequence.StartTime;
        }
        else
        {
            pos = WheelTurn.AngleToPos(WheelTurn.WrapAngle(Note.StartAngle + Note.DeltaAngle));
            time = Note.EndTime;
        }
        rect.anchorMin = rect.anchorMax = PlaybackManager.Instance.TrackToUIPosition(pos, time);
    }

    protected override void OnLeftClick() => UIManager.SelectedNote = WheelSequence;

    protected override void OnRightClick()
    {
        if (WheelSequence.Notes.Count <= 1) 
        {
            WheelSequence.RemoveSelf();
            return;
        }

        if (Note == null) // If right clicked start vertex, remove the 1st turn and move start to the end of the first turn
        {
            var secondTurn = WheelSequence.Notes.ElementAt(1);
            WheelSequence.Pos = WheelTurn.AngleToPos(WheelTurn.WrapAngle(secondTurn.StartAngle));
            WheelSequence.StartTime = secondTurn.StartTime;
            secondTurn.PreviousTurn.DeltaAngle = 0;
            WheelSequence.RemoveNote(secondTurn.PreviousTurn);
        }
        else
        {
            Note.RemoveSelf();
        }
    }

    protected override void OnDragStart()
    {
        UIManager.SelectedNote = WheelSequence;
        dragStartStartAngle = Note == null ? WheelSequence.Notes.Min.StartAngle : Note.StartAngle;
        dragStartDeltaAngle = Note == null ? WheelSequence.Notes.Min.DeltaAngle : Note.DeltaAngle;
        dragStartSeqPos = WheelSequence.Pos;
        dragStartSeqTime = WheelSequence.StartTime;
        dragStartEndTimes = WheelSequence.Notes.ToDictionary(n => n, n => n.EndTime);
    }

    protected override void OnDrag()
    {
        bool changeTime = !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl);
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            if (Note != null)
            {
                int deltaAngleDiff = dragStartDeltaAngle - Note.DeltaAngle;
                Note.DeltaAngle += deltaAngleDiff;
                if (Note.NextTurn != null) Note.NextTurn.DeltaAngle -= deltaAngleDiff;
            }
            else
            {
                WheelSequence.Notes.Min.DeltaAngle = dragStartDeltaAngle;
            }

            if (changeTime) WheelSequence.StartTime = PlaybackManager.ClampTime(dragStartSeqTime + dragTimeDiff);
            else WheelSequence.StartTime = dragStartSeqTime;
            foreach (var note in dragStartEndTimes.Keys)
            {
                if (changeTime) note.EndTime = PlaybackManager.ClampTime(dragStartEndTimes[note] + dragTimeDiff);
                else note.EndTime = dragStartEndTimes[note];
            }

            WheelSequence.Pos = Math.Clamp(dragStartSeqPos + dragPosDiff, 0, 100);
        }
        else
        {
            WheelSequence.StartTime = dragStartSeqTime;
            foreach (var note in dragStartEndTimes.Keys)
            {
                note.EndTime = dragStartEndTimes[note];
            }

            if (changeTime)
            {
                if (Note == null)
                {
                    WheelSequence.StartTime = Math.Clamp(dragStartSeqTime + dragTimeDiff, 0, WheelSequence.Notes.Min.EndTime-1);
                }
                else
                {
                    Note.EndTime = Math.Max(PlaybackManager.ClampTime(dragStartEndTimes[Note] + dragTimeDiff), WheelSequence.StartTime+1);
                }
            }
            
            if (Note == null)
            {
                WheelSequence.Pos = (int) Mathf.Repeat(dragStartSeqPos + dragPosDiff, 100);
                var firstTurn = WheelSequence.Notes.Min;
                int startAngleDiff = WheelTurn.PosToAngle(dragStartSeqPos + dragPosDiff) - dragStartStartAngle;
                firstTurn.DeltaAngle = dragStartDeltaAngle - startAngleDiff;
            }
            else
            {
                int startAngleDiff = Note.StartAngle - dragStartStartAngle;
                int newDeltaAngle = dragStartDeltaAngle - startAngleDiff + WheelTurn.PosToAngle(dragPosDiff);
                int deltaAngleDiff = newDeltaAngle - Note.DeltaAngle;
                Note.DeltaAngle += deltaAngleDiff;
                if (Note.NextTurn != null) Note.NextTurn.DeltaAngle -= deltaAngleDiff;
            }
        }
    }
}
