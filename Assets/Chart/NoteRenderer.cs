using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class NoteRenderer<NoteType> : NoteRenderer<NoteType, EditableChart, Note>
where NoteType : Note, IEditableNote<EditableChart, Note>
{}

public abstract class NoteRenderer<NoteType, C> : NoteRenderer<NoteType, C, NoteType>
where NoteType : Note, IEditableNote<C, NoteType>
where C : IEditableNoteContainer<NoteType>
{}

[RequireComponent(typeof(RectTransform))]
public abstract class NoteRenderer<NoteType, C, N> : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler
where NoteType : N, IEditableNote<C, N>
where N : Note
where C : IEditableNoteContainer<N>
{
    protected Track Track;
    protected NoteType Note;

    protected RectTransform rect;

    int prevTime = int.MaxValue;

    protected PointerEventData pointerEventData { get; private set; }
    protected int dragStartPointerPos { get; private set; }
    protected int dragStartPointerTime { get; private set; }
    protected int dragPos { get; private set; }
    protected int dragTime { get; private set; }
    protected int dragPosDiff { get; private set; }
    protected int dragTimeDiff { get; private set; }

    public virtual void Init(Track track, NoteType note)
    {
        Track = track;
        Note = note;
        
        Note.Container.OnNoteRemoved += DestroyOnNoteRemoved;
    }

    protected virtual void Awake()
    {
        rect = GetComponent<RectTransform>();

        ChartManager.OnChartChanged += DestroyOnChartChanged;
    }

    protected virtual void OnDestroy()
    {
        ChartManager.OnChartChanged -= DestroyOnChartChanged;
        Note.Container.OnNoteRemoved -= DestroyOnNoteRemoved;
    }

    // TODO: create UpdateOnPlayback that is called when the user drags the playback/zoom or during playback
    // and note is visible or startTime and endTime go in/out of view as a result

    // Methods called when the note starts or ends on playback (not when user is scrolling)
    protected virtual void OnNoteStarted() {}
    protected virtual void OnNoteEnded() {}


    protected virtual void Update()
    {
        int currTime = PlaybackManager.Instance.MillisecondsInChart;
        if (!Track.IsScrolling && prevTime < Note.StartTime && currTime >= Note.StartTime) OnNoteStarted();
        if (!Track.IsScrolling && prevTime < Note.EndTime && currTime >= Note.EndTime) OnNoteStarted();
        prevTime = currTime;
    }

    void DestroyOnChartChanged(EditableChart prevChart, EditableChart newChart)
    {
        Destroy(gameObject);
    }

    void DestroyOnNoteRemoved(N note)
    {
        if (note == Note)
        {
            Destroy(gameObject);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
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

    protected virtual void OnLeftClick() {}
    protected virtual void OnRightClick() {}


    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        pointerEventData = eventData;
        if (Track.PointerToTrackPosition(eventData.pressPosition, clamp: false) is not var (pos, time)) return;
        dragStartPointerPos = pos;
        dragStartPointerTime = time;
        OnDragStart();
    }

    public virtual void OnDrag(PointerEventData eventData)
    {
        pointerEventData = eventData;
        if (Track.PointerToTrackPosition(eventData.position, clamp: false) is not var (dragPos, dragTime)) return;
        this.dragPos = dragPos;
        this.dragTime = dragTime;
        dragPosDiff = dragPos - dragStartPointerPos;
        dragTimeDiff = dragTime - dragStartPointerTime;
        OnDrag();
    }

    protected virtual void OnDragStart() {}
    protected virtual void OnDrag() {}
}
