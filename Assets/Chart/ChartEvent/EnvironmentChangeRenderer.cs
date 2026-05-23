using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class EnvironmentChangeRenderer : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Track Track { get; private set; }
    public EditableEnvironmentChangeEvent Event { get; private set; }

    RectTransform rect;

    [SerializeField] Image gradientStartColor;
    [SerializeField] Image gradientEndColor;
    [SerializeField] Image startHandleColor;
    [SerializeField] Image endHandleColor;

    [SerializeField] EventTrigger startHandle;
    [SerializeField] EventTrigger endHandle;

    int dragStartTimestamp;
    int dragStartPointerTime;
    int dragStartStartTime;
    int dragStartEndTime;
    int oldDuration;
    Environment oldEnvironment;

    // drag state fields+properties
    protected PointerEventData pointerEventData { get; private set; }

    // set to true in OnPointerDown, false in OnPointerUp
    protected bool isBeingPressed = false;

    // set to true while drag is in progress
    // set to false if note is deselected or drag is completed
    protected bool isBeingDragged = false;

    // set to true when note is deselected while isBeingDragged is true
    private bool isDragCancelled = false;

    public virtual void Init(Track track, EditableEnvironmentChangeEvent evt)
    {
        Track = track;
        Event = evt;

        if (Event != null) Event.Chart.OnEventRemoved += DestroyOnNoteRemoved;
    }

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        
        gameObject.SetActive(false);
        // static void setAlpha(Image image, float alpha)
        //     => image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
        // setAlpha(gradientStartColor, 0);
        // setAlpha(gradientEndColor, 0);
        // startHandle.gameObject.SetActive(false);
        // endHandle.gameObject.SetActive(false);

        ChartManager.OnChartChanged += DestroyOnChartChanged;
        UIManager.OnElementSelected += OnElementSelected;
        UIManager.OnDragCanceled += OnDragCanceled;
    }

    void Start()
    {

    }

    void OnDestroy()
    {
        ChartManager.OnChartChanged -= DestroyOnChartChanged;
        UIManager.OnElementSelected -= OnElementSelected;
        UIManager.OnDragCanceled -= OnDragCanceled;
        if (Event != null) Event.Chart.OnEventRemoved -= DestroyOnNoteRemoved;
    }

    void Update()
    {
        // Update line position
        Vector2 startUIPos = PlaybackManager.Instance.TrackToUIPosition(50, Event.StartTime);
        Vector2 endUIPos = PlaybackManager.Instance.TrackToUIPosition(50, Event.EndTime);
        rect.anchorMin = new Vector2(rect.anchorMin.x, startUIPos.y);
        rect.anchorMax = new Vector2(rect.anchorMax.x, endUIPos.y);

        // Update colors
        static void setColor(Image image, Color color)
            => image.color = new Color(color.r, color.g, color.b, image.color.a);
        setColor(gradientEndColor, Event.Environment.Color);
        setColor(endHandleColor, Event.Environment.Color);
        setColor(gradientStartColor, Event.PreviousEnvironment.Color);
        setColor(startHandleColor, Event.PreviousEnvironment.Color);

        // start dragging if we scroll/enter playback while mouse is held over note
        // if drag via scroll wheel or playback, call OnDrag with the previous dragstart/end settings
        if (isBeingPressed && (UIManager.Track.IsScrolling || !PlaybackManager.Instance.IsPaused))
        {
            isBeingPressed = false;
            OnBeginDrag(pointerEventData);
        }

        // toggle drag scrolling
        bool shouldDragScroll = PlaybackManager.Instance.IsPaused && isBeingDragged;
        UIManager.Track.IsDragScrolling = shouldDragScroll || UIManager.Track.IsDragScrolling;
        
        // drag note if track is scrolling or in playback
        if (shouldDragScroll && UIManager.Track.scrollAmount != 0 || (!PlaybackManager.Instance.IsPaused && isBeingDragged))
        {
            bool isHoldingShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            Drag(pointerEventData, dragStart: Event.isDraggingStartHandle || isHoldingShift, dragEnd: Event.isDraggingEndHandle || isHoldingShift);
        }
    }

    private void OnElementSelected(object previousElement, object newElement)
    {
        static void setAlpha(Image image, float alpha)
            => image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
        if (newElement == Event)
        {
            gameObject.SetActive(true);
            // setAlpha(gradientStartColor, 1);
            // setAlpha(gradientEndColor, 1);
            // startHandle.gameObject.SetActive(true);
            // endHandle.gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }
        else
        {
            gameObject.SetActive(false);
            // setAlpha(gradientStartColor, 0);
            // setAlpha(gradientEndColor, 0);
            // startHandle.gameObject.SetActive(false);
            // endHandle.gameObject.SetActive(false);
        }
    }

    public void OnDragCanceled()
    {
        if (isBeingDragged)
        {
            CancelDrag();
        }
    }

    public void CancelDrag()
    {
        // return to timestamp of drag start
        PlaybackManager.Instance.SetTimestamp(dragStartTimestamp);
        // reset env change position
        Event.EndTime = dragStartEndTime;
        Event.StartTime = dragStartStartTime;
        Event.Duration = oldDuration;
        Event.Environment = oldEnvironment;
        // update flags
        isDragCancelled = true;
        isBeingDragged = false;
        UIManager.IsDragInProgress = false;
    }

    void DestroyOnChartChanged(EditableChart prevChart, EditableChart newChart)
    {
        Destroy(gameObject);
    }

    void DestroyOnNoteRemoved(EditableEnvironmentChangeEvent evt)
    {
        if (evt == Event)
        {
            Destroy(gameObject);
        }
    }

    // when you click on the middle of the environment transition
    // drag both the start and end handles
    public void OnPointerDown(PointerEventData eventData)
    {
        PointerDown(eventData, dragStart: true, dragEnd: true);
    }

    public void PointerDown(PointerEventData eventData, bool dragStart, bool dragEnd)
    {
        pointerEventData = eventData;
        Event.isDraggingStartHandle = dragStart;
        Event.isDraggingEndHandle = dragEnd;
        switch (eventData.button)
        {
            case PointerEventData.InputButton.Left:
                isBeingPressed = true;

                // if we drag without moving the mouse (i.e. during playback)
                // we need to manually trigger the OnBeginDrag function
                if (!PlaybackManager.Instance.IsPaused)
                {
                    OnBeginDrag(eventData);
                }
                break;
            case PointerEventData.InputButton.Right:
                break;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        switch (eventData.button)
        {
            case PointerEventData.InputButton.Left:
                isBeingPressed = false;
                UIManager.SelectElement(Event, updateEditHistory:true);
                // if we dragged without moving the mouse (i.e. via scrolling or during playback)
                // we need to manually trigger the OnEndDrag function
                if (isBeingDragged)
                {
                    OnEndDrag(eventData);
                }
                break;
            case PointerEventData.InputButton.Right:
                // track the deletion for undo/redo
                RemoveEnvironmentChangeAction removeEnvironmentChangeAction = new RemoveEnvironmentChangeAction(Event, Event.Chart);
                removeEnvironmentChangeAction.CommitAction();
                Event.Chart.RemoveEvent(Event);
                UIManager.SelectElement(null); // TODO: find better way to unfocus the event inspector
                break;
        }
    }
    public void OnHandlePointerUp(BaseEventData eventData)
    {
        if (eventData is not PointerEventData pointerEventData) return;
        OnPointerUp(pointerEventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isBeingDragged)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (Track.PointerToTrackPosition(eventData.pressPosition, clamp: false) is not var (pos, time)) return;
            dragStartTimestamp = PlaybackManager.Instance.MillisecondsInChart;
            dragStartPointerTime = time;
            dragStartStartTime = Event.StartTime;
            dragStartEndTime = Event.EndTime;
            oldDuration = Event.Duration;
            oldEnvironment = Event.Environment;
            isBeingPressed = false;
            isBeingDragged = true;
            isDragCancelled = false;
            UIManager.IsDragInProgress = true;
        }
    }
    void Drag(PointerEventData eventData, bool dragStart, bool dragEnd)
    {
        if (!isDragCancelled)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (Track.PointerToTrackPosition(eventData.position, clamp: false) is not var (dragPos, dragTime)) return;
            int dragTimeDiff = dragTime - dragStartPointerTime;
            if (dragTimeDiff > 0)
            {
                if (dragEnd) Event.EndTime = UIManager.SnapTime(dragStartEndTime + dragTimeDiff);
                else Event.EndTime = dragStartEndTime;
                if (dragStart) Event.StartTime = UIManager.SnapTime(dragStartStartTime + dragTimeDiff);
                else Event.StartTime = dragStartStartTime;
            }
            else if (dragTimeDiff < 0)
            {
                if (dragStart) Event.StartTime = UIManager.SnapTime(dragStartStartTime + dragTimeDiff);
                else Event.StartTime = dragStartStartTime;
                if (dragEnd) Event.EndTime = UIManager.SnapTime(dragStartEndTime + dragTimeDiff);
                else Event.EndTime = dragStartEndTime;
            }
        }
    }
    public void OnDrag(PointerEventData eventData)
    {
        Drag(eventData, dragStart: true, dragEnd: true);
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragCancelled && isBeingDragged)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            int startTimeDelta = Event.StartTime - dragStartStartTime;
            int endTimeDelta = Event.EndTime - dragStartEndTime;
            EditEnvironmentChangeAction editEnvironmentChangeAction = new EditEnvironmentChangeAction
            (
                Event, startTimeDelta, endTimeDelta, oldEnvironment, Event.Environment
            );
            editEnvironmentChangeAction.CommitAction();
        }
        isBeingDragged = false;
        isDragCancelled = false;
        Event.isDraggingStartHandle = false;
        Event.isDraggingEndHandle = false;
        UIManager.IsDragInProgress = false;
    }
    // the following functions are called from EventTrigger components on the the EnvironmentChange prefab
    // so that the separate handles can all trigger drag events
    public void OnHandleBeginDrag(BaseEventData eventData)
    {
        if (eventData is not PointerEventData pointerEventData) return;
        OnBeginDrag(pointerEventData);
    }
    public void OnStartHandlePointerDown(BaseEventData eventData)
    {
        if (eventData is not PointerEventData pointerEventData) return;
        PointerDown(pointerEventData, dragStart: true, dragEnd: false);
    }
    public void OnStartHandleDrag(BaseEventData eventData)
    {
        if (eventData is not PointerEventData pointerEventData) return;
        Drag(pointerEventData, dragStart: true, dragEnd: Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
    }
    public void OnEndHandlePointerDown(BaseEventData eventData)
    {
        if (eventData is not PointerEventData pointerEventData) return;
        PointerDown(pointerEventData, dragStart: false, dragEnd: true);
    }
    public void OnEndHandleDrag(BaseEventData eventData)
    {
        if (eventData is not PointerEventData pointerEventData) return;
        Drag(pointerEventData, dragStart: Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift), dragEnd: true);
    }
    public void OnHandleEndDrag(BaseEventData eventData)
    {
        if (eventData is not PointerEventData pointerEventData) return;
        OnEndDrag(pointerEventData);
    }
}
