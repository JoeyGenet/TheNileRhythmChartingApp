using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class ButtonNoteRenderer : NoteRenderer<EditableButtonNote>
{
    [SerializeField] RectTransform leftNote;
    [SerializeField] RectTransform rightNote;
    [SerializeField] Image line;
    
    AudioSource hitSound;

    int dragStartTime;
    int dragStartLeftPos;
    int? dragStartRightPos;

    protected override void Awake()
    {
        base.Awake();
        hitSound = GetComponent<AudioSource>();
    }

    protected override void OnNoteStarted()
    {
        hitSound.Play();
    }

    protected override void Update()
    {
        base.Update();
        
        // Hide right note if note is single
        rightNote.gameObject.SetActive(!Note.IsSingle);
        line.gameObject.SetActive(!Note.IsSingle);

        // Update position
        Vector2 leftUIPos = PlaybackManager.Instance.TrackToUIPosition(Note.LeftPos, Note.StartTime);
        rect.anchorMin = leftUIPos;
        rect.anchorMax = Note.RightPos is int rightPos
            ? PlaybackManager.Instance.TrackToUIPosition(rightPos, Note.StartTime)
            : leftUIPos;
    }

    protected override void OnLeftClick() 
    {
        if (!isBeingDragged)
        {
            // if holding shift/ctrl/alt, either add this note to or remove it from current selection
            if (TrackMarqueeSelection.ModifyExistingSelection && UIManager.SelectedElement != null &&
                (UIManager.ClickedElement is Note || UIManager.SelectedElement is TrackMarqueeSelectionGroup))
            {
                AddOrSubtractNoteFromSelection(Note);
            }
            // select the group if it contains Note, or clear group and select Note
            else
            {
                SelectNoteOrMultiselection(Note);
            }
        }
        
        UIManager.ClickedElement = Note;
    } 
    
    protected override void OnRightClick()
    {
        // If single note or in multiselection, delete entire thing
        if (Note.RightPos is not int rightPos || isInMarqueeSelection)
        {
            Note.RemoveSelf();
            return;
        }

        if (Track.PointerToTrackPosition(pointerEventData.position, clamp: false) is not var (clickPos, _)) return;

        // cache note positions before drag clear to detect which note we are mousing over
        int lastLeftPos = Note.LeftPos;
        int lastRightPos = rightPos;
        // if we delete a paired note while dragging, cancel drag
        // to prevent an UndoRedoAction from being logged when drag is complete
        if (isBeingDragged)
        {
            CancelDrag();
        }
        int oldLeftPos = Note.LeftPos;
        int oldRightPos = (int)Note.RightPos;

        if (Math.Abs(clickPos - lastRightPos) < Math.Abs(clickPos - lastLeftPos))
        {
            Note.RightPos = null;
        }
        else
        {
            Note.LeftPos = oldRightPos;
            Note.RightPos = null;
        }

        EditButtonNoteAction moveButtonNoteAction = new EditButtonNoteAction(Note, 0, oldLeftPos, oldRightPos, Note.LeftPos, Note.RightPos);
        moveButtonNoteAction.CommitAction();
        
    }

    protected override void OnDragStart()
    {   
        SelectNoteOrMultiselection(Note, updateEditHistory:false);
        dragStartLeftPos = Note.LeftPos;
        dragStartRightPos = Note.RightPos;
        dragStartTime = Note.StartTime;
    }

    protected override void OnDrag()
    {
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || isInMarqueeSelection)
        {
            Note.LeftPos = Math.Clamp(dragStartLeftPos + dragPosDiff, 0, 100);
            if (dragStartRightPos is int rightPos) Note.RightPos = Math.Clamp(rightPos + dragPosDiff, 0, 100);
        }
        else
        {
            if (dragStartRightPos is int rightPos && Math.Abs(dragStartPointerPos - rightPos) < Math.Abs(dragStartPointerPos - dragStartLeftPos))
            {
                Note.LeftPos = dragStartLeftPos;
                Note.RightPos = Math.Clamp(rightPos + dragPosDiff, Note.LeftPos, 100);
            }
            else
            {
                Note.LeftPos = Math.Clamp(dragStartLeftPos + dragPosDiff, 0, dragStartRightPos ?? 100);
                Note.RightPos = dragStartRightPos;
            }
        }

        if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
        {
            Note.StartTime = PlaybackManager.ClampTime(UIManager.SnapTime(dragStartTime + dragTimeDiff));
        }
        else
        {
            Note.StartTime = dragStartTime;
        }
    }

    protected override void OnEndDrag()
    {
        int startTimeDelta = Note.StartTime - dragStartTime;
        EditButtonNoteAction moveButtonNoteAction = new EditButtonNoteAction(Note, startTimeDelta, dragStartLeftPos, dragStartRightPos, Note.LeftPos, Note.RightPos);
        
        if (!isInMarqueeSelection)
        {
            moveButtonNoteAction.CommitAction();
        }
        else
        {
            UIManager.SelectionGroup.AddToMultiselectMoveAction(moveButtonNoteAction);
        }
    }

    protected override void OnCancelDrag()
    {
        Note.LeftPos = dragStartLeftPos;
        Note.RightPos = dragStartRightPos;
        Note.StartTime = dragStartTime;
    }
}
