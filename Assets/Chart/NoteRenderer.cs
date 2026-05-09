using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// used by singular notes or whole note sequences
// i.e. notes that are contained by a chart
public abstract class NoteRenderer<NoteType> : NoteRenderer<NoteType, EditableChart, Note>
where NoteType : Note, IEditableNote<EditableChart, Note>
{}

// used for individual notes in sequences
// i.e. notes that are contained in other notes
public abstract class NoteRenderer<NoteType, C> : NoteRenderer<NoteType, C, NoteType>
where NoteType : Note, IEditableNote<C, NoteType>
where C : IEditableNoteContainer<NoteType>
{}

[RequireComponent(typeof(RectTransform))]
public abstract class NoteRenderer<NoteType, C, N> : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
where NoteType : N, IEditableNote<C, N>
where N : Note
where C : IEditableNoteContainer<N>
{
    [SerializeField] protected Animator animator;
    public Track Track { get; private set; }
    public NoteType Note { get; private set; }
    public bool isInMarqueeSelection;
    public bool modifyExistingSelection;
    public bool addToSelection;

    protected RectTransform rect;

    int prevTime = int.MaxValue;

    protected PointerEventData pointerEventData { get; private set; }
    protected int dragStartPointerPos { get; private set; }
    protected int dragStartPointerTime { get; private set; }
    protected int dragPos { get; private set; }
    protected int dragTime { get; private set; }
    protected int dragPosDiff { get; private set; }
    protected int dragTimeDiff { get; private set; }
    protected int dragPosDelta { get; private set; }
    protected int dragTimeDelta { get; private set; }
    private int lastDragPos;
    private int lastDragTime;

    private bool isReadyToShareEventData;

    // set to true while drag is in progress
    // set to false if note is deselected or drag is completed
    protected bool isBeingDragged = false;

    // set to true when note is deselected while isBeingDragged is true
    private bool isDragCancelled = false;

    public virtual void Init(Track track, NoteType note)
    {
        Track = track;
        Note = note;

        // set up mapping between notes and their renderer game objects
        if (Note != null) track.noteGameObjectMapping.Add(note, gameObject);

        if (Note != null) note.Container.OnNoteRemoved += DestroyOnNoteRemoved;

        // prepare notes to share event data with other selected notes
        isReadyToShareEventData = true;
    }

    protected virtual void Awake()
    {
        rect = GetComponent<RectTransform>();

        ChartManager.OnChartChanged += DestroyOnChartChanged;
        UIManager.OnElementSelected += OnNoteSelected;
    }

    protected virtual void OnDestroy()
    {
        ChartManager.OnChartChanged -= DestroyOnChartChanged;
        UIManager.OnElementSelected -= OnNoteSelected;
        if (Note != null) 
        {
            Track.noteGameObjectMapping.Remove(Note);
            Note.Container.OnNoteRemoved -= DestroyOnNoteRemoved;
        }
    }

    // TODO: create UpdateOnPlayback that is called when the user drags the playback/zoom or during playback
    // and note is visible or startTime and endTime go in/out of view as a result

    // Methods called when the note starts or ends on playback (not when user is scrolling)
    protected virtual void OnNoteStarted() {}
    protected virtual void OnNoteEnded() {}

    protected virtual void Update()
    {
        // scroll track
        int scrollAmount = 0;
        if (PlaybackManager.Instance.IsPaused && isBeingDragged && UIManager.SelectionGroup.eventSenderNote==Note)
        {
            scrollAmount = UIManager.Track.DragScrollTrack();
        }
        // drag note if scrolling or in playback
        if (scrollAmount != 0 || (!PlaybackManager.Instance.IsPaused && isBeingDragged))
        {
            OnDrag(pointerEventData);
        }

        int currTime = PlaybackManager.Instance.MillisecondsInChart;
        if (!PlaybackManager.Instance.IsPaused && !isBeingDragged && !Track.IsScrolling && prevTime < Note.StartTime && currTime >= Note.StartTime) OnNoteStarted();
        if (!PlaybackManager.Instance.IsPaused && !isBeingDragged && !Track.IsScrolling && prevTime < Note.EndTime && currTime >= Note.EndTime) OnNoteStarted();
        prevTime = currTime;

        if (animator != null)
        {
            animator.SetBool("IsMultiSelected", isInMarqueeSelection);
        }
    }

    protected void AddOrSubtractNoteFromSelection(Note note, Note sequenceContainer=null)
    {
        SortedSet<Note> selectedNotes = new SortedSet<Note>();
        
        // create new selection from previously selected note
        // only include it if there is no current selection
        // and if it wasn't just cleared from a selection
        if (UIManager.SelectedElement!=null && UIManager.SelectedElement!=UIManager.SelectionGroup && addToSelection && 
            UIManager.ClickedElement is Note previousNote && !UIManager.SelectionGroup.lastDeselectedNotes.Contains(previousNote))
        {
            selectedNotes.Add(previousNote);
        }

        // for sequence notes, add/remove the sequence itself
        if (sequenceContainer != null)
        {
            selectedNotes.Add(sequenceContainer);
        }

        // add/remove the clicked note to the group
        selectedNotes.Add(note);
        UIManager.MultiselectElements(addToSelection, selectedNotes, true);
    }

    protected void SelectNoteOrMultiselection(Note note, bool updateEditHistory=true)
    {
        // if this note is in selection group, select the group
        if (isInMarqueeSelection)
        {
            UIManager.SelectElement(UIManager.SelectionGroup, updateEditHistory);
        }
        // if this note is outside selection group, deselect selection group and select this
        else
        {
            UIManager.SelectElement(note, updateEditHistory);
        }
    }

    private void OnNoteSelected(object previousNote, object newNote)
    {
        if (Note != null && newNote == Note) transform.SetAsLastSibling();

        // handle deselection of previous note/selection group
        if (isBeingDragged)
        {
            // if this note is deselected while in multiselection, deselect all other notes
            if (isInMarqueeSelection && previousNote==UIManager.SelectionGroup && UIManager.SelectionGroup.eventSenderNote==Note && isReadyToShareEventData)
            {
                Debug.Log("passing cancel drag to all selected notes!");
                isReadyToShareEventData = false;
                UIManager.SelectionGroup.CancelMultiselectMoveAction();
            }
            else if (previousNote==Note ||
                    (Note is EditableWheelTurn turn && turn.Container==previousNote) ||
                    (Note is EditableLeverNote note && note.Container==previousNote))
            {
                CancelDrag();
            }
        }
    }

    public void CancelDrag()
    {
        isReadyToShareEventData = true;
        isDragCancelled = true;
        isBeingDragged = false;
        UIManager.SelectionGroup.eventSenderNote = null;
        OnCancelDrag();
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

    public void OnPointerUp(PointerEventData eventData)
    {
        pointerEventData = eventData;
        switch (eventData.button)
        {
            case PointerEventData.InputButton.Left:
                OnLeftClick();
                // if we dragged without moving the mouse (i.e. during playback)
                // we need to manually trigger the OnEndDrag function
                if (isBeingDragged)
                {
                    OnEndDrag(eventData);
                }
                break;
            case PointerEventData.InputButton.Right:
                OnRightClick();
                break;
        }
    }

    protected virtual void OnLeftClick() {}
    protected virtual void OnRightClick() {}

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerEventData = eventData;
        switch (eventData.button)
        {
            case PointerEventData.InputButton.Left:
                // for modifying multiselection, check keys (shift/ctrl/alt) when we click down
                modifyExistingSelection = TrackMarqueeSelection.ModifyExistingSelection;
                addToSelection = TrackMarqueeSelection.AddToSelection;
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



    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        // the first note to be dragged should pass event data to all others
        if (UIManager.SelectionGroup.eventSenderNote==null)
        {
            UIManager.SelectionGroup.eventSenderNote = Note;
        }
        // if this note is dragged while in multiselection, drag all other notes
        if (isInMarqueeSelection && UIManager.SelectionGroup.eventSenderNote==Note && isReadyToShareEventData)
        {
            Debug.Log("passing begin drag to all selected notes!");
            isReadyToShareEventData = false;
            UIManager.SelectionGroup.PassEventDataToAllSelectedNotes(eventData, ExecuteEvents.beginDragHandler);
        }
        // note is either not selected or received event data from another selected note
        else
        {
            isReadyToShareEventData = true;
            isBeingDragged = true;
            isDragCancelled = false;
            if (eventData.button != PointerEventData.InputButton.Left) return;
            pointerEventData = eventData;
            if (Track.PointerToTrackPosition(eventData.pressPosition, clamp: false) is not var (pos, time)) return;
            dragStartPointerPos = pos;
            dragStartPointerTime = time;
            lastDragPos = pos;
            lastDragTime = time;
            OnDragStart();
        }
    }

    public virtual void OnDrag(PointerEventData eventData)
    {
        if (!isDragCancelled)
        {
            // if this note is dragged while in multiselection, drag all other notes
            if (isInMarqueeSelection && UIManager.SelectionGroup.eventSenderNote==Note && isReadyToShareEventData)
            {
                Debug.Log("passing drag to all selected notes!");
                // sender marks flag as false so that they can receive the event data at the correct time
                isReadyToShareEventData = false;
                UIManager.SelectionGroup.PassEventDataToAllSelectedNotes(eventData, ExecuteEvents.dragHandler);
            }
            else
            {
                isReadyToShareEventData = true;
                if (eventData.button != PointerEventData.InputButton.Left) return;
                pointerEventData = eventData;
                if (Track.PointerToTrackPosition(eventData.position, clamp: false) is not var (dragPos, dragTime)) return;
                this.dragPos = dragPos;
                this.dragTime = dragTime;
                dragPosDiff = dragPos - dragStartPointerPos;
                dragTimeDiff = dragTime - dragStartPointerTime;
                dragPosDelta = dragPos - lastDragPos;
                dragTimeDelta = dragTime - lastDragTime;
                lastDragPos = dragPos;
                lastDragTime = dragTime;
                OnDrag();
            }
        }
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragCancelled && isBeingDragged)
        {
            // if this note is dropped while in multiselection, drop all other notes
            if (isInMarqueeSelection && UIManager.SelectionGroup.eventSenderNote==Note && isReadyToShareEventData)
            {
                Debug.Log("passing end drag to all selected notes!");
                isReadyToShareEventData = false;
                UIManager.SelectionGroup.CommitMultiselectMoveAction(eventData);
            }
            else
            {
                isReadyToShareEventData = true;
                OnEndDrag();
            }
        }
        isBeingDragged = false;
        UIManager.SelectionGroup.eventSenderNote = null;
    }

    protected virtual void OnDragStart() {}
    protected virtual void OnDrag() {}
    protected virtual void OnEndDrag() {}
    protected virtual void OnCancelDrag() {}
}
