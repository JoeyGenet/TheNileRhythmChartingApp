using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;

[RequireComponent(typeof(RectTransform))]
public class TrackMarqueeSelection : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] RectTransform scrollUpArea;
    [SerializeField] RectTransform scrollDownArea;
    [SerializeField] float scrollSpeed = 0.1f;
    [SerializeField] RectTransform marquee;
    [SerializeField] Image marqueeImage;
    [SerializeField] Track Track;

    private bool modifyExistingSelection;
    public static bool ModifyExistingSelection
    {
        get 
        { 
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ||
                   Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
                   Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        }
    }

    private bool addToSelection;
    public static bool AddToSelection
    {
        get 
        { 
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ||
                   Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
                   (!Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt));
        }
    }

    protected bool isBeingDragged = false;

    protected RectTransform rect;

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
        // scroll the track if you mouse over the scroll areas
        int scrollAmount = 0;
        if (PlaybackManager.Instance.IsPaused && isBeingDragged)
        {
            scrollAmount = UIManager.Track.DragScrollTrack();
        }
        // drag note if scrolling or in playback
        if (scrollAmount != 0 || (!PlaybackManager.Instance.IsPaused && isBeingDragged))
        {
            OnDrag(pointerEventData);
        }
    }

    void OnNoteSelected(object previousNote, object newNote)
    {
        
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerEventData = eventData;
        switch (eventData.button)
        {
            case PointerEventData.InputButton.Left:
                OnLeftDown();
                break;
            case PointerEventData.InputButton.Right:
                OnRightDown();
                break;
        }
    }

    protected void OnLeftDown()
    {
        // update selection modifiers based on keypresses the moment you click
        modifyExistingSelection = ModifyExistingSelection;
        addToSelection = AddToSelection;
    }

    protected void OnRightDown() {}

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

    protected void OnLeftClick() 
    {
        // clear selection if we completed click on the track without dragging
        // don't clear if we started click while holding a key to modify the current selection
        // and only clear if we don't have a note cursor selected
        if (!modifyExistingSelection && !isBeingDragged && UIManager.CursorMode==UICursorMode.None)
        {
            UIManager.SelectElement(null, updateEditHistory: true);
        }
    }
    
    protected void OnRightClick() {}


    // get start timestamp and horizontal position when starting drag
    public void OnBeginDrag(PointerEventData eventData)
    {
        // do not start selection if you right click or have note type selected
        if (eventData.button != PointerEventData.InputButton.Left || UIManager.CursorMode != UICursorMode.None) return;
        
        // set start drag data
        pointerEventData = eventData;
        if (Track.PointerToTrackPosition(eventData.pressPosition, clamp: false) is not var (pos, time)) return;
        dragStartPointerPos = pos;
        dragStartPointerTime = time;

        Debug.Log("Start drag pos: "+Track.TrackToUIPosition(dragStartPointerPos, dragStartPointerTime));
        marqueeImage.enabled = true;
        isBeingDragged = true;
    }
    
    // get current timestamp and horizontal pos when dragging
    public void OnDrag(PointerEventData eventData)
    {
        // do not continue drag if you right click or have note type selected
        if (eventData.button != PointerEventData.InputButton.Left || UIManager.CursorMode != UICursorMode.None) return;

        // update drag info
        if (eventData.button != PointerEventData.InputButton.Left) return;
        pointerEventData = eventData;
        if (Track.PointerToTrackPosition(eventData.position, clamp: false) is not var (dragPos, dragTime)) return;
        this.dragPos = dragPos;
        this.dragTime = dragTime;
        dragPosDiff = dragPos - dragStartPointerPos;
        dragTimeDiff = dragTime - dragStartPointerTime;

        DrawSelectionRectangle();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // do not end drag if you right click or have note type selected
        if (eventData.button != PointerEventData.InputButton.Left || UIManager.CursorMode != UICursorMode.None) return;

        Debug.Log("End drag pos: "+Track.TrackToUIPosition(dragPos, dragTime));
        // this class only detects selected notes. The selected notes are controlled by UIManager.SelectionGroup.
        SortedSet<Note> selectedNotes = CheckForSelectedNotes(dragStartPointerTime, dragTime, dragStartPointerPos, dragPos); // OFFENDER!!!
        UIManager.MultiselectElements(addToSelection, selectedNotes, modifyExistingSelection);
        marqueeImage.enabled = false;
        isBeingDragged = false;
    }

    protected void DrawSelectionRectangle() 
    {
        // draw the selection rectangle
        Vector2 startUiPos = Track.TrackToUIPosition(dragStartPointerPos, dragStartPointerTime, clamp:false);
        Vector2 currentUiPos = Track.TrackToUIPosition(dragPos, dragTime);
        marquee.pivot = new Vector2(currentUiPos.x > startUiPos.x ? 0 : 1, currentUiPos.y > startUiPos.y ? 0 : 1);
        marquee.localPosition = startUiPos;
        marquee.sizeDelta = new Vector2(Math.Abs(currentUiPos.x-startUiPos.x), Math.Abs(currentUiPos.y-startUiPos.y));
    }

    // would it be better to search for the note renderers directly?
    private SortedSet<Note> CheckForSelectedNotes(int startTime, int endTime, int startPos, int endPos)
    {
        int minTime = Math.Min(startTime, endTime);
        int maxTime = Math.Max(startTime, endTime);
        int minPos = Math.Min(startPos, endPos);
        int maxPos = Math.Max(startPos, endPos);
        // get the min and max bounds of marquee and lever track
        // so we can determine if selection includes them
        Vector3[] marqueeCorners = new Vector3[4];
        Vector3[] leverTrackAreaCorners = new Vector3[4];
        Vector3[] mainTrackAreaCorners = new Vector3[4];
        marquee.GetWorldCorners(marqueeCorners);
        Track.LeverTrackArea.GetWorldCorners(leverTrackAreaCorners);
        Track.MainTrackArea.GetWorldCorners(mainTrackAreaCorners);
        // [0] is bottom left (min) corner, [2] is top right (max) corner
        bool leftTrackSelected = marqueeCorners[2].x >= leverTrackAreaCorners[0].x &&
                                 marqueeCorners[0].x <= leverTrackAreaCorners[0].x;
        bool rightTrackSelected = marqueeCorners[2].x >= leverTrackAreaCorners[2].x &&
                                  marqueeCorners[0].x <= leverTrackAreaCorners[2].x;
        bool mainTrackSelected = marqueeCorners[2].x >= mainTrackAreaCorners[0].x &&
                                 marqueeCorners[0].x <= mainTrackAreaCorners[2].x;

        // highlight all notes that fall under current selection range
        IReadOnlyCollection<Note> notesInRange = ChartManager.SelectedChart.GetNotesBetween(minTime, maxTime);
        SortedSet<Note> selectedNotes = new SortedSet<Note>();
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
                    EditableLeverSequence leverSequence = note as EditableLeverSequence;
                    // if lever sequence is in range and if marquee covers the correct side of the track...
                    if ((leverSequence.Dir == LeftRight.Left && leftTrackSelected) ||
                        (leverSequence.Dir == LeftRight.Right && rightTrackSelected))
                    {
                        // get all sequence notes that fall within selection range
                        IReadOnlyCollection<LeverNote> leverNotesInRange = leverSequence.GetNotesBetween(minTime, maxTime);
                        bool noteAdded = false;
                        foreach (EditableLeverNote leverNote in leverNotesInRange)
                        {
                            selectedNotes.Add(leverNote);
                            noteAdded = true;
                        }
                        if (noteAdded)
                        {
                            selectedNotes.Add(leverSequence);
                        }
                    }
                    break;
                case EditableWheelFlick:
                    EditableWheelFlick wheelFlick = note as EditableWheelFlick;
                    // wheel flicks take up whole track, so just check if marquee intersects with track
                    if (mainTrackSelected)
                    {
                        selectedNotes.Add(wheelFlick);
                    }
                    break;
                case EditableWheelSequence:
                    EditableWheelSequence wheelSequence = note as EditableWheelSequence;
                    // the sequence start is not included in list of turns, so consider it separately
                    if (wheelSequence.Pos >= minPos && wheelSequence.Pos <= maxPos)
                    {
                        // add the placeholder starting note as a proxy for the start of the sequence
                        // this note is mapped to the first vertex renderer in the wheel sequence
                        // and allows us to multiselect start of sequence separately from the first turn
                        // and we can use it to find the renderer of the first vertex
                        selectedNotes.Add(wheelSequence.startingNote);
                    }
                    // get all sequence notes that fall within selection range
                    IReadOnlyCollection<WheelTurn> wheelTurnsInRange = wheelSequence.GetNotesBetween(minTime, maxTime);
                    bool turnAdded = false;
                    foreach (EditableWheelTurn wheelTurn in wheelTurnsInRange)
                    {
                        if (wheelTurn.Pos >= minPos && wheelTurn.Pos <= maxPos)
                        {
                            selectedNotes.Add(wheelTurn);
                            turnAdded = true;
                        }
                    }
                    if (turnAdded)
                    {
                        selectedNotes.Add(wheelSequence);
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
                bool noteAdded = false;
                foreach (EditableLeverNote leverNote in leverNotesInRange)
                {
                    selectedNotes.Add(leverNote);
                    noteAdded = true;
                }
                if (noteAdded)
                {
                    selectedNotes.Add(lastLeftLeverSequence);
                }
            }
        }
        if (rightTrackSelected)
        {
            EditableLeverSequence lastRightLeverSequence = ChartManager.SelectedChart.GetNotesBefore(minTime).OfType<EditableLeverSequence>().LastOrDefault(seq => seq.Dir == LeftRight.Right);
            if (lastRightLeverSequence != null)
            {
                IReadOnlyCollection<LeverNote> leverNotesInRange = lastRightLeverSequence.GetNotesBetween(minTime, maxTime);
                bool noteAdded = false;
                foreach (EditableLeverNote leverNote in leverNotesInRange)
                {
                    selectedNotes.Add(leverNote);
                    noteAdded = true;
                }
                if (noteAdded)
                {
                    selectedNotes.Add(lastRightLeverSequence);
                }
            }
        }
        // check last wheel sequence before start of marquee to see if any wheel turn nodes are captured by selection
        EditableWheelSequence lastWheelSequence = ChartManager.SelectedChart.GetNotesBefore(minTime).OfType<EditableWheelSequence>().LastOrDefault();
        if (lastWheelSequence != null)
        {
            // get all sequence notes that fall within selection range
            IReadOnlyCollection<WheelTurn> wheelTurnsInRange = lastWheelSequence.GetNotesBetween(minTime, maxTime);
            bool turnAdded = false;
            foreach (EditableWheelTurn wheelTurn in wheelTurnsInRange)
            {
                if (wheelTurn.Pos >= minPos && wheelTurn.Pos <= maxPos)
                {
                    selectedNotes.Add(wheelTurn);
                    turnAdded = true;
                }
            }
            if (turnAdded)
            {
                selectedNotes.Add(lastWheelSequence);
            }
        }

        // log selected notes
        string selectedNoteLog = "Selected Notes:";
        foreach(Note note in selectedNotes)
        {
            selectedNoteLog += "\nID: " + note.Id + " | start time: " + note.StartTime+ " | " + note.ToString();
            if (Track.noteGameObjectMapping.TryGetValue(note, out GameObject gameObject) && gameObject!=null)
            {
                selectedNoteLog += " | " + gameObject.name + " | " + gameObject.GetInstanceID();
            }
        }
        Debug.Log(selectedNoteLog);
        
        return selectedNotes;
    }
}
