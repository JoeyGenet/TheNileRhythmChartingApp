using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.EventSystems.ExecuteEvents;

[RequireComponent(typeof(RectTransform))]
public class TrackMarqueeSelectionGroup : MonoBehaviour
{
    [SerializeField] RectTransform scrollUpArea;
    [SerializeField] RectTransform scrollDownArea;
    [SerializeField] float scrollSpeed = 0.1f;
    [SerializeField] Track Track;
    public SortedSet<Note> notes { get; private set; }
    private SortedSet<Note> noteClipboard;
    public Dictionary<GameObject, Transform> rendererTransformMapping { get; private set; }
    public MultiselectMoveAction multiselectMoveAction;
    public MultiselectPasteAction multiselectPasteAction;

    protected RectTransform rect;

    int prevTime = int.MaxValue;

    public void Init(Track track)
    {
        Track = track;
    }

    protected void Awake()
    {
        rect = GetComponent<RectTransform>();
        notes = new SortedSet<Note>();
        rendererTransformMapping = new Dictionary<GameObject, Transform>();
    }

    protected void OnDestroy() {}

    // TODO: create UpdateOnPlayback that is called when the user drags the playback/zoom or during playback
    // and note is visible or startTime and endTime go in/out of view as a result

    protected void Update()
    {
        int currTime = PlaybackManager.Instance.MillisecondsInChart;
        prevTime = currTime;

        // scroll the track if you mouse over the scroll areas
        // DragScrollTrack();

        // TODO: copy and paste buttons/bindings should be implemented in multiselectiongroup inspector
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (notes.Count > 0)
            {
                noteClipboard = new SortedSet<Note>(notes);
                // TODO: have unified
                SetSelectedNoteRenderersFlag(false);
                notes.Clear();
            }
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            if (noteClipboard.Count > 0)
            {
                PasteNotesFromClipboard();
            }
        }
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
        }
        else
        {
            Track.IsScrolling = false;
        }
    }

    public void SetSelectedNoteRenderersFlag(bool isInMarqueeSelection, SortedSet<Note> selectedNotes=null)
    {
        if (selectedNotes != null)
        {
            notes = selectedNotes;
        }
        foreach(Note note in notes)
        {
            // get the object associated with each note
            if (Track.noteGameObjectMapping.TryGetValue(note, out GameObject gameObject) && gameObject!=null)
            {
                // flag all selected notes to change their click/drag/drop behavior
                switch (note)
                {
                    case EditableButtonNote:
                        ButtonNoteRenderer buttonNoteRenderer = gameObject.GetComponent<ButtonNoteRenderer>();
                        buttonNoteRenderer.isInMarqueeSelection = isInMarqueeSelection;
                        break;
                    case EditableLeverNote:
                        LeverNoteRenderer leverNoteRenderer = gameObject.GetComponent<LeverNoteRenderer>();
                        leverNoteRenderer.isInMarqueeSelection = isInMarqueeSelection;
                        break;
                    case EditableWheelFlick:
                        WheelFlickRenderer wheelFlickRenderer = gameObject.GetComponent<WheelFlickRenderer>();
                        wheelFlickRenderer.isInMarqueeSelection = isInMarqueeSelection;
                        break;
                    case EditableWheelTurn:
                        WheelSequenceVertexRenderer wheelSequenceVertexRenderer = gameObject.GetComponent<WheelSequenceVertexRenderer>();
                        wheelSequenceVertexRenderer.isInMarqueeSelection = isInMarqueeSelection;
                        break; 
                    default:
                        break;
                }
            }
        }
    }

    public void PassEventDataToAllSelectedNotes<T>(BaseEventData eventData, EventFunction<T> functor)
    where T : IEventSystemHandler
    {
        foreach(Note note in notes)
        {
            // pass the event data to all notes, including the selected one
            // the selected note does not execute the event initially, instead waiting until now
            // so that it can execute sequentially with related notes (i.e. wheel sequences)
            if (Track.noteGameObjectMapping.TryGetValue(note, out GameObject gameObject) && gameObject!=null)
            {
                Execute(gameObject, eventData, functor);
            }
        }
    }

    // cache all selected notes in some sort of clipboard
    // TODO: add Paste function to Track.cs so you can paste without a present selection
    // TODO: add a note paste UndoRedoAction that is basically a group event for adding multiple notes
    // ALSO TODO: group deletion
    public void CopySelectedNotes()
    {

    }

    public void PasteNotesFromClipboard()
    {
        if (Track.PointerToTrackPosition(Input.mousePosition, clamp: true) is not var (pos, time)) return;
        multiselectPasteAction = new MultiselectPasteAction(new List<UndoRedoAction>());
        EditableChart chart = ChartManager.SelectedChart;
        int selectionStartTime = noteClipboard.First().StartTime;
        int pasteStartTime = time; //PlaybackManager.Instance.MillisecondsInChart;
        // TODO: use clipboard instead of selection
        // TODO: use local chart in Track.cs instead of ChartManager.SelectedChart
        foreach (Note note in noteClipboard)
        {
            switch (note)
            {
                case EditableButtonNote:
                    EditableButtonNote buttonNote = note as EditableButtonNote;
                    int newButtonNoteStartTime = buttonNote.StartTime - selectionStartTime + pasteStartTime;
                    EditableButtonNote pastedButtonNote = new EditableButtonNote(chart, newButtonNoteStartTime, buttonNote.LeftPos, buttonNote.RightPos);
                    chart.AddNote(pastedButtonNote, updateEditHistory:false);
                    AddNoteAction addButtonNoteAction = new AddNoteAction(pastedButtonNote, chart);
                    multiselectPasteAction.AddUndoRedoAction(addButtonNoteAction);
                    break;
                case EditableLeverNote:
                    EditableLeverNote leverNote = note as EditableLeverNote;
                    int newLeverNoteStartTime = leverNote.StartTime - selectionStartTime + pasteStartTime;
                    multiselectPasteAction.AddUndoRedoAction(AddLeverNote(leverNote, newLeverNoteStartTime, chart));
                    break;
                case EditableWheelFlick:
                    EditableWheelFlick wheelFlick = note as EditableWheelFlick;
                    int newWheelFlickStartTime = wheelFlick.StartTime - selectionStartTime + pasteStartTime;
                    EditableWheelFlick pastedWheelFlick = new EditableWheelFlick(chart, newWheelFlickStartTime, wheelFlick.Dir);
                    chart.AddNote(pastedWheelFlick, updateEditHistory:false);
                    AddNoteAction addWheelFlickAction = new AddNoteAction(pastedWheelFlick, chart);
                    multiselectPasteAction.AddUndoRedoAction(addWheelFlickAction);
                    break;
                case EditableWheelTurn:
                    EditableWheelTurn wheelTurn = note as EditableWheelTurn;
                    int newWheelTurnStartTime = wheelTurn.StartTime - selectionStartTime + pasteStartTime;
                    multiselectPasteAction.AddUndoRedoAction(AddWheelTurn(wheelTurn.Pos, newWheelTurnStartTime, wheelTurn.EndTime, wheelTurn.StartAngle+wheelTurn.DeltaAngle, chart));
                    break;
                default:
                    break;
            }
        }

        multiselectPasteAction.CommitAction();
    }

    UndoRedoAction AddWheelTurn(int pos, int startTime, int endTime, int endAngle, EditableChart chart)
    {
        // If placing inside an existing sequence or holding shift, add to the previous sequence. Otherwise create a new sequence
        var lastWheelSequence = chart.GetNotesBefore(endTime).OfType<EditableWheelSequence>().LastOrDefault();
        bool addToWheelSequence = lastWheelSequence != null && (lastWheelSequence.EndTime > endTime 
            || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        if (addToWheelSequence)
        {
            var prevNote = lastWheelSequence.GetNotesBefore(endTime).LastOrDefault() as EditableWheelTurn;
            int prevEndAngle = prevNote == null ? WheelTurn.PosToAngle(lastWheelSequence.Pos)
                : WheelTurn.WrapAngle(prevNote.StartAngle + prevNote.DeltaAngle);

            EditableWheelTurn editableWheelTurn = new EditableWheelTurn(lastWheelSequence, endTime, endAngle - prevEndAngle);
            lastWheelSequence.AddNote(editableWheelTurn, updateEditHistory:false);
            AddNoteToWheelSequenceAction addNoteToWheelSequenceAction = new AddNoteToWheelSequenceAction(editableWheelTurn, lastWheelSequence);
            return addNoteToWheelSequenceAction;
        }
        else
        {
            EditableWheelSequence editableWheelSequence = new EditableWheelSequence(chart, startTime, pos, new() { (0, endTime) });
            chart.AddNote(editableWheelSequence, updateEditHistory:false);
            AddNoteAction addNoteAction = new AddNoteAction(editableWheelSequence, chart);
            return addNoteAction;
        }
    }

    UndoRedoAction AddLeverNote(EditableLeverNote originalNote, int newTime, EditableChart chart)
    {
        LeftRight side = originalNote.Container.Dir;
        LeverPos leverPos = originalNote.Pos;

        // If placing inside an existing sequence or holding shift, add to the previous sequence. Otherwise create a new sequence
        var lastOldLeverSequence = chart.GetNotesBefore(originalNote.StartTime, inclusive:false).OfType<EditableLeverSequence>().LastOrDefault(seq => seq.Dir == side);
        var lastOldLeverNote = lastOldLeverSequence?.GetNotesBefore(originalNote.StartTime, inclusive:false).OfType<EditableLeverNote>().LastOrDefault(note => note.Container.Dir == side);
        var lastLeverSequence = chart.GetNotesBefore(newTime, inclusive:false).OfType<EditableLeverSequence>().LastOrDefault(seq => seq.Dir == side);
        bool addToLeverSequence = lastLeverSequence != null && (lastLeverSequence.EndTime > newTime 
            || (originalNote.PreviousNote!=null && noteClipboard.Contains(originalNote.PreviousNote))// && originalNote.PreviousNote == lastOldLeverNote)
            || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        
        if (addToLeverSequence)
        {
            EditableLeverNote editableLeverNote = new EditableLeverNote(lastLeverSequence, newTime, leverPos);
            lastLeverSequence.AddNote(editableLeverNote, updateEditHistory:false);
            AddNoteToLeverSequenceAction addNoteToLeverSequenceAction = new AddNoteToLeverSequenceAction(editableLeverNote, lastLeverSequence);
            return addNoteToLeverSequenceAction;
        }
        else
        {
            EditableLeverSequence editableLeverSequence = new EditableLeverSequence(chart, newTime, side, new() { (newTime, leverPos) });
            chart.AddNote(editableLeverSequence, updateEditHistory:false);
            AddNoteAction addNoteAction = new AddNoteAction(editableLeverSequence, chart);
            return addNoteAction;
        }
    }
    
}
