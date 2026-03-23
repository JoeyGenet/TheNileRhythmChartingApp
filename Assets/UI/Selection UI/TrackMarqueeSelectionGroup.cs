using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.EventSystems.ExecuteEvents;

public class TrackMarqueeSelectionGroup
{
    private Track track;
    public SortedSet<Note> notes { get; set; }
    public SortedSet<Note> lastDeselectedNotes { get; set; }
    public Note eventSenderNote;
    private MultiselectMoveAction multiselectMoveAction;

    public TrackMarqueeSelectionGroup(Track track)
    {
        notes = new SortedSet<Note>();
        lastDeselectedNotes = new SortedSet<Note>();
        this.track = track;
    }

    public void FlagNotes(bool addToSelection)
    {
        if (notes!=null)
        {
            foreach(Note note in notes)
            {
                // get the object associated with each note
                if (track.noteGameObjectMapping.TryGetValue(note, out GameObject gameObject) && gameObject!=null)
                {
                    // flag all selected notes to change their click/drag/drop behavior
                    // only the clickable renderers will be added--no WheelSequences or LeverSequences here
                    switch (note)
                    {
                        case EditableButtonNote:
                            ButtonNoteRenderer buttonNoteRenderer = gameObject.GetComponent<ButtonNoteRenderer>();
                            buttonNoteRenderer.isInMarqueeSelection = addToSelection;
                            break;
                        case EditableLeverNote:
                            LeverNoteRenderer leverNoteRenderer = gameObject.GetComponent<LeverNoteRenderer>();
                            leverNoteRenderer.isInMarqueeSelection = addToSelection;
                            break;
                        case EditableWheelFlick:
                            WheelFlickRenderer wheelFlickRenderer = gameObject.GetComponent<WheelFlickRenderer>();
                            wheelFlickRenderer.isInMarqueeSelection = addToSelection;
                            break;
                        case EditableWheelTurn:
                            WheelSequenceVertexRenderer wheelSequenceVertexRenderer = gameObject.GetComponent<WheelSequenceVertexRenderer>();
                            wheelSequenceVertexRenderer.isInMarqueeSelection = addToSelection;
                            break; 
                        default:
                            break;
                    }
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
            if (track.noteGameObjectMapping.TryGetValue(note, out GameObject gameObject) && gameObject!=null)
            {
                Execute(gameObject, eventData, functor);
            }
        }
    }

    public void MirrorSelectedNotes()
    {
        MultiselectMirrorAction multiselectMirrorAction = new MultiselectMirrorAction(new List<UndoRedoAction>());
        foreach(Note note in notes)
        {
            switch (note)
            {
                case EditableButtonNote buttonNote:
                    int oldLeftPos = buttonNote.LeftPos;
                    int? oldRightPos = buttonNote.RightPos;
                    buttonNote.RightPos = oldRightPos is int ? 100-oldLeftPos : null;
                    buttonNote.LeftPos = oldRightPos is int rightPos ? 100-rightPos : 100-oldLeftPos;
                    EditButtonNoteAction editButtonNoteAction = new EditButtonNoteAction(buttonNote, 0, oldLeftPos, oldRightPos, buttonNote.LeftPos, buttonNote.RightPos);
                    multiselectMirrorAction.AddUndoRedoAction(editButtonNoteAction);
                    break;
                case EditableLeverSequence leverSequence:
                    LeftRight oldLeverSequenceDir = leverSequence.Dir;
                    leverSequence.Dir = leverSequence.Dir == LeftRight.Left ? LeftRight.Right : LeftRight.Left;
                    int leverSequenceDirDelta = leverSequence.Dir-oldLeverSequenceDir;
                    EditLeverSequenceAction editLeverSequenceAction = new EditLeverSequenceAction(leverSequence, dirDelta:leverSequenceDirDelta); 
                    multiselectMirrorAction.AddUndoRedoAction(editLeverSequenceAction);
                    break;
                case EditableWheelFlick wheelFlick:
                    LeftRight oldFlickDir = wheelFlick.Dir;
                    wheelFlick.Dir = wheelFlick.Dir == LeftRight.Left ? LeftRight.Right : LeftRight.Left;
                    int wheelFlickDirDelta = wheelFlick.Dir - oldFlickDir;
                    EditWheelFlickAction editWheelFlickAction = new EditWheelFlickAction(wheelFlick, 0, dirDelta: wheelFlickDirDelta);
                    multiselectMirrorAction.AddUndoRedoAction(editWheelFlickAction);
                    break;
                case EditableWheelTurn wheelTurn:
                    Dictionary<EditableWheelTurn, int> oldDeltaAngles = wheelTurn.Container.Notes.ToDictionary(n => n, n => n.DeltaAngle);
                    int oldWheelSequencePos = wheelTurn.Container.Pos;
                    if (wheelTurn.Container.startingNote != wheelTurn)
                    {
                        int oldDeltaAngle = wheelTurn.DeltaAngle;
                        wheelTurn.DeltaAngle *= -1;
                        EditableWheelTurn nextTurn = wheelTurn.NextTurn;
                        while (nextTurn != null && UIManager.SelectionGroup.notes.Contains(nextTurn))
                        {
                            nextTurn = nextTurn.NextTurn;
                        }
                        if (nextTurn != null)
                        {
                            nextTurn.DeltaAngle -= wheelTurn.DeltaAngle - oldDeltaAngle;
                        }
                    }
                    else
                    {
                        wheelTurn.Container.Pos = 100-wheelTurn.Container.Pos;
                    }
                    int wheelSequencePosDelta = wheelTurn.Container.Pos - oldWheelSequencePos;
                    Dictionary<EditableWheelTurn, int> newDeltaAngles = wheelTurn.Container.Notes.ToDictionary(n => n, n => n.DeltaAngle);
                    EditWheelSequenceAction editWheelSequenceAction = new EditWheelSequenceAction(
                        wheelTurn.Container, startTimeDelta: 0, PosDelta: wheelSequencePosDelta, 
                        oldDeltaAngles: oldDeltaAngles, newDeltaAngles: newDeltaAngles);
                    multiselectMirrorAction.AddUndoRedoAction(editWheelSequenceAction);
                    break; 
                default:
                    break;
            }
        }
        multiselectMirrorAction.CommitAction();
    }

    public void AddToMultiselectMoveAction(UndoRedoAction noteEditAction)
    {
        multiselectMoveAction.AddUndoRedoAction(noteEditAction);
    }

    public void CommitMultiselectMoveAction(PointerEventData eventData)
    {
        multiselectMoveAction = new MultiselectMoveAction(new List<UndoRedoAction>());
        // this triggers all selected notes to execute end drag events, which then
        // pass in their individual UndoRedoActions
        PassEventDataToAllSelectedNotes(eventData, endDragHandler);
        multiselectMoveAction.CommitAction();
    }

    public void CancelMultiselectMoveAction()
    {
        foreach(Note note in notes)
        {
            // get the object associated with each note
            if (track.noteGameObjectMapping.TryGetValue(note, out GameObject gameObject) && gameObject!=null)
            {
                // call deselect callback for each
                // which then resets notes back to drag start positions/times
                switch (note)
                {
                    case EditableButtonNote:
                        ButtonNoteRenderer buttonNoteRenderer = gameObject.GetComponent<ButtonNoteRenderer>();
                        buttonNoteRenderer.CancelDrag();
                        break;
                    case EditableLeverNote:
                        LeverNoteRenderer leverNoteRenderer = gameObject.GetComponent<LeverNoteRenderer>();
                        leverNoteRenderer.CancelDrag();
                        break;
                    case EditableWheelFlick:
                        WheelFlickRenderer wheelFlickRenderer = gameObject.GetComponent<WheelFlickRenderer>();
                        wheelFlickRenderer.CancelDrag();
                        break;
                    case EditableWheelTurn:
                        WheelSequenceVertexRenderer wheelSequenceVertexRenderer = gameObject.GetComponent<WheelSequenceVertexRenderer>();
                        wheelSequenceVertexRenderer.CancelDrag();
                        break; 
                    default:
                        break;
                }
            }
        }
    }

    public bool CommitMultiselectDeleteAction()
    {
        UIManager.SelectElement(null, updateEditHistory:false);
        bool isRemoveSuccessful = true;

        MultiselectRemoveAction multiselectRemoveAction = new MultiselectRemoveAction(new List<UndoRedoAction>());
        // remove notes from latest to newest
        // this simplifies deleting notes from wheel sequences, as we are guaranteed to remove starting note last
        foreach(Note note in notes.Reverse())
        {
            switch (note)
            {
                case EditableButtonNote buttonNote:
                    isRemoveSuccessful = isRemoveSuccessful && buttonNote.Container.RemoveNote(buttonNote, updateEditHistory:false);
                    multiselectRemoveAction.AddUndoRedoAction(new RemoveNoteAction(note, buttonNote.Container));
                    break;
                case EditableLeverNote leverNote:
                    if (leverNote.Container.Notes.Count <= 1) 
                    {
                        isRemoveSuccessful = isRemoveSuccessful && leverNote.Container.Container.RemoveNote(leverNote.Container, updateEditHistory:false);
                        multiselectRemoveAction.AddUndoRedoAction(new RemoveNoteAction(leverNote.Container, leverNote.Container.Container));
                    }
                    else
                    {
                        isRemoveSuccessful = isRemoveSuccessful && leverNote.Container.RemoveNote(leverNote, updateEditHistory:false);
                        multiselectRemoveAction.AddUndoRedoAction(new RemoveNoteFromLeverSequenceAction(leverNote, leverNote.Container));
                    }
                    break;
                case EditableWheelFlick wheelFlick:
                    isRemoveSuccessful = isRemoveSuccessful && wheelFlick.Container.RemoveNote(wheelFlick, updateEditHistory:false);
                    multiselectRemoveAction.AddUndoRedoAction(new RemoveNoteAction(note, wheelFlick.Container));
                    break;
                case EditableWheelTurn wheelTurn:
                    // if sequence has already been removed, don't provess this
                    // otherwise, might try to delete starting vertex after sequence with one turn has already been removed
                    if (wheelTurn.Container.Container.ContainsNote(wheelTurn.Container))
                    {
                        if (wheelTurn.Container.Notes.Count <= 1) 
                        {
                            isRemoveSuccessful = isRemoveSuccessful && wheelTurn.Container.Container.RemoveNote(wheelTurn.Container, updateEditHistory:false);
                            multiselectRemoveAction.AddUndoRedoAction(new RemoveNoteAction(wheelTurn.Container, wheelTurn.Container.Container));
                        }
                        else if (wheelTurn.Container.startingNote == wheelTurn)
                        {
                            EditableWheelTurn firstTurn = wheelTurn.Container.Notes.ElementAt(0);
                            isRemoveSuccessful = isRemoveSuccessful && wheelTurn.Container.RemoveNoteFromFront(firstTurn, updateEditHistory:false);
                            multiselectRemoveAction.AddUndoRedoAction(new RemoveNoteFromWheelSequenceAction(firstTurn, wheelTurn.Container, isStartingVertex:true));
                        }
                        else
                        {
                            isRemoveSuccessful = isRemoveSuccessful && wheelTurn.Container.RemoveNote(wheelTurn, updateEditHistory:false);
                            multiselectRemoveAction.AddUndoRedoAction(new RemoveNoteFromWheelSequenceAction(wheelTurn, wheelTurn.Container));
                        }
                    }
                    break; 
                default:
                    break;
            }
        }
        multiselectRemoveAction.CommitAction();
        return isRemoveSuccessful;
    }
}
