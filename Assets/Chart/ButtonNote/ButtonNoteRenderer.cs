using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class ButtonNoteRenderer : NoteRenderer<EditableButtonNote>, IPointerClickHandler, IBeginDragHandler, IDragHandler
{
    [SerializeField] RectTransform leftNote;
    [SerializeField] RectTransform rightNote;
    [SerializeField] Image line;
    
    AudioSource hitSound;

    int dragStartPointerPos;
    int dragStartPointerTime;
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

    public void OnPointerClick(PointerEventData eventData)
    {
        switch (eventData.button)
        {
            case PointerEventData.InputButton.Left:
                UIManager.SelectedNote = Note;
                break;
            case PointerEventData.InputButton.Right:
                Note.RemoveSelf();
                break;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Track.PointerToTrackPosition(eventData.pressPosition, clamp: false) is not var (pos, time)) return;
        dragStartPointerPos = pos;
        dragStartPointerTime = time;
        dragStartLeftPos = Note.LeftPos;
        dragStartRightPos = Note.RightPos;
        dragStartTime = Note.StartTime;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (Track.PointerToTrackPosition(eventData.position, clamp: false) is not var (dragPos, dragTime)) return;
        int posDiff = dragPos - dragStartPointerPos;
        int timeDiff = dragTime - dragStartPointerTime;

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            Note.LeftPos = Math.Clamp(dragStartLeftPos + posDiff, 0, 100);
            if (dragStartRightPos is int rightPos) Note.RightPos = Math.Clamp(rightPos + posDiff, 0, 100);
        }
        else
        {
            if (dragStartRightPos is int rightPos && Math.Abs(dragStartPointerPos - rightPos) < Math.Abs(dragStartPointerPos - dragStartLeftPos))
            {
                Note.LeftPos = dragStartLeftPos;
                Note.RightPos = Math.Clamp(rightPos + posDiff, Note.LeftPos, 100);
            }
            else
            {
                Note.LeftPos = Math.Clamp(dragStartLeftPos + posDiff, 0, dragStartRightPos ?? 100);
                Note.RightPos = dragStartRightPos;
            }
        }

        if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
        {
            Note.StartTime = Math.Clamp(dragStartTime + timeDiff, 0, (int) ChartManager.ChartPackage.SongLength);
        }
        else
        {
            Note.StartTime = dragStartTime;
        }
    }
}
