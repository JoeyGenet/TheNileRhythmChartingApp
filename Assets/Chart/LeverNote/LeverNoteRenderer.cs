using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class LeverNoteRenderer : NoteRenderer<EditableLeverNote, EditableLeverSequence>
{
    [Header("Sprites")]
    [SerializeField] private Image arrow;
    [SerializeField] private Sprite arrowSprite;
    [SerializeField] private Sprite neutralSprite;
    [SerializeField] private Color leftColor;
    [SerializeField] private Color upColor;
    [SerializeField] private Color rightColor;
    [SerializeField] private Color downColor;
    [SerializeField] private Color middleColor;

    [Header("Hit Sounds")]
    [SerializeField] AudioClip leverSideSound;
    [SerializeField] AudioClip leverPushSound;
    [SerializeField] AudioClip leverPullSound;

    AudioSource audioSource;

    int dragStartTime;
    bool isLastSelectedVertex;
    Dictionary<EditableLeverNote, int> dragStartTimes;
    LeftRight dragStartDir;
    LeverPos dragStartLeverPos;
    Vector2 dragLeverPosChangePos;
    Dictionary<EditableLeverNote, LeverPos> oldLeverPositions;
    protected override void Awake()
    {
        base.Awake();
        audioSource = GetComponent<AudioSource>();
    }

    protected override void OnNoteStarted()
    {
        audioSource.clip = Note.Pos switch
        {
            LeverPos.Left => leverSideSound,
            LeverPos.Up => leverPushSound,
            LeverPos.Right => leverSideSound,
            LeverPos.Down => leverPullSound,
            LeverPos.Middle => leverPullSound,
            _ => throw new System.Exception($"Lever direction not supported.")
        };
        audioSource.Play();
    }

    protected override void Update()
    {
        base.Update();
        
        // Update position
        Vector2 uiPos = PlaybackManager.Instance.TrackToUIPosition(50, Note.StartTime);
        rect.anchorMin = new Vector2(rect.anchorMin.x, uiPos.y);
        rect.anchorMax = new Vector2(rect.anchorMax.x, uiPos.y);

        // update lever direction
        float angle;
        (arrow.sprite, angle, arrow.color) = Note.Pos switch
        {
            LeverPos.Left => (arrowSprite, 0, leftColor),
            LeverPos.Up => (arrowSprite, -90, upColor),
            LeverPos.Right => (arrowSprite, 180, rightColor),
            LeverPos.Down => (arrowSprite, 90, downColor),
            LeverPos.Middle => (neutralSprite, 0, middleColor),
            _ => throw new Exception($"Lever direction {Note.Pos} not supported."),
        };
        arrow.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    protected override void OnLeftClick() 
    {
        UIManager.ClickedElement = Note;
        UIManager.SelectedElement = Note.Container;
    }

    protected override void OnRightClick()
    {
        if (Note.Container.Notes.Count <= 1) 
        {
            Note.Container.RemoveSelf();
        }
        else
        {
            Note.RemoveSelf();
        }
    }


    protected override void OnDragStart()
    {   
        UIManager.SelectedElement = Note.Container;
        dragStartTime = Note.StartTime;
        dragStartTimes = Note.Container.Notes.ToDictionary(n => n, n => n.StartTime);
        dragStartLeverPos = Note.Pos;
        dragStartDir = Note.Container.Dir;
        dragLeverPosChangePos = pointerEventData.position;
        oldLeverPositions = Note.Container.Notes.ToDictionary(n => n, n => n.Pos);
        if (isInMarqueeSelection)
        {
            EditableLeverNote currentNote = Note.Container.Notes.Last();
            while (!UIManager.SelectionGroup.notes.Contains(currentNote) && currentNote.PreviousNote!=null)
            {
                currentNote = currentNote.PreviousNote;
            }
            isLastSelectedVertex = Note == currentNote;
        }
    }

    protected override void OnDrag()
    {
        bool changeTime = !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl);
        // do not allow for whole sequence dragging if this note is in marquee selection
        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && !isInMarqueeSelection)
        {
            int snappedTimeDiff = UIManager.SnapTime(dragStartTimes[Note] + dragTimeDiff) - dragStartTimes[Note];
            foreach (var note in dragStartTimes.Keys)
            {
                if (changeTime) note.StartTime = PlaybackManager.ClampTime(dragStartTimes[note] + snappedTimeDiff);
                else note.StartTime = dragStartTimes[note];
            }

            Note.Container.Dir = dragPos < 50 ? LeftRight.Left : LeftRight.Right;

            dragLeverPosChangePos = pointerEventData.position;
            Note.Pos = dragStartLeverPos;
        }
        else
        {
            // if we stop holding shift, reset note positions to their pos when drag started
            // ... unless this note is in a marquee selection
            if (!isInMarqueeSelection)
            {
                foreach (var note in dragStartTimes.Keys)
                {
                    note.StartTime = dragStartTimes[note];
                }
            }

            if (changeTime)
            {
                Note.StartTime = PlaybackManager.ClampTime(UIManager.SnapTime(dragStartTimes[Note] + dragTimeDiff));

                dragLeverPosChangePos = pointerEventData.position;
                Note.Pos = dragStartLeverPos;
            }
            else
            {
                Note.StartTime = dragStartTime;
                // while holding ctrl, change the lever position
                Vector2 changePosDiff = pointerEventData.position - dragLeverPosChangePos;
                if (Mathf.Abs(changePosDiff.x) > Mathf.Abs(changePosDiff.y))
                {
                    if (changePosDiff.x > 100)
                    {
                        Note.Pos = Note.Pos == LeverPos.Left ? LeverPos.Middle : LeverPos.Right;
                        dragLeverPosChangePos = pointerEventData.position;
                    }
                    else if (changePosDiff.x < -100)
                    {
                        Note.Pos = Note.Pos == LeverPos.Right ? LeverPos.Middle : LeverPos.Left;
                        dragLeverPosChangePos = pointerEventData.position;
                    }
                }
                else
                {
                    if (changePosDiff.y > 100)
                    {
                        Note.Pos = Note.Pos == LeverPos.Down ? LeverPos.Middle : LeverPos.Up;
                        dragLeverPosChangePos = pointerEventData.position;
                    }
                    else if (changePosDiff.y < -100)
                    {
                        Note.Pos = Note.Pos == LeverPos.Up ? LeverPos.Middle : LeverPos.Down;
                        dragLeverPosChangePos = pointerEventData.position;
                    }
                }
            }

            Note.Container.Dir = dragStartDir;
        }
    }

    protected override void OnEndDrag()
    {
        int dirDelta = Note.Container.Dir - dragStartDir;
        Dictionary<EditableLeverNote, int> newStartTimes = Note.Container.Notes.ToDictionary(n => n, n => n.StartTime);
        Dictionary<EditableLeverNote, LeverPos> newLeverPositions = Note.Container.Notes.ToDictionary(n => n, n => n.Pos);
        EditLeverSequenceAction editLeverSequenceAction = new EditLeverSequenceAction(Note.Container, dirDelta, dragStartTimes, newStartTimes, oldLeverPositions, newLeverPositions);
        
        if (!isInMarqueeSelection)
        {
            editLeverSequenceAction.CommitAction();
        }
        else if (isLastSelectedVertex)
        {
            UIManager.SelectionGroup.AddToMultiselectMoveAction(editLeverSequenceAction);
        }
    }
}
