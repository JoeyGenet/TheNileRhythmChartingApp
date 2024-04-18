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

    protected override void OnLeftClick() => UIManager.SelectedNote = Note;
    protected override void OnRightClick() => Note.RemoveSelf();

    protected override void OnDragStart()
    {   
        UIManager.SelectedNote = Note;
        dragStartLeftPos = Note.LeftPos;
        dragStartRightPos = Note.RightPos;
        dragStartTime = Note.StartTime;
    }

    protected override void OnDrag()
    {
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
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
            Note.StartTime = PlaybackManager.ClampTime(dragStartTime + dragTimeDiff);
        }
        else
        {
            Note.StartTime = dragStartTime;
        }
    }
}
