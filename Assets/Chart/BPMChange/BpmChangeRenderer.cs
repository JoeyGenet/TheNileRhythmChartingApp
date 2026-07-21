using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BpmChangeRenderer : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Track Track { get; private set; }
    public EditableBpmChangeEvent Event { get; private set; }

    RectTransform rect;

    [SerializeField] protected Animator animator;

    int dragStartTimestamp;
    int dragStartPointerTime;
    int dragStartStartTime;
    uint oldBpm;

    // drag state fields+properties
    protected PointerEventData pointerEventData { get; private set; }

    // set to true in OnPointerDown, false in OnPointerUp
    protected bool isBeingPressed = false;

    // Event.isBeingDragged set to true while drag is in progress
    // set to false if note is deselected or drag is completed
    // Event.isDraggingStart/EndHandle set for dragging handles individually or together
    protected bool IsBeingDragged 
    {
        get { return Event.isBeingDragged; }
        private set
        {
            Event.isBeingDragged = value;
            if (animator != null)
            {
                animator.SetBool("IsDragged", Event.isBeingDragged);
            }
        }
    }

    // set to true when event is deselected while Event.isBeingDragged is true
    private bool isDragCancelled = false;

    public virtual void Init(Track track, EditableBpmChangeEvent evt)
    {
        Track = track;
        Event = evt;

        if (Event != null) Event.Chart.OnBpmChangeRemoved += DestroyOnNoteRemoved;
    }

    void Awake()
    {
        rect = GetComponent<RectTransform>();

        ChartManager.OnChartChanged += DestroyOnChartChanged;
        UIManager.OnElementSelected += OnElementSelected;
        UIManager.OnDragCanceled += OnDragCanceled;
        UIManager.OnAnimationToggled += OnAnimationToggled;
    }

    void Start()
    {

    }

    void OnDestroy()
    {
        ChartManager.OnChartChanged -= DestroyOnChartChanged;
        UIManager.OnElementSelected -= OnElementSelected;
        UIManager.OnDragCanceled -= OnDragCanceled;
        if (Event != null) Event.Chart.OnBpmChangeRemoved -= DestroyOnNoteRemoved;
        UIManager.OnAnimationToggled -= OnAnimationToggled;
    }

    void Update()
    {
        // Update position
        Vector2 uiPos = PlaybackManager.Instance.TrackToUIPosition(50, Event.StartTime);
        rect.anchorMax = new Vector2(rect.anchorMax.x, uiPos.y);
        rect.anchorMin = new Vector2(rect.anchorMin.x, uiPos.y);

        // start dragging if we scroll/enter playback while mouse is held over note
        // if drag via scroll wheel or playback, call OnDrag with the previous dragstart/end settings
        if (isBeingPressed && (UIManager.Track.IsScrolling || !PlaybackManager.Instance.IsPaused))
        {
            isBeingPressed = false;
            OnBeginDrag(pointerEventData);
        }

        // toggle drag scrolling
        // this tells the Track to calculate additional scroll amount if the mouse hovers
        // over the top or bottom of the track
        bool shouldDragScroll = PlaybackManager.Instance.IsPaused && Event.isBeingDragged;
        UIManager.Track.IsDragScrolling = shouldDragScroll || UIManager.Track.IsDragScrolling;
        
        // manually call OnDrag if track is scrolling or in playback
        if (Event.isBeingDragged && (UIManager.Track.scrollAmount != 0 || !PlaybackManager.Instance.IsPaused))
        {
            Drag(pointerEventData);
        }
    }

    private void OnElementSelected(object previousElement, object newElement)
    {
        if (newElement == Event)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }
        else
        {
            // gameObject.SetActive(false);
        }
    }

    public void OnDragCanceled()
    {
        if (Event.isBeingDragged)
        {
            CancelDrag();
        }
    }

    public void CancelDrag()
    {
        // return to timestamp of drag start
        PlaybackManager.Instance.SetTimestamp(dragStartTimestamp);
        // reset BPM change position
        Event.StartTime = dragStartStartTime;
        Event.BPM = oldBpm;
        // update flags
        isDragCancelled = true;
        IsBeingDragged = false;
        UIManager.IsDragInProgress = false;
    }

    void DestroyOnChartChanged(EditableChart prevChart, EditableChart newChart)
    {
        Destroy(gameObject);
    }

    void DestroyOnNoteRemoved(EditableBpmChangeEvent evt)
    {
        if (evt == Event)
        {
            Destroy(gameObject);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        PointerDown(eventData);
    }

    public void PointerDown(PointerEventData eventData)
    {
        pointerEventData = eventData;
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
                if (Event.isBeingDragged)
                {
                    OnEndDrag(eventData);
                }
                else
                {
                    animator.SetTrigger("IsClicked");
                }
                break;
            case PointerEventData.InputButton.Right:
                // track the deletion for undo/redo
                RemoveBpmChangeAction removeBpmChangeAction = new RemoveBpmChangeAction(Event, Event.Chart);
                removeBpmChangeAction.CommitAction();
                Event.Chart.RemoveBpmChange(Event);
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
        if (!Event.isBeingDragged)
        {
            UIManager.SelectElement(Event, updateEditHistory:false);
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (Track.PointerToTrackPosition(eventData.pressPosition, clamp: false) is not var (pos, time)) return;
            dragStartTimestamp = PlaybackManager.Instance.MillisecondsInChart;
            dragStartPointerTime = time;
            dragStartStartTime = Event.StartTime;
            isBeingPressed = false;
            IsBeingDragged = true;
            isDragCancelled = false;
            oldBpm = Event.BPM;
            UIManager.IsDragInProgress = true;
        }
    }
    void Drag(PointerEventData eventData)
    {
        if (!isDragCancelled)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (Track.PointerToTrackPosition(eventData.position, clamp: false) is not var (dragPos, dragTime)) return;
            int dragTimeDiff = dragTime - dragStartPointerTime;
            Event.StartTime = SnapTime(dragStartStartTime + dragTimeDiff);
        }
    }
    public void OnDrag(PointerEventData eventData)
    {
        Drag(eventData);
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragCancelled && Event.isBeingDragged)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            int startTimeDelta = Event.StartTime - dragStartStartTime;
            uint bpmDelta = Event.BPM - oldBpm;
            EditBpmChangeAction editBpmChangeAction = new EditBpmChangeAction (Event, startTimeDelta, bpmDelta);
            editBpmChangeAction.CommitAction();
        }
        IsBeingDragged = false;
        isDragCancelled = false;
        UIManager.IsDragInProgress = false;
    }
    // the following functions are called from EventTrigger components on the the BpmChange prefab
    // so that the separate handles can all trigger drag events
    public void OnBeginDrag(BaseEventData eventData)
    {
        if (eventData is not PointerEventData pointerEventData) return;
        OnBeginDrag(pointerEventData);
    }
    public void OnEndDrag(BaseEventData eventData)
    {
        if (eventData is not PointerEventData pointerEventData) return;
        OnEndDrag(pointerEventData);
    }

    // custom snapping functions
    public int SnapTime(int time)
    {
        if (UIManager.Snapping > 0)
        {
            float beat = TimeToBeat(time);
            beat = Mathf.Round(beat * UIManager.Snapping) / UIManager.Snapping;
            return BeatToTime(beat);
        }
        return time;
    }

    public int BeatToTime(float beat) 
    {
        int bpmOffset = ChartManager.ChartPackage.BPMOffset;
        float startingBpm = ChartManager.ChartPackage.BPM;
        int time;
        float beatCount;
        // there are no BPM changes before this one, use chart's starting BPM
        if (Event.PreviousBpmChange == null)
        {
            time = (int) (beat * 60000f / startingBpm + bpmOffset);
        }
        else
        {
            var sortedBpmChanges = ChartManager.SelectedChart.BPMChanges.OrderBy(evt => evt.StartTime).ToList();
            time = sortedBpmChanges.First().StartTime + bpmOffset;
            beatCount = startingBpm * sortedBpmChanges.First().StartTime / 60000f;
            foreach (EditableBpmChangeEvent bpmChange in sortedBpmChanges)
            {
                // sum time durations in all bpm changes before this one
                if (bpmChange != Event.PreviousBpmChange)
                {
                    time += bpmChange.Duration; 
                    beatCount += bpmChange.BeatCount; 
                }
                // use the duration of the last bpm change to determine when this one should occur
                else
                {
                    time += (int)((beat - beatCount) / bpmChange.BeatCount * bpmChange.Duration);
                    break;
                }
            }
        }
        return time;
    }

	public float TimeToBeat(int time)
    {
        int bpmOffset = ChartManager.ChartPackage.BPMOffset;
        float startingBpm = ChartManager.ChartPackage.BPM;
        int offsetTime = time - bpmOffset;
        float beatNumber;
        // there are no BPM changes before this one, use chart's starting BPM
        if (Event.PreviousBpmChange == null)
        {
            beatNumber = offsetTime * startingBpm / 60000f;
        }
        else
        {
            var sortedBpmChanges = ChartManager.SelectedChart.BPMChanges.OrderBy(evt => evt.StartTime).ToList();
            beatNumber = sortedBpmChanges.First().StartTime * startingBpm / 60000f;
            foreach (EditableBpmChangeEvent bpmChange in sortedBpmChanges)
            {
                // count up beats in all bpm changes before this one
                if (bpmChange != Event.PreviousBpmChange)
                {
                    beatNumber += bpmChange.BeatCount; 
                }
                // use BPM of previous bpm change to determine final beat
                else
                {
                    beatNumber += bpmChange.BPM * (offsetTime - bpmChange.StartTime) / 60000f;
                    break;
                }
            }
        }
        return beatNumber;
    }

    public virtual void OnAnimationToggled(bool isAnimationEnabled)
    {
        if (animator != null)
        {
            animator.SetBool("IsAnimationEnabled", isAnimationEnabled);
        }
    }
}
