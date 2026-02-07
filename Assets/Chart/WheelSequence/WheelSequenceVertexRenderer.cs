using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class WheelSequenceVertexRenderer : NoteRenderer<EditableWheelTurn, EditableWheelSequence>, IPointerDownHandler
{
    public EditableWheelSequence WheelSequence { get; private set; }

    int dragStartStartAngle;
    int dragStartDeltaAngle;
    int dragStartSeqPos;
    int dragStartSeqTime;
    bool isStartingVertex;
    Dictionary<EditableWheelTurn, int> dragStartEndTimes;
    Dictionary<EditableWheelTurn, int> oldDeltaAngles;

    public void Init(WheelSequenceRenderer sequenceRenderer, EditableWheelTurn turn)
    {
        base.Init(sequenceRenderer.Track, turn);
        WheelSequence = sequenceRenderer.Note;
        isStartingVertex = Note == Note.Container.startingNote;
    }

    protected override void Update()
    {
        // Don't call base.Update() because we don't need OnNoteStarted or OnNoteEnded, 
        // and it will cause exception when Note is null

        int pos;
        int time;
        if (isStartingVertex)
        {
            pos = WheelTurn.AngleToPos(WheelSequence.Notes.First.StartAngle);
            time = WheelSequence.StartTime;
        }
        else
        {
            pos = WheelTurn.AngleToPos(WheelTurn.WrapAngle(Note.StartAngle + Note.DeltaAngle));
            time = Note.EndTime;
        }
        rect.anchorMin = rect.anchorMax = PlaybackManager.Instance.TrackToUIPosition(pos, time);
    }

    protected override void OnLeftClick() 
    {
        UIManager.ClickedElement = Note;
        UIManager.SelectedElement = WheelSequence;
    } 

    protected override void OnRightClick()
    {
        if (WheelSequence.Notes.Count <= 1) 
        {
            WheelSequence.RemoveSelf();
            return;
        }

        if (isStartingVertex) // If right clicked start vertex, remove the 1st turn and move start to the end of the first turn
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
        UIManager.SelectedElement = WheelSequence;
        dragStartStartAngle = isStartingVertex ? WheelSequence.Notes.First.StartAngle : Note.StartAngle;
        dragStartDeltaAngle = isStartingVertex ? WheelSequence.Notes.First.DeltaAngle : Note.DeltaAngle;
        dragStartSeqPos = WheelSequence.Pos;
        dragStartSeqTime = WheelSequence.StartTime;
        dragStartEndTimes = WheelSequence.Notes.ToDictionary(n => n, n => n.EndTime);
        oldDeltaAngles = WheelSequence.Notes.ToDictionary(n => n, n => n.DeltaAngle);
    }

    protected override void OnDrag()
    {
        bool changeTime = !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl);
        // do not allow for whole sequence dragging if this note is in marquee selection
        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && !isInMarqueeSelection)
        {
            if (Note != null)
            {
                int deltaAngleDiff = dragStartDeltaAngle - Note.DeltaAngle;
                Note.DeltaAngle += deltaAngleDiff;
                if (Note.NextTurn != null) Note.NextTurn.DeltaAngle -= deltaAngleDiff;
            }
            else
            {
                WheelSequence.Notes.First.DeltaAngle = dragStartDeltaAngle;
            }

            int myStartTime = isStartingVertex ? dragStartSeqTime : dragStartEndTimes[Note];
            int snappedTimeDiff = UIManager.SnapTime(myStartTime + dragTimeDiff) - myStartTime;

            // I don't really know why, but the delta angles get messed up here
            // it's likely because looping through the notes re-orders the list a few times, but idk how that messes things up
            // workaround for now is to store the delta angles and re-apply them afterwards

            if (changeTime) WheelSequence.StartTime = PlaybackManager.ClampTime(dragStartSeqTime + snappedTimeDiff);
            else WheelSequence.StartTime = dragStartSeqTime;

            Dictionary<EditableWheelTurn, int> deltaAngles = dragStartEndTimes.Keys.ToDictionary(n => n, n => n.DeltaAngle);
            foreach (var note in dragStartEndTimes.Keys)
            {
                if (changeTime) note.EndTime = PlaybackManager.ClampTime(dragStartEndTimes[note] + snappedTimeDiff);
                else note.EndTime = dragStartEndTimes[note];
            }
            foreach (var note in dragStartEndTimes.Keys)
            {
                note.DeltaAngle = deltaAngles[note];
            }

            WheelSequence.Pos = (int) Mathf.Repeat(dragStartSeqPos + dragPosDiff, 100);
        }
        else
        {
            // if we stop holding shift, reset note positions to their pos when drag started
            // ... unless this note is in a marquee selection
            if (!isInMarqueeSelection)
            {
                WheelSequence.StartTime = dragStartSeqTime;

                foreach (var note in dragStartEndTimes.Keys)
                {
                    note.EndTime = dragStartEndTimes[note];
                }
            }

            // if in marquee selection, other notes may have moved in the same frame,
            // which changes the start/delta angle and sequence position, so reset these values
            // and use the delta drag position of this frame
            int thisDragPosDiff = dragPosDiff;
            if (isInMarqueeSelection)
            {
                dragStartStartAngle = isStartingVertex ? WheelSequence.Notes.First.StartAngle : Note.StartAngle;
                dragStartDeltaAngle = isStartingVertex ? WheelSequence.Notes.First.DeltaAngle : Note.DeltaAngle;
                dragStartSeqPos = WheelSequence.Pos;
                thisDragPosDiff = dragPosDelta;
            }

            // modify time of notes if changeTime is true
            if (isStartingVertex)
            {
                WheelSequence.StartTime = changeTime ? Math.Clamp(UIManager.SnapTime(dragStartSeqTime + dragTimeDiff), 0, WheelSequence.Notes.First.EndTime-1)
                                                     : dragStartSeqTime;
            }
            else
            {
                Note.EndTime = changeTime ? Math.Max(PlaybackManager.ClampTime(UIManager.SnapTime(dragStartEndTimes[Note] + dragTimeDiff)), WheelSequence.StartTime+1)
                                          : dragStartEndTimes[Note];
            }

            // modify position of notes
            if (isStartingVertex)
            {
                WheelSequence.Pos = (int) Mathf.Repeat(dragStartSeqPos + thisDragPosDiff, 100);
                var firstTurn = WheelSequence.Notes.First;
                int startAngleDiff = WheelTurn.PosToAngle(dragStartSeqPos + thisDragPosDiff) - dragStartStartAngle;
                firstTurn.DeltaAngle = dragStartDeltaAngle - startAngleDiff;
            }
            else
            {   
                int startAngleDiff = Note.StartAngle - dragStartStartAngle;
                int newDeltaAngle = WheelTurn.PosToAngle(WheelTurn.AngleToPos(dragStartDeltaAngle) + thisDragPosDiff) - startAngleDiff;
                int deltaAngleDiff = newDeltaAngle - Note.DeltaAngle;
                Note.DeltaAngle += deltaAngleDiff;
                if (Note.NextTurn != null) Note.NextTurn.DeltaAngle -= deltaAngleDiff;
            }
        }
    }

    protected override void OnEndDrag()
    {
        int startTimeDelta = WheelSequence.StartTime - dragStartSeqTime;
        int posDelta = WheelSequence.Pos - dragStartSeqPos;
        Dictionary<EditableWheelTurn, int> newEndTimes = WheelSequence.Notes.ToDictionary(n => n, n => n.EndTime);
        Dictionary<EditableWheelTurn, int> newDeltaAngles = WheelSequence.Notes.ToDictionary(n => n, n => n.DeltaAngle);

        EditWheelSequenceAction wheelSequenceAction = new EditWheelSequenceAction(WheelSequence, startTimeDelta, posDelta, dragStartEndTimes, newEndTimes, oldDeltaAngles, newDeltaAngles);
        wheelSequenceAction.CommitAction();
    }
}
