using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class EnvironmentChangeRenderer : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler
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

    public virtual void Init(Track track, EditableEnvironmentChangeEvent evt)
    {
        Track = track;
        Event = evt;

        if (Event != null) Event.Chart.OnEventRemoved += DestroyOnNoteRemoved;
    }

    void Awake()
    {
        rect = GetComponent<RectTransform>();

        ChartManager.OnChartChanged += DestroyOnChartChanged;
    }

    void OnDestroy()
    {
        ChartManager.OnChartChanged -= DestroyOnChartChanged;
        if (Event != null) Event.Chart.OnEventRemoved -= DestroyOnNoteRemoved;
    }

    void Update()
    {
        // Update line position
        Vector2 startUIPos = PlaybackManager.Instance.TrackToUIPosition(50, Event.StartTime);
        Vector2 endUIPos = PlaybackManager.Instance.TrackToUIPosition(50, Event.EndTime);
        rect.anchorMin = new Vector2(rect.anchorMin.x, startUIPos.y);
        rect.anchorMax = new Vector2(rect.anchorMax.x, endUIPos.y);
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
                UIManager.SelectedElement = Event;
                break;
            case PointerEventData.InputButton.Right:
                Event.Chart.RemoveEvent(Event);
                break;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (Track.PointerToTrackPosition(eventData.pressPosition, clamp: false) is not var (pos, time)) return;
        dragStartPointerTime = time;
        dragStartStartTime = Event.StartTime;
        dragStartEndTime = Event.EndTime;
    }
    void Drag(PointerEventData eventData, bool dragStart, bool dragEnd)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (Track.PointerToTrackPosition(eventData.position, clamp: false) is not var (dragPos, dragTime)) return;
        int dragTimeDiff = dragTime - dragStartPointerTime;
        if (dragTimeDiff > 0)
        {
            if (dragEnd) Event.EndTime = UIManager.SnapTime(dragStartEndTime + dragTimeDiff);
            if (dragStart) Event.StartTime = UIManager.SnapTime(dragStartStartTime + dragTimeDiff);
        }
        else if (dragTimeDiff < 0)
        {
            if (dragStart) Event.StartTime = UIManager.SnapTime(dragStartStartTime + dragTimeDiff);
            if (dragEnd) Event.EndTime = UIManager.SnapTime(dragStartEndTime + dragTimeDiff);
        }
    }
    public void OnDrag(PointerEventData eventData)
    {
        Drag(eventData, dragStart: true, dragEnd: true);
    }
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
}
