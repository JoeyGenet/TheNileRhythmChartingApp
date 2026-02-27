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
    [SerializeField] Animator animator;
    
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

        animator.SetBool("IsMultiSelected", isInMarqueeSelection);
    }

    protected override void OnLeftClick() 
    {
        UIManager.ClickedElement = Note;
        SelectNoteOrMultiselection(Note);
    } 
    
    protected override void OnRightClick()
    {
        if (Note.RightPos is not int rightPos                 // Single note, so delete entire thing
            || UIManager.SelectionGroup.notes.Contains(Note)) // or if in multiselection, delete entire thing
        {
            Note.RemoveSelf();
            return;
        }

        if (Track.PointerToTrackPosition(pointerEventData.position, clamp: false) is not var (clickPos, _)) return;

        int oldLeftPos = Note.LeftPos;
        int? oldRightPos = Note.RightPos;

        if (Math.Abs(clickPos - rightPos) < Math.Abs(clickPos - Note.LeftPos))
        {
            Note.RightPos = null;
        }
        else
        {
            Note.LeftPos = rightPos;
            Note.RightPos = null;
        }

        EditButtonNoteAction moveButtonNoteAction = new EditButtonNoteAction(Note, 0, oldLeftPos, oldRightPos, Note.LeftPos, Note.RightPos);
        moveButtonNoteAction.CommitAction();
        
    }

    protected override void OnDragStart()
    {   
        SelectNoteOrMultiselection(Note);
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
}
