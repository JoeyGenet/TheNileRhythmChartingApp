using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;

// TODO: Should this class be a child of NoteRenderer? i.e. make this a type of "note" that is composed of other notes in selection?
[RequireComponent(typeof(RectTransform))]
public class TrackMarqueeSelection : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] RectTransform scrollUpArea;
    [SerializeField] RectTransform scrollDownArea;
    [SerializeField] float scrollSpeed = 0.1f;
    [SerializeField] RectTransform marquee;
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

        // scroll the track if you mouse over the scroll areas
        if (marqueeImage.enabled)
        {
            DragScrollTrack();
        }
    }

    void OnNoteSelected(object previousNote, object newNote)
    {
        
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
        // clear all selected notes if we click on the track without dragging
        Track.SelectionGroup.SetSelectedNoteRenderersFlag(false);
    }

    protected void OnLeftClick() {}
    protected void OnRightClick() {}


    // get start timestamp and horizontal position when starting drag
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || UIManager.CursorMode != UICursorMode.None) return;
        pointerEventData = eventData;
        if (Track.PointerToTrackPosition(eventData.pressPosition, clamp: false) is not var (pos, time)) return;
        dragStartPointerPos = pos;
        dragStartPointerTime = time;
        // clear all selected notes if we click and dragging outside of current selection
        Track.SelectionGroup.SetSelectedNoteRenderersFlag(false);
        OnDragStart();
    }
    
    // get current timestamp and horizontal pos when dragging
    public void OnDrag(PointerEventData eventData)
    {
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
        OnEndDrag();
    }

    protected void OnDragStart() 
    {
        Debug.Log("Start drag pos: "+Track.TrackToUIPosition(dragStartPointerPos, dragStartPointerTime));
        marqueeImage.enabled = true;
    }

    protected void OnDrag() 
    {
        // draw the selection rectangle
        Vector2 startUiPos = Track.TrackToUIPosition(dragStartPointerPos, dragStartPointerTime);
        Vector2 currentUiPos = Track.TrackToUIPosition(dragPos, dragTime);
        marquee.pivot = new Vector2(currentUiPos.x > startUiPos.x ? 0 : 1, currentUiPos.y > startUiPos.y ? 0 : 1);
        marquee.localPosition = startUiPos;
        marquee.sizeDelta = new Vector2(Math.Abs(currentUiPos.x-startUiPos.x), Math.Abs(currentUiPos.y-startUiPos.y));
    }

    protected void OnEndDrag() 
    {   
        Debug.Log("End drag pos: "+Track.TrackToUIPosition(dragPos, dragTime));
        // this class only detects selected notes. The selected notes are controlled
        // by a TrackMarqueeSelectionGroup instance.
        List<Note> selectedNotes = CheckForSelectedNotes(dragStartPointerTime, dragTime, dragStartPointerPos, dragPos);
        if (selectedNotes.Count > 0)
        {
            Track.SelectionGroup.SetSelectedNoteRenderersFlag(true, selectedNotes);
        }
        marqueeImage.enabled = false;
    }

    public void DragScrollTrack()
    {
        // check if cursor is above bottom edge of the "scroll up" bounds
        RectTransformUtility.ScreenPointToLocalPointInRectangle(scrollUpArea, Input.mousePosition, null, out Vector2 localScrollUpPos);
        bool isScrollingUp = localScrollUpPos.y >= scrollUpArea.rect.yMin;
        // check if cursor is below top edge of the "scroll down" bounds
        RectTransformUtility.ScreenPointToLocalPointInRectangle(scrollDownArea, Input.mousePosition, null, out Vector2 localScrollDownPos);
        bool isScrollingDown = localScrollDownPos.y <= scrollDownArea.rect.yMax;

        int scrollAmount = isScrollingUp ? (int) (PlaybackManager.Instance.VerticalViewSize * scrollSpeed) :
                           isScrollingDown ? (int) (PlaybackManager.Instance.VerticalViewSize * -scrollSpeed) : 0;
        if (scrollAmount != 0)
        {
            PlaybackManager.Instance.SetTimestamp(PlaybackManager.Instance.MillisecondsInChart + scrollAmount);
            Track.IsScrolling = true;
            // update marquee bounds while scrolling
            if (Track.PointerToTrackPosition(Input.mousePosition, clamp: false) is not var (dragPos, dragTime)) return;
            this.dragPos = dragPos;
            this.dragTime = dragTime;
            dragPosDiff = dragPos - dragStartPointerPos;
            dragTimeDiff = dragTime - dragStartPointerTime;
            OnDrag();
        }
        else
        {
            Track.IsScrolling = false;
        }
    }

    // would it be better to search for the note renderers directly?
    private List<Note> CheckForSelectedNotes(int startTime, int endTime, int startPos, int endPos)
    {
        int minTime = Math.Min(startTime, endTime);
        int maxTime = Math.Max(startTime, endTime);
        int minPos = Math.Min(startPos, endPos);
        int maxPos = Math.Max(startPos, endPos);
        // get the min and max bounds of marquee and lever track
        // so we can determine if selection includes them
        // TODO: find cleaner way to do this
        Vector3[] marqueeCorners = new Vector3[4];
        marquee.GetWorldCorners(marqueeCorners);
        Vector3 marqueeGlobalMin = marqueeCorners[0];
        Vector3 marqueeGlobalMax = marqueeCorners[2];
        Vector3[] leverTrackAreaCorners = new Vector3[4];
        Track.LeverTrackArea.GetWorldCorners(leverTrackAreaCorners);
        Vector3 leverTrackAreaGlobalMin = leverTrackAreaCorners[0];
        Vector3 leverTrackAreaGlobalMax = leverTrackAreaCorners[2];
        Vector3[] mainTrackAreaCorners = new Vector3[4];
        Track.MainTrackArea.GetWorldCorners(mainTrackAreaCorners);
        Vector3 mainTrackAreaGlobalMin = mainTrackAreaCorners[0];
        Vector3 mainTrackAreaGlobalMax = mainTrackAreaCorners[2];
        bool leftTrackSelected = marqueeGlobalMax.x >= leverTrackAreaGlobalMin.x &&
                                 marqueeGlobalMin.x <= leverTrackAreaGlobalMin.x;
        bool rightTrackSelected = marqueeGlobalMax.x >= leverTrackAreaGlobalMax.x &&
                                  marqueeGlobalMin.x <= leverTrackAreaGlobalMax.x;
        bool mainTrackSelected = marqueeGlobalMax.x >= mainTrackAreaGlobalMin.x &&
                                 marqueeGlobalMin.x <= mainTrackAreaGlobalMax.x;

        // highlight all notes that fall under current selection range
        IReadOnlyCollection<Note> notesInRange = ChartManager.SelectedChart.GetNotesBetween(minTime, maxTime);
        List<Note> selectedNotes = new List<Note>();
        foreach(Note note in notesInRange)
        {
            switch (note)
            {
                case EditableButtonNote:
                    EditableButtonNote buttonNote = note as EditableButtonNote;
                    // select both button notes in a pair if either is in marquee
                    if ((buttonNote.LeftPos >= minPos && buttonNote.LeftPos <= maxPos) ||
                        (buttonNote.RightPos != null && buttonNote.RightPos >= minPos && buttonNote.RightPos <= maxPos))
                    {
                        selectedNotes.Add(note);
                    }
                    break;
                case EditableLeverSequence:
                    Debug.Log("LeverSequence detected");
                    EditableLeverSequence leverSequence = note as EditableLeverSequence;
                    // if lever sequence is in range and if marquee covers the correct side of the track...
                    if ((leverSequence.Dir == LeftRight.Left && leftTrackSelected) ||
                        (leverSequence.Dir == LeftRight.Right && rightTrackSelected))
                    {
                        // get all sequence notes that fall within selection range
                        IReadOnlyCollection<LeverNote> leverNotesInRange = leverSequence.GetNotesBetween(minTime, maxTime);
                        foreach (EditableLeverNote leverNote in leverNotesInRange)
                        {
                            selectedNotes.Add(leverNote);
                        }
                    }
                    break;
                case EditableWheelFlick:
                    Debug.Log("wheelFlick detected");
                    EditableWheelFlick wheelFlick = note as EditableWheelFlick;
                    // wheel flicks take up whole track, so just check if marquee intersects with track
                    if (mainTrackSelected)
                    {
                        selectedNotes.Add(wheelFlick);
                    }
                    break;
                case EditableWheelSequence:
                    Debug.Log("WheelSequence detected");
                    EditableWheelSequence wheelSequence = note as EditableWheelSequence;
                    // the sequence start is not included in list of turns, so consider it separately
                    if (wheelSequence.Pos >= minPos && wheelSequence.Pos <= maxPos)
                    {
                        selectedNotes.Add(wheelSequence);
                    }
                    // get all sequence notes that fall within selection range
                    IReadOnlyCollection<WheelTurn> wheelTurnsInRange = wheelSequence.GetNotesBetween(minTime, maxTime);
                    foreach (EditableWheelTurn wheelTurn in wheelTurnsInRange)
                    {
                        if (wheelTurn.Pos >= minPos && wheelTurn.Pos <= maxPos)
                        {
                            selectedNotes.Add(wheelTurn);
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        // check last lever sequence before start of marquee for any individual lever notes are captured by selection
        if (leftTrackSelected)
        {
            EditableLeverSequence lastLeftLeverSequence = ChartManager.SelectedChart.GetNotesBefore(minTime).OfType<EditableLeverSequence>().LastOrDefault(seq => seq.Dir == LeftRight.Left);
            if (lastLeftLeverSequence != null)
            {
                IReadOnlyCollection<LeverNote> leverNotesInRange = lastLeftLeverSequence.GetNotesBetween(minTime, maxTime);
                foreach (EditableLeverNote leverNote in leverNotesInRange)
                {
                    selectedNotes.Add(leverNote);
                }
            }
        }
        if (rightTrackSelected)
        {
            EditableLeverSequence lastRightLeverSequence = ChartManager.SelectedChart.GetNotesBefore(minTime).OfType<EditableLeverSequence>().LastOrDefault(seq => seq.Dir == LeftRight.Right);
            if (lastRightLeverSequence != null)
            {
                IReadOnlyCollection<LeverNote> leverNotesInRange = lastRightLeverSequence.GetNotesBetween(minTime, maxTime);
                foreach (EditableLeverNote leverNote in leverNotesInRange)
                {
                    selectedNotes.Add(leverNote);
                }
            }
        }
        // check last wheel sequence before start of marquee to see if any wheel turn nodes are captured by selection
        EditableWheelSequence lastWheelSequence = ChartManager.SelectedChart.GetNotesBefore(minTime).OfType<EditableWheelSequence>().LastOrDefault();
        if (lastWheelSequence != null)
        {
            // the sequence start is not included in list of turns, so consider it separately
            if (lastWheelSequence.Pos >= minPos && lastWheelSequence.Pos <= maxPos &&
                lastWheelSequence.StartTime >= minTime && lastWheelSequence.StartTime <= maxTime)
            {
                selectedNotes.Add(lastWheelSequence);
            }
            // get all sequence notes that fall within selection range
            IReadOnlyCollection<WheelTurn> wheelTurnsInRange = lastWheelSequence.GetNotesBetween(minTime, maxTime);
            foreach (EditableWheelTurn wheelTurn in wheelTurnsInRange)
            {
                if (wheelTurn.Pos >= minPos && wheelTurn.Pos <= maxPos)
                {
                    selectedNotes.Add(wheelTurn);
                }
            }
        }

        // log selected notes
        string selectedNoteLog = "";
        foreach(Note note in selectedNotes)
        {
            selectedNoteLog += "\nID: " + note.Id + " | note type: " + note.GetType() + " | " + note.ToString();
            if (Track.noteGameObjectMapping.TryGetValue(note, out GameObject gameObject))
            {
                selectedNoteLog += " | " + gameObject.name;
            }
        }
        Debug.Log(selectedNoteLog);
        
        return selectedNotes;
    }
}
