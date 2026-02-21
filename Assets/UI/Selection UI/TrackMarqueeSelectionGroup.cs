using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.EventSystems.ExecuteEvents;

public class TrackMarqueeSelectionGroup
{
    private Track track;
    public SortedSet<Note> notes { get; private set; }
    private MultiselectMoveAction multiselectMoveAction;

    public TrackMarqueeSelectionGroup(Track track)
    {
        notes = new SortedSet<Note>();
        this.track = track;
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
            if (track.noteGameObjectMapping.TryGetValue(note, out GameObject gameObject) && gameObject!=null)
            {
                // flag all selected notes to change their click/drag/drop behavior
                // only the clickable renderers will be added--no WheelSequences or LeverSequences here
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
            if (track.noteGameObjectMapping.TryGetValue(note, out GameObject gameObject) && gameObject!=null)
            {
                Execute(gameObject, eventData, functor);
            }
        }
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
    
}
