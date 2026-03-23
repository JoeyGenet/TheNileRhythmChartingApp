using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class EnvironmentChangeRenderer : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
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

    int dragStartPointerTime;
    int dragStartStartTime;
    int dragStartEndTime;
    int oldDuration;
    Environment oldEnvironment;

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
    }

    void Start()
    {

    }

    void OnDestroy()
    {
        ChartManager.OnChartChanged -= DestroyOnChartChanged;
        UIManager.OnElementSelected -= OnElementSelected;
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

    public void OnPointerClick(PointerEventData eventData)
    {
        switch (eventData.button)
        {
            case PointerEventData.InputButton.Left:
                UIManager.SelectElement(Event);
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
    public void OnHandlePointerClick(BaseEventData eventData)
    {
        if (eventData is not PointerEventData pointerEventData) return;
        OnPointerClick(pointerEventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (Track.PointerToTrackPosition(eventData.pressPosition, clamp: false) is not var (pos, time)) return;
        dragStartPointerTime = time;
        dragStartStartTime = Event.StartTime;
        dragStartEndTime = Event.EndTime;
        oldDuration = Event.Duration;
        oldEnvironment = Event.Environment;
    }
    void Drag(PointerEventData eventData, bool dragStart, bool dragEnd)
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
    public void OnDrag(PointerEventData eventData)
    {
        Drag(eventData, dragStart: true, dragEnd: true);
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        int startTimeDelta = Event.StartTime - dragStartStartTime;
        int endTimeDelta = Event.EndTime - dragStartEndTime;
        int durationDelta = Event.Duration - oldDuration;
        EditEnvironmentChangeAction editEnvironmentChangeAction = new EditEnvironmentChangeAction
        (
            Event, startTimeDelta, endTimeDelta, oldEnvironment, Event.Environment
        );
        editEnvironmentChangeAction.CommitAction();
    }
    // the following functions are called from EventTrigger components on the the EnvironmentChange prefab
    // so that the separate handles can all trigger drag events
    public void OnHandleBeginDrag(BaseEventData eventData)
    {
        if (eventData is not PointerEventData pointerEventData) return;
        OnBeginDrag(pointerEventData);
    }
    public void OnStartHandleDrag(BaseEventData eventData)
    {
        if (eventData is not PointerEventData pointerEventData) return;
        Drag(pointerEventData, dragStart: true, dragEnd: Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
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
