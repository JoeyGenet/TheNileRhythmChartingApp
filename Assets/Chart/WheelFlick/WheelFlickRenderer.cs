using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class WheelFlickRenderer : NoteRenderer<EditableWheelFlick>, IPointerDownHandler, IBeginDragHandler, IDragHandler
{
    // public WheelFlick Note;

    [SerializeField] Image image;

    [SerializeField] Sprite leftSprite;
    [SerializeField] Sprite rightSprite;

    [SerializeField] AudioClip flickLeftSound;
    [SerializeField] AudioClip flickRightSound;
    AudioSource audioSource;

    int dragStartTime;
    LeftRight oldDir;
    float dragDirChangePosX;

    protected override void Awake()
    {
        base.Awake();
        audioSource = GetComponent<AudioSource>();
    }

    protected override void OnNoteStarted()
    {
        audioSource.clip = Note.Dir == LeftRight.Left ? flickLeftSound : flickRightSound;
        audioSource.Play();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
        
        Vector2 uiPos = PlaybackManager.Instance.TrackToUIPosition(50, Note.StartTime);
        rect.anchorMax = new Vector2(rect.anchorMax.x, uiPos.y);
        rect.anchorMin = new Vector2(rect.anchorMin.x, uiPos.y);
        
        image.sprite = Note.Dir switch
        {
            LeftRight.Left => leftSprite,
            LeftRight.Right => rightSprite,
            _ => null,
        };
    }

    protected override void OnLeftClick() 
    {
        UIManager.ClickedElement = Note;
        UIManager.SelectedElement = Note;
    } 
    protected override void OnRightClick() => Note.RemoveSelf();

    protected override void OnDragStart()
    {
        UIManager.SelectedElement = Note;
        dragStartTime = Note.StartTime;
        oldDir = Note.Dir;
        dragDirChangePosX = pointerEventData.position.x;
    }

    protected override void OnDrag()
    {
        float dragDirChangePosXDiff = pointerEventData.position.x - dragDirChangePosX;
        bool changeTime = !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl);
        
        if ((!isInMarqueeSelection || (isInMarqueeSelection && !changeTime)) &&
            (dragDirChangePosXDiff < -50 || (dragDirChangePosXDiff < -5 && Note.Dir == LeftRight.Left)))
        {
            Note.Dir = LeftRight.Left;
            dragDirChangePosX = pointerEventData.position.x;
        }
        else if ((!isInMarqueeSelection || (isInMarqueeSelection && !changeTime)) &&
                 (dragDirChangePosXDiff > 50 || (dragDirChangePosXDiff > 5 && Note.Dir == LeftRight.Right)))
        {
            Note.Dir = LeftRight.Right;
            dragDirChangePosX = pointerEventData.position.x;
        }
        
        if (changeTime)
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
        int dirDelta = Note.Dir - oldDir;
        EditWheelFlickAction editWheelFlickAction = new EditWheelFlickAction(Note, startTimeDelta, dirDelta);
        
        if (!isInMarqueeSelection)
        {
            editWheelFlickAction.CommitAction();
        }
        else
        {
            Track.SelectionGroup.multiselectMoveAction.AddUndoRedoAction(editWheelFlickAction);
        }
    }
}
