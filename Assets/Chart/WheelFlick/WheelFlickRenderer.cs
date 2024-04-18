using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class WheelFlickRenderer : NoteRenderer<EditableWheelFlick>, IPointerClickHandler, IBeginDragHandler, IDragHandler
{
    // public WheelFlick Note;

    [SerializeField] Image image;

    [SerializeField] Sprite leftSprite;
    [SerializeField] Sprite rightSprite;

    [SerializeField] AudioClip flickLeftSound;
    [SerializeField] AudioClip flickRightSound;
    AudioSource audioSource;

    int dragStartTime;
    int dragDirChangePointerPos;

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

    protected override void OnLeftClick() => UIManager.SelectedNote = Note;
    protected override void OnRightClick() => Note.RemoveSelf();

    protected override void OnDragStart()
    {
        dragStartTime = Note.StartTime;
        dragDirChangePointerPos = dragStartPointerPos;
    }

    protected override void OnDrag()
    {
        int dragDirChangePosDiff = dragPos - dragDirChangePointerPos;
        if (dragDirChangePosDiff < -5)
        {
            Note.Dir = LeftRight.Left;
            dragDirChangePointerPos = dragPos;
        }
        else if (dragDirChangePosDiff > 5)
        {
            Note.Dir = LeftRight.Right;
            dragDirChangePointerPos = dragPos;
        }
        
        if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
        {
            Note.StartTime = Math.Clamp(dragStartTime + dragTimeDiff, 0, (int) ChartManager.ChartPackage.SongLength);
        }
        else
        {
            Note.StartTime = dragStartTime;
        }
    }
}
