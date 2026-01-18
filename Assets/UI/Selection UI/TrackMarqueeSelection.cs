using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// TODO: Should this class be a child of NoteRenderer? i.e. make this a type of "note" that is composed of other notes in selection?
[RequireComponent(typeof(RectTransform))]
public class TrackMarqueeSelection : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] Image marqueeImage;
    [SerializeField] Track Track;
    public List<Note> Notes { get; private set; }

    protected RectTransform rect;

    int prevTime = int.MaxValue;

    protected PointerEventData pointerEventData { get; private set; }
    protected int dragStartPointerPos { get; private set; }
    protected int dragStartPointerTime { get; private set; }
    protected int dragPos { get; private set; }
    protected int dragTime { get; private set; }
    protected int dragPosDiff { get; private set; }
    protected int dragTimeDiff { get; private set; }

    public void Init(Track track)
    {
        Track = track;
    }

    protected void Awake()
    {
        rect = GetComponent<RectTransform>();
        marqueeImage.enabled = false;
        UIManager.OnElementSelected += OnNoteSelected;
    }

    protected void OnDestroy()
    {
        UIManager.OnElementSelected -= OnNoteSelected;
    }

    // TODO: create UpdateOnPlayback that is called when the user drags the playback/zoom or during playback
    // and note is visible or startTime and endTime go in/out of view as a result

    protected void Update()
    {
        int currTime = PlaybackManager.Instance.MillisecondsInChart;
        prevTime = currTime;
    }

    void OnNoteSelected(object previousNote, object newNote)
    {
        
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("OnPointerClick()");
        pointerEventData = eventData;
        switch (eventData.button)
        {
            case PointerEventData.InputButton.Left:
                OnLeftClick();
                break;
            case PointerEventData.InputButton.Right:
                OnRightClick();
                break;
        }
    }

    protected void OnLeftClick() {}
    protected void OnRightClick() {}


    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("OnBeginDrag()");
        if (eventData.button != PointerEventData.InputButton.Left || UIManager.CursorMode != UICursorMode.None) return;
        pointerEventData = eventData;
        if (Track.PointerToTrackPosition(eventData.pressPosition, clamp: false) is not var (pos, time)) return;
        dragStartPointerPos = pos;
        dragStartPointerTime = time;
        OnDragStart();
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log("OnDrag()");
        if (eventData.button != PointerEventData.InputButton.Left) return;
        pointerEventData = eventData;
        if (Track.PointerToTrackPosition(eventData.position, clamp: false) is not var (dragPos, dragTime)) return;
        this.dragPos = dragPos;
        this.dragTime = dragTime;
        dragPosDiff = dragPos - dragStartPointerPos;
        dragTimeDiff = dragTime - dragStartPointerTime;
        OnDrag();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("OnEndDrag()");
        OnEndDrag();
    }

    protected void OnDragStart() 
    {
        Debug.Log("Start drag pos: "+Track.TrackToUIPosition(dragStartPointerPos, dragStartPointerTime));
        marqueeImage.enabled = true;
    }

    protected void OnDrag() 
    {
        Vector2 startUiPos = Track.TrackToUIPosition(dragStartPointerPos, dragStartPointerTime);
        Vector2 currentUiPos = Track.TrackToUIPosition(dragPos, dragTime);
        marqueeImage.rectTransform.pivot = new Vector2(currentUiPos.x > startUiPos.x ? 0 : 1, currentUiPos.y > startUiPos.y ? 0 : 1);
        marqueeImage.rectTransform.localPosition = startUiPos;
        marqueeImage.rectTransform.sizeDelta = new Vector2(Math.Abs(currentUiPos.x-startUiPos.x), Math.Abs(currentUiPos.y-startUiPos.y));
    }

    protected void OnEndDrag() 
    {
        marqueeImage.enabled = false;
    }

        void MultiSelect()
    {
        if (UIManager.CursorMode == UICursorMode.None)
        {
            bool selectStarted = Input.GetMouseButtonDown(0);
            var chart = ChartManager.SelectedChart;

            // get start timestamp and horizontal position when starting drag

            // get current timestamp and horizontal pos when dragging

            // highlight all notes that fall under current selection range

            // if release drag, click on any note to begin drag

        }
    }
}
