using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class WheelSequenceVertexRenderer : NoteRenderer<EditableWheelTurn, EditableWheelSequence>, IPointerUpHandler
{
    public EditableWheelSequence WheelSequence { get; private set; }

    int dragStartStartAngle;
    int dragStartDeltaAngle;
    int dragStartSeqPos;
    int dragStartSeqTime;
    bool isStartingVertex;
    bool isLastSelectedVertex;
    Dictionary<EditableWheelTurn, int> dragStartEndTimes;
    Dictionary<EditableWheelTurn, int> oldDeltaAngles;

    public void Init(WheelSequenceRenderer sequenceRenderer, EditableWheelTurn turn)
    {
        base.Init(sequenceRenderer.Track, turn);
        WheelSequence = sequenceRenderer.Note;
        isStartingVertex = turn == WheelSequence.startingNote;
    }

    protected override void Update()
    {
        // Don't call base.Update() because we don't need OnNoteStarted or OnNoteEnded, 
        // and it will cause exception when Note is null

        // start dragging if we scroll/enter playback while mouse is held over note
        if (isBeingPressed && (UIManager.Track.IsScrolling || !PlaybackManager.Instance.IsPaused))
        {
            isBeingPressed = false;
            OnBeginDrag(pointerEventData);
        }

        // toggle drag scrolling
        bool shouldDragScroll = PlaybackManager.Instance.IsPaused && IsBeingDragged && UIManager.SelectionGroup.eventSenderNote==Note;
        UIManager.Track.IsDragScrolling = shouldDragScroll || UIManager.Track.IsDragScrolling;
        
        // drag note if track is scrolling or in playback
        if (shouldDragScroll && UIManager.Track.scrollAmount != 0 || (!PlaybackManager.Instance.IsPaused && IsBeingDragged))
        {
            OnDrag(pointerEventData);
        }

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

        // animator.SetBool("IsMultiSelected", IsInMarqueeSelection);
    }

    protected override void OnLeftClick() 
    {
        if (!IsBeingDragged)
        {
            // if held shift/ctrl/alt when note clicked, either add this note to or remove it from current selection
            if (modifyExistingSelection)
            {
                AddOrSubtractNoteFromSelection(Note, Note.Container);
            }
            // select the group if it contains Note, or clear group and select Note.Container
            else
            {
                SelectNoteOrMultiselection(Note.Container);
            }
        }
    } 

    protected override void OnRightClick()
    {
        if (WheelSequence.Notes.Count <= 1) 
        {
            WheelSequence.RemoveSelf();
            return;
        }

        if (isStartingVertex && !IsInMarqueeSelection) // If right clicked start vertex, remove the 1st turn and move start to the end of the first turn
        {
            WheelSequence.RemoveNoteFromFront(WheelSequence.Notes.ElementAt(0));
        }
        else
        {
            Note.RemoveSelf();
        }
    }

    protected override void OnDragStart()
    {
        SelectNoteOrMultiselection(WheelSequence, updateEditHistory:false);
        dragStartStartAngle = isStartingVertex ? WheelSequence.Notes.First.StartAngle : Note.StartAngle;
        dragStartDeltaAngle = isStartingVertex ? WheelSequence.Notes.First.DeltaAngle : Note.DeltaAngle;
        dragStartSeqPos = WheelSequence.Pos;
        dragStartSeqTime = WheelSequence.StartTime;
        dragStartEndTimes = WheelSequence.Notes.ToDictionary(n => n, n => n.EndTime);
        oldDeltaAngles = WheelSequence.Notes.ToDictionary(n => n, n => n.DeltaAngle);
        isStartingVertex = Note == WheelSequence.startingNote;
        if (IsInMarqueeSelection)
        {
            EditableWheelTurn currentTurn = Note.Container.Notes.Last();
            while (!UIManager.SelectionGroup.notes.Contains(currentTurn) && currentTurn!=null)
            {
                currentTurn = currentTurn.PreviousTurn;
            }
            // if the note is the last one in the selection, of if the starting vertex the sole selected note
            isLastSelectedVertex = Note == currentTurn || (currentTurn == null && isStartingVertex);
        }
    }

    protected override void OnDrag()
    {
        bool changeTime = !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl);
        // do not allow for whole sequence dragging if this note is in marquee selection
        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && !IsInMarqueeSelection)
        {
            // undo any single-note drag changes before applying drag diff to whole sequence
            OnCancelDrag();
            
            if (!isStartingVertex)
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
            if (!IsInMarqueeSelection)
            {
                // undo any changes to other notes via shift+drag before applying drag diff to this note
                OnCancelDrag();
            }

            int currentDragStartStartAngle = dragStartStartAngle;
            int currentDragStartDeltaAngle = dragStartDeltaAngle;
            int currentDragStartSeqPos = dragStartSeqPos;
            int currentDragPosDiff = dragPosDiff;
            // if in marquee selection, other notes may have moved in the same frame,
            // which changes the start/delta angle and sequence position, so reset these values
            // and use the delta drag position of this frame
            if (IsInMarqueeSelection)
            {
                currentDragStartStartAngle = isStartingVertex ? WheelSequence.Notes.First.StartAngle : Note.StartAngle;
                currentDragStartDeltaAngle = isStartingVertex ? WheelSequence.Notes.First.DeltaAngle : Note.DeltaAngle;
                currentDragStartSeqPos = WheelSequence.Pos;
                currentDragPosDiff = dragPosDelta;
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
                WheelSequence.Pos = (int) Mathf.Repeat(currentDragStartSeqPos + currentDragPosDiff, 100);
                var firstTurn = WheelSequence.Notes.First;
                int startAngleDiff = WheelTurn.PosToAngle(currentDragStartSeqPos + currentDragPosDiff) - currentDragStartStartAngle;
                firstTurn.DeltaAngle = currentDragStartDeltaAngle - startAngleDiff;
            }
            else
            {   
                int startAngleDiff = Note.StartAngle - currentDragStartStartAngle;
                int newDeltaAngle = WheelTurn.PosToAngle(WheelTurn.AngleToPos(currentDragStartDeltaAngle) + currentDragPosDiff) - startAngleDiff;
                // int newDeltaAngle = currentDragStartDeltaAngle + WheelTurn.PosToAngle(thisDragPosDiff) - startAngleDiff; // use the current drag?
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
        
        if (!IsInMarqueeSelection)
        {
            wheelSequenceAction.CommitAction();
        }
        else if (isLastSelectedVertex)
        {
            UIManager.SelectionGroup.AddToMultiselectMoveAction(wheelSequenceAction);
        }
    }

    protected override void OnCancelDrag()
    {
        WheelSequence.StartTime = dragStartSeqTime;
        if (dragStartEndTimes != null)
        {
            foreach (var note in dragStartEndTimes.Keys)
            {
                note.EndTime = dragStartEndTimes[note];
            }
        }
        // changing the delta angle and wheel sequence position calls RecomputeStartAngle()
        // so these must be done after all other changes have been made (for some reason)
        if (oldDeltaAngles != null)
        {
            foreach (var note in oldDeltaAngles.Keys)
            {
                note.DeltaAngle = oldDeltaAngles[note];
            }
        }
        WheelSequence.Pos = dragStartSeqPos;
    }
}
