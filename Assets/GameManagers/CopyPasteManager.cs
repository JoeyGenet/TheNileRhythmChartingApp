using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class CopyPasteManager : MonoBehaviour
{
    public static CopyPasteManager Instance;
    public SortedSet<Note> noteClipboard;

    private void Awake() {
        noteClipboard = new SortedSet<Note>();

        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
        }
    }

    // Create deep copies of all selected notes and put them in the clipboard
    // the clipboard notes will stay the same even if the originals are altered or deleted
    public static void CopySelectedNotesToClipboard(SortedSet<Note> notes)
    {
        Instance.noteClipboard = new SortedSet<Note>();
        foreach (Note note in notes)
        {
            switch (note)
            {
                case EditableButtonNote:
                    EditableButtonNote buttonNote = note as EditableButtonNote;
                    Instance.noteClipboard.Add(new EditableButtonNote(null, buttonNote));
                    break;
                case EditableLeverSequence:
                    EditableLeverSequence leverSequence = note as EditableLeverSequence;
                    EditableLeverSequence leverSequenceCopy = new EditableLeverSequence(null, leverSequence);
                    for (int i=0; i<leverSequence.Notes.Count; i++)
                    {
                        if (notes.Contains(leverSequence.Notes.ElementAt(i)))
                        {
                            Instance.noteClipboard.Add(leverSequenceCopy.Notes.ElementAt(i));
                        }
                    }
                    break;
                case EditableWheelFlick:
                    EditableWheelFlick wheelFlick = note as EditableWheelFlick;
                    Instance.noteClipboard.Add(new EditableWheelFlick(null, wheelFlick));
                    break;
                case EditableWheelSequence:
                    EditableWheelSequence wheelSequence = note as EditableWheelSequence;
                    EditableWheelSequence wheelSequenceCopy = new EditableWheelSequence(null, wheelSequence);
                    if (notes.Contains(wheelSequence.startingNote) || notes.Contains(wheelSequence.Notes.First)) Instance.noteClipboard.Add(wheelSequenceCopy.startingNote);
                    for (int i=0; i<wheelSequence.Notes.Count; i++)
                    {
                        if (notes.Contains(wheelSequence.Notes.ElementAt(i)))
                        {
                            Instance.noteClipboard.Add(wheelSequenceCopy.Notes.ElementAt(i));
                        }
                    }
                    break;
                default:
                    break;
            }
        }
    }

    public static void PasteNotesFromClipboard(int time)
    {
        if (Instance.noteClipboard.Count > 0)
        {
            // deselect current note or selection
            // commit as UndoRedoAction
            UIManager.SelectElement(null);

            MultiselectPasteAction multiselectPasteAction = new MultiselectPasteAction( new List<UndoRedoAction>());
            EditableChart chart = ChartManager.SelectedChart;
            int selectionStartTime = Instance.noteClipboard.First().StartTime;
            int pasteStartTime = time;
            // after pasting, make a selection using the pasted notes
            SortedSet<Note> pastedNotes = new SortedSet<Note>();

            foreach (Note note in Instance.noteClipboard)
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
                        pastedNotes.Add(pastedButtonNote);
                        break;
                    case EditableLeverNote:
                        EditableLeverNote leverNote = note as EditableLeverNote;
                        int newLeverNoteStartTime = leverNote.StartTime - selectionStartTime + pasteStartTime;
                        (UndoRedoAction addLeverNoteAction, SortedSet<Note> pastedLeverNotes) = PasteLeverNote(leverNote, newLeverNoteStartTime, chart);
                        multiselectPasteAction.AddUndoRedoAction(addLeverNoteAction);
                        pastedNotes.AddRange(pastedLeverNotes);
                        break;
                    case EditableWheelFlick:
                        EditableWheelFlick wheelFlick = note as EditableWheelFlick;
                        int newWheelFlickStartTime = wheelFlick.StartTime - selectionStartTime + pasteStartTime;
                        EditableWheelFlick pastedWheelFlick = new EditableWheelFlick(chart, newWheelFlickStartTime, wheelFlick.Dir);
                        chart.AddNote(pastedWheelFlick, updateEditHistory:false);
                        AddNoteAction addWheelFlickAction = new AddNoteAction(pastedWheelFlick, chart);
                        multiselectPasteAction.AddUndoRedoAction(addWheelFlickAction);
                        pastedNotes.Add(pastedWheelFlick);
                        break;
                    case EditableWheelTurn:
                        EditableWheelTurn wheelTurn = note as EditableWheelTurn;
                        int newWheelTurnStartTime = wheelTurn.StartTime - selectionStartTime + pasteStartTime;
                        int newWheelTurnEndTime = wheelTurn.EndTime - selectionStartTime + pasteStartTime;
                        (UndoRedoAction addWheelTurnAction, SortedSet<Note> pastedWheelNotes) = PasteWheelTurn(wheelTurn, newWheelTurnStartTime, newWheelTurnEndTime, wheelTurn.StartAngle, wheelTurn.DeltaAngle, chart);
                        if (addWheelTurnAction!=null)
                        {
                            multiselectPasteAction.AddUndoRedoAction(addWheelTurnAction);
                            pastedNotes.AddRange(pastedWheelNotes);
                        }
                        break;
                    default:
                        break;
                }
            }

            // select the pasted notes in multiselection
            UIManager.MultiselectElements(true, pastedNotes, modifyExistingSelection:false, createNewSelection:true, updateEditHistory:false);
            multiselectPasteAction.selectedElement = UIManager.SelectionGroup;
            multiselectPasteAction.pastedNotes = pastedNotes;
            
            multiselectPasteAction.CommitAction();
        }
    }

    private static (UndoRedoAction, SortedSet<Note>) PasteWheelTurn(EditableWheelTurn originalTurn, int startTime, int endTime, int startAngle, int deltaAngle, EditableChart chart)
    {
        // the starting vertex has a dummy note only used for selection logic--it does not represent real note data, so skip it
        bool isStartingVertex = originalTurn.Container.startingNote == originalTurn;
        bool previousTurnInSequenceWasPasted = originalTurn.PreviousTurn!=null && Instance.noteClipboard.Contains(originalTurn.PreviousTurn);
        // If placing inside an existing sequence or holding shift, add to the previous sequence. Otherwise create a new sequence
        var lastWheelSequence = chart.GetNotesBefore(endTime).OfType<EditableWheelSequence>().LastOrDefault();
        bool addToWheelSequence = lastWheelSequence != null && (
            (lastWheelSequence.EndTime > startTime && lastWheelSequence.StartTime < endTime) || // pasted note falls between two existing notes
            previousTurnInSequenceWasPasted ||                                                  // the original note's previous note was pasted
            Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));               // if holding shift, always append

        if (addToWheelSequence)
        {
            if (isStartingVertex)
            {
                startAngle = WheelTurn.PosToAngle(originalTurn.Container.Pos);
            }
            int endAngle = startAngle + deltaAngle;

            EditableWheelTurn prevNoteOnTrack = lastWheelSequence.GetNotesBefore(endTime).LastOrDefault() as EditableWheelTurn;
            EditableWheelTurn prevNote = prevNoteOnTrack;
            int timeDiff = startTime - originalTurn.StartTime;
            // if original turn's previous note is closer to pasted note than the start of the last sequence or prevNoteOnTrack,
            // use the original turn's end angle
            if (previousTurnInSequenceWasPasted && ((prevNoteOnTrack==null && originalTurn.PreviousTurn.EndTime+timeDiff >= lastWheelSequence.StartTime) ||
                                                    (prevNoteOnTrack!=null && originalTurn.PreviousTurn.EndTime+timeDiff >= prevNoteOnTrack.EndTime)))
            {
                prevNote = originalTurn.PreviousTurn;
            }

            // if no previous note can be found, use angle from sequence start pos
            // otherwise, use end angle of previous note
            int prevEndAngle = prevNote==null ? WheelTurn.PosToAngle(lastWheelSequence.Pos) : prevNote.StartAngle + prevNote.DeltaAngle;

            EditableWheelTurn editableWheelTurn = new EditableWheelTurn(lastWheelSequence, endTime, endAngle - prevEndAngle);
            lastWheelSequence.AddNote(editableWheelTurn, updateEditHistory:false);
            AddNoteToWheelSequenceAction addNoteToWheelSequenceAction = new AddNoteToWheelSequenceAction(editableWheelTurn, lastWheelSequence);
            return (addNoteToWheelSequenceAction, new SortedSet<Note>{editableWheelTurn});
        }
        else if (!isStartingVertex)
        {
            int newSequencePos = WheelTurn.AngleToPos(WheelTurn.WrapAngle(startAngle));
            EditableWheelSequence editableWheelSequence = new EditableWheelSequence(chart, startTime, newSequencePos, new() { (deltaAngle, endTime) });
            chart.AddNote(editableWheelSequence, updateEditHistory:false);
            AddNoteAction addNoteAction = new AddNoteAction(editableWheelSequence, chart);
            return (addNoteAction, new SortedSet<Note>{editableWheelSequence, editableWheelSequence.startingNote, editableWheelSequence.Notes.First});
        }
        else
        {
            return (null, null);
        }
    }

    private static (UndoRedoAction, SortedSet<Note>) PasteLeverNote(EditableLeverNote originalNote, int newTime, EditableChart chart)
    {
        LeftRight side = originalNote.Container.Dir;
        LeverPos leverPos = originalNote.Pos;

        // If placing inside an existing sequence or holding shift, add to the previous sequence. Otherwise create a new sequence
        var lastLeverSequence = chart.GetNotesBefore(newTime, inclusive:false).OfType<EditableLeverSequence>().LastOrDefault(seq => seq.Dir == side);
        bool addToLeverSequence = lastLeverSequence != null && (lastLeverSequence.EndTime > newTime 
            || (originalNote.PreviousNote!=null && Instance.noteClipboard.Contains(originalNote.PreviousNote))
            || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        
        if (addToLeverSequence)
        {
            EditableLeverNote editableLeverNote = new EditableLeverNote(lastLeverSequence, newTime, leverPos);
            lastLeverSequence.AddNote(editableLeverNote, updateEditHistory:false);
            AddNoteToLeverSequenceAction addNoteToLeverSequenceAction = new AddNoteToLeverSequenceAction(editableLeverNote, lastLeverSequence);
            return (addNoteToLeverSequenceAction, new SortedSet<Note>{editableLeverNote});
        }
        else
        {
            EditableLeverSequence editableLeverSequence = new EditableLeverSequence(chart, newTime, side, new() { (newTime, leverPos) });
            chart.AddNote(editableLeverSequence, updateEditHistory:false);
            AddNoteAction addNoteAction = new AddNoteAction(editableLeverSequence, chart);
            return (addNoteAction, new SortedSet<Note>{editableLeverSequence, editableLeverSequence.Notes.First});
        }
    }
}