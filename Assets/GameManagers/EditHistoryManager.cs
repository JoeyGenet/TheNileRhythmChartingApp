using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

// MARK: Manager

public static class EditHistoryManager
{
    private static List<UndoRedoAction> editHistory = new List<UndoRedoAction>();
    private static int editHistoryHeadIndex = -1;

    public static void CommitAction(UndoRedoAction newAction)
    {
        // remove any history entries above the current head when we make a new change
        if (editHistory.Count != 0 && editHistoryHeadIndex + 1 < editHistory.Count)
        {
            int firstOverwrittenIndex = editHistoryHeadIndex + 1;
            int numberOfEntriesToRemove = editHistory.Count - firstOverwrittenIndex;
            editHistory.RemoveRange(firstOverwrittenIndex, numberOfEntriesToRemove);
        }
        editHistory.Add(newAction);
        Debug.Log("Performed " + newAction.Message + "\n\n" + newAction.ToString() + "\n");
        editHistoryHeadIndex += 1;
    }

    public static void Undo()
    {
        if (editHistoryHeadIndex >= 0)
        {
            PlaybackManager.Instance.Pause();
            UndoRedoAction undoRedoAction = editHistory[editHistoryHeadIndex];
            undoRedoAction.Undo();
            Debug.Log("Undid " + undoRedoAction.Message + "\n\n" + undoRedoAction.ToString() + "\n");
            editHistoryHeadIndex -= 1;
            UIManager.SelectElement(editHistoryHeadIndex>-1 ? editHistory[editHistoryHeadIndex].selectedElement : null, false);
        }
    }

    public static void Redo()
    {
        if (editHistoryHeadIndex < editHistory.Count - 1)
        {
            PlaybackManager.Instance.Pause();
            editHistoryHeadIndex += 1;
            UndoRedoAction undoRedoAction = editHistory[editHistoryHeadIndex];
            undoRedoAction.Redo();
            Debug.Log("Redid " + undoRedoAction.Message + "\n\n" + undoRedoAction.ToString() + "\n");
            UIManager.SelectElement(editHistory[editHistoryHeadIndex].selectedElement, false);
        }
    }

    public static void ClearEditHistory()
    {
        editHistory = new List<UndoRedoAction>();
        editHistoryHeadIndex = -1;
        Debug.Log("Edit history cleared.");
    }
}

// MARK: Abstract action

public abstract class UndoRedoAction
{
    // element that should be selected when this action is redone, or when the next action is undone.
    public object selectedElement;

    // message that describes the action performed, i.e. "add note"
    public abstract string Message { get; }

    // each UndoRedoAction implements a way to display its fields
    public abstract override string ToString();

    // each action has its own arguments that are serialized here

    // serialize the arguments necessary for the action and call Redo() to perform it
    public void CommitAction()
    {
        EditHistoryManager.CommitAction(this);
        // Redo();
    }

    // called when the action is first performed, or when it is redone
    public abstract void Redo();

    // called when undoing the action, opposite of Redo, uses same args
    public abstract void Undo();
}

public abstract class GroupUndoRedoAction : UndoRedoAction
{
    [SerializeField] public List<UndoRedoAction> undoRedoActions;

    public GroupUndoRedoAction(List<UndoRedoAction> undoRedoActions)
    {
        this.undoRedoActions = undoRedoActions;
    }

    public void AddUndoRedoAction(UndoRedoAction undoRedoAction)
    {
        undoRedoActions.Add(undoRedoAction);
    }

    public override void Redo()
    {
        foreach (UndoRedoAction undoRedoAction in undoRedoActions)
        {
            undoRedoAction.Redo();
        }
    }

    public override void Undo()
    {
        // actions must be done in true reverse order to preserve symmetry
        // e.g. remove two wheel turn notes in the reverse order they were added to preserve delta angles
        for (int i=undoRedoActions.Count-1; i>=0; i--)
        {
            undoRedoActions[i].Undo();
        }
    }
}

// MARK: Selection Actions
public class SelectElementAction : UndoRedoAction
{
    public override string Message { get { return "select element"; } }
    [SerializeField] private object oldSelectedElement;
    [SerializeField] private object newSelectedElement;

    public override string ToString()
    {
        return $"Old selected element: {oldSelectedElement}, ID: {oldSelectedElement?.GetHashCode()} \n"+
               $"New selected element: {newSelectedElement}, ID: {newSelectedElement?.GetHashCode()}";
    }

    public SelectElementAction(object oldSelectedElement, object newSelectedElement)
    {
        this.oldSelectedElement = oldSelectedElement;
        this.newSelectedElement = newSelectedElement;
        selectedElement = newSelectedElement;
    }

    // base EditHistoryManager.Undo()/Redo() methods change the selection based on the "selectedElement" of the action
    public override void Redo() 
    {
        if (newSelectedElement is Note note)
        {
            PlaybackManager.Instance.SnapToTimestamp(note.StartTime);
        }
        else if (newSelectedElement is EnvironmentChangeEvent environmentChangeEvent)
        {
            PlaybackManager.Instance.SnapToTimestamp(environmentChangeEvent.StartTime);
        }
        else if (newSelectedElement is TrackMarqueeSelectionGroup group)
        {
            PlaybackManager.Instance.SnapToTimestamp(group.notes.Select(note => note is EditableWheelTurn ? note.EndTime : note.StartTime).ToList());
        }
    }

    public override void Undo() 
    {
        if (oldSelectedElement is Note note)
        {
            PlaybackManager.Instance.SnapToTimestamp(note.StartTime);
        }
        else if (newSelectedElement is EnvironmentChangeEvent environmentChangeEvent)
        {
            PlaybackManager.Instance.SnapToTimestamp(environmentChangeEvent.StartTime);
        }
        else if (oldSelectedElement is TrackMarqueeSelectionGroup group)
        {
            PlaybackManager.Instance.SnapToTimestamp(group.notes.Select(note => note is EditableWheelTurn ? note.EndTime : note.StartTime).ToList());
        }
    }
}

// MARK: Basic note actions
// specific classes for different actions you can perform in the editor
// each defines its own parameters and behavior for undo/redo

public class AddNoteAction : UndoRedoAction
{
    public override string Message { get { return "add "+note?.GetType(); } }
    [SerializeField] private EditableChart editableChart;
    [SerializeField] private Note note;

    public override string ToString()
    {
        return "ID: " + note.Id + "\n" + 
               "Affects score: " + note.AffectsScore + "\n" +
               "Start time: " + note.StartTime + ", end time: " + note.EndTime;
    }

    public AddNoteAction(Note note, EditableChart editableChart)
    {
        this.editableChart = editableChart;
        this.note = note;
        selectedElement = note;
    }

    public override void Redo()
    {
        editableChart.AddNote(note, notify: true, updateEditHistory: false);
        // TODO: set timestamp to focus on the undone/redone edit if it is not already in view
        //PlaybackManager.Instance.SetTimestamp(note.StartTime);
        PlaybackManager.Instance.SnapToTimestamp(note.StartTime);
    }

    public override void Undo()
    {
        editableChart.RemoveNote(note, notify: true, updateEditHistory: false);
        PlaybackManager.Instance.SnapToTimestamp(note.StartTime);
    }
}

public class RemoveNoteAction : UndoRedoAction
{
    public override string Message { get { return "remove " + note?.GetType(); } }
    [SerializeField] private EditableChart editableChart;
    [SerializeField] private Note note;

    public override string ToString()
    {
        return "ID: " + note.Id + "\n" + 
               "Affects score: " + note.AffectsScore + "\n" +
               "Start time: " + note.StartTime + ", end time: " + note.EndTime;
    }

    public RemoveNoteAction(Note note, EditableChart editableChart)
    {
        this.editableChart = editableChart;
        this.note = note;
        selectedElement = null;
    }

    public override void Redo()
    {
        editableChart.RemoveNote(note, notify: true, updateEditHistory: false);
        PlaybackManager.Instance.SnapToTimestamp(note.StartTime);
    }

    public override void Undo()
    {
        editableChart.AddNote(note, notify: true, updateEditHistory: false);
        PlaybackManager.Instance.SnapToTimestamp(note.StartTime);
    }
}

// MARK: Button note actions
public class EditButtonNoteAction : UndoRedoAction
{
    public override string Message { get { return "edit button note"; } }
    [SerializeField] private EditableButtonNote buttonNote;
    [SerializeField] private int startTimeDelta;
    [SerializeField] private int oldLeftPos;
    [SerializeField] private int? oldRightPos;
    [SerializeField] private int newLeftPos;
    [SerializeField] private int? newRightPos;

    public override string ToString()
    {
        return "ID: " + buttonNote.Id + "\n" +
               "Affects score: " + buttonNote.AffectsScore + "\n" +
               "Start time delta: " + startTimeDelta + "\n" +
               "old left note position: " + oldLeftPos + ", new position: " + newLeftPos + "\n" +
               "old right note position: " + (oldRightPos!=null? oldRightPos : "null") + ", new position: " + (newRightPos!=null? newRightPos : "null");
    }

    public EditButtonNoteAction(EditableButtonNote buttonNote, int startTimeDelta, int oldLeftPos, int? oldRightPos,
                                int newLeftPos, int? newRightPos)
    {
        this.buttonNote = buttonNote;
        this.startTimeDelta = startTimeDelta;
        this.oldLeftPos = oldLeftPos;
        this.oldRightPos = oldRightPos;        
        this.newLeftPos = newLeftPos;
        this.newRightPos = newRightPos;
        selectedElement = buttonNote;
    }

    public override void Redo()
    {
        buttonNote.StartTime += startTimeDelta;
        buttonNote.LeftPos = newLeftPos;
        buttonNote.RightPos = newRightPos;
        PlaybackManager.Instance.SnapToTimestamp(buttonNote.StartTime);
    }

    public override void Undo()
    {
        buttonNote.StartTime -= startTimeDelta;
        buttonNote.LeftPos = oldLeftPos;
        buttonNote.RightPos = oldRightPos;
        PlaybackManager.Instance.SnapToTimestamp(buttonNote.StartTime);
    }
}

// MARK: Wheel Flick actions

public class EditWheelFlickAction : UndoRedoAction
{
    public override string Message { get { return "edit wheel flick"; } }

    [SerializeField] EditableWheelFlick editableWheelFlick;
    [SerializeField] int startTimeDelta;
    [SerializeField] int dirDelta;

    public override string ToString()
    {
        return "ID: " + editableWheelFlick.Id + "\n" +
               "Affects score: " + editableWheelFlick.AffectsScore + "\n" + 
               "Start time delta: " + startTimeDelta + "\n" +
               "Direction delta: " + dirDelta;
    }

    public EditWheelFlickAction(EditableWheelFlick editableWheelFlick, int startTimeDelta, int dirDelta)
    {
        this.editableWheelFlick = editableWheelFlick;
        this.startTimeDelta = startTimeDelta;
        this.dirDelta = dirDelta;
        selectedElement = editableWheelFlick;
    }

    public override void Redo()
    {
        editableWheelFlick.StartTime += startTimeDelta;
        editableWheelFlick.Dir += dirDelta;
        PlaybackManager.Instance.SnapToTimestamp(editableWheelFlick.StartTime);
    }

    public override void Undo()
    {
        editableWheelFlick.StartTime -= startTimeDelta;
        editableWheelFlick.Dir -= dirDelta;
        PlaybackManager.Instance.SnapToTimestamp(editableWheelFlick.StartTime);
    }
}

// MARK: Wheel Sequence actions

public class EditWheelSequenceAction : UndoRedoAction
{
    public override string Message { get { return "edit wheel sequence"; } }
    [SerializeField] private EditableWheelSequence wheelSequence;
    [SerializeField] private int startTimeDelta;
    [SerializeField] private int PosDelta;
    [SerializeField] private Dictionary<EditableWheelTurn, int> oldEndTimes;
    [SerializeField] private Dictionary<EditableWheelTurn, int> newEndTimes;
    [SerializeField] private Dictionary<EditableWheelTurn, int> oldDeltaAngles;
    [SerializeField] private Dictionary<EditableWheelTurn, int> newDeltaAngles;

    public override string ToString()
    {
        return "Sequence ID: " + wheelSequence.Id + "\n" +
               "Affects score: " + wheelSequence.AffectsScore + "\n" +
               (startTimeDelta != 0 ? ("Start time delta: " + startTimeDelta + "\n") : "") +
               (PosDelta != 0 ? ("Position delta: " + PosDelta + "\n" ) : "")+ 
               (oldEndTimes != null ? ("Old end times: " + string.Join(", ", oldEndTimes.Select(x => x.Value)) + "\n") : "") +
               (newEndTimes != null ? ("New end times: " + string.Join(", ", newEndTimes.Select(x => x.Value)) + "\n") : "") +
               (oldDeltaAngles != null ? ("Old delta angles: " + string.Join(", ", oldDeltaAngles.Select(x => x.Value)) + "\n") : "") +
               (newDeltaAngles != null ? ("New delta angles: " + string.Join(", ", newDeltaAngles.Select(x => x.Value))) : "");
    }

    public EditWheelSequenceAction(EditableWheelSequence wheelSequence, int startTimeDelta=0, int PosDelta=0,
                                   Dictionary<EditableWheelTurn, int> oldEndTimes=null, Dictionary<EditableWheelTurn, int> newEndTimes=null,
                                   Dictionary<EditableWheelTurn, int> oldDeltaAngles=null, Dictionary<EditableWheelTurn, int> newDeltaAngles=null)
    {
        this.wheelSequence = wheelSequence;
        this.startTimeDelta = startTimeDelta;
        this.PosDelta = PosDelta;
        this.oldEndTimes = oldEndTimes;
        this.newEndTimes = newEndTimes;
        this.oldDeltaAngles = oldDeltaAngles;
        this.newDeltaAngles = newDeltaAngles;
        selectedElement = wheelSequence;
    }

    public override void Redo()
    {
        wheelSequence.StartTime += startTimeDelta;
        if (newEndTimes != null)
        {
            foreach (var note in newEndTimes.Keys)
            {
                note.EndTime = newEndTimes[note];
            }
        }
        // changing the delta angle and wheel sequence position calls RecomputeStartAngle()
        // so these must be done after all other changes have been made (for some reason)
        if (newDeltaAngles != null)
        {
            foreach (var note in newDeltaAngles.Keys)
            {
                note.DeltaAngle = newDeltaAngles[note];
            }
        }
        wheelSequence.Pos += PosDelta;

        // pass all the changed turns to SnapToTimestamp--will snap to closest if NONE are in view
        List<int> changedTimes = new List<int>();
        // add start time if the whole sequence was moved
        if (startTimeDelta != 0 || PosDelta != 0)
        {
            changedTimes.Add(wheelSequence.StartTime);
        }
        // find all changed turns
        List<EditableWheelTurn> changedTurns = wheelSequence.Notes.Where(turn =>
            (oldEndTimes != null && newEndTimes != null && (oldEndTimes[turn] != newEndTimes[turn])) ||
            (oldDeltaAngles != null && newDeltaAngles != null && (oldDeltaAngles[turn] != newDeltaAngles[turn]))
        ).ToList();
        // add the end times of the changed turns (use updated end time if the time was changed)
        changedTimes.AddRange(changedTurns.Select(turn => newEndTimes != null ? newEndTimes[turn] : turn.EndTime));
        // snap to closest changed note
        if (changedTimes.Count != 0)
        {
            PlaybackManager.Instance.SnapToTimestamp(changedTimes);
        }
    }

    public override void Undo()
    {
        wheelSequence.StartTime -= startTimeDelta;
        if (oldEndTimes != null)
        {
            foreach (var note in oldEndTimes.Keys)
            {
                note.EndTime = oldEndTimes[note];
            }
        }
        // changing the delta angle and wheel sequence position calls RecomputeStartAngle()
        // so these must be done after all other changes have been made (for some reason)
        if (oldDeltaAngles != null)
        {
            foreach (var note in oldDeltaAngles.Keys)
            {
                note.DeltaAngle = oldDeltaAngles[note];
            }
        }
        wheelSequence.Pos -= PosDelta;

        // pass all the changed turns to SnapToTimestamp--will snap to closest if NONE are in view
        List<int> changedTimes = new List<int>();
        // add start time if the whole sequence was moved
        if (startTimeDelta != 0 || PosDelta != 0)
        {
            changedTimes.Add(wheelSequence.StartTime);
        }
        // find all changed turns
        List<EditableWheelTurn> changedTurns = wheelSequence.Notes.Where(turn =>
            (oldEndTimes != null && newEndTimes != null && (oldEndTimes[turn] != newEndTimes[turn])) ||
            (oldDeltaAngles != null && newDeltaAngles != null && (oldDeltaAngles[turn] != newDeltaAngles[turn]))
        ).ToList();
        // add the end times of the changed turns (use updated end time if the time was changed)
        changedTimes.AddRange(changedTurns.Select(turn => oldEndTimes != null ? oldEndTimes[turn] : turn.EndTime));
        // snap to closest changed note
        if (changedTimes.Count != 0)
        {
            PlaybackManager.Instance.SnapToTimestamp(changedTimes);
        }
    }
}

public class AddNoteToWheelSequenceAction : UndoRedoAction
{
    public override string Message { get { return "add note to wheel sequence"; } }
    [SerializeField] private EditableWheelSequence editableWheelSequence;
    [SerializeField] private EditableWheelTurn editableWheelTurn;

    public override string ToString()
    {
        EditableWheelTurn prevTurn = editableWheelTurn.PreviousTurn;
        EditableWheelTurn nextTurn = editableWheelTurn.NextTurn;
        return "Sequence ID: " + editableWheelSequence.Id + "\n" +
               "Previous turn ID: " + (prevTurn!=null? prevTurn.Id : "none") + ", next turn ID: " + (nextTurn!=null? nextTurn.Id : "none") + "\n" +
               "ID: " + editableWheelTurn.Id + "\n" +
               "Affects score: " + editableWheelTurn.AffectsScore + "\n" +
               "Start time: " + editableWheelTurn.StartTime + ", end time: " + editableWheelTurn.EndTime + "\n" +
               "Delta angle: " + editableWheelTurn.DeltaAngle + "\n";
    }

    public AddNoteToWheelSequenceAction(EditableWheelTurn editableWheelTurn, EditableWheelSequence editableWheelSequence)
    {
        this.editableWheelSequence = editableWheelSequence;
        this.editableWheelTurn = editableWheelTurn;
        selectedElement = editableWheelSequence;
    }

    public override void Redo()
    {
        editableWheelSequence.AddNote(editableWheelTurn, notify: true, updateEditHistory: false);
        PlaybackManager.Instance.SnapToTimestamp(editableWheelTurn.EndTime);
    }

    public override void Undo()
    {
        editableWheelSequence.RemoveNote(editableWheelTurn, notify: true, updateEditHistory: false);
        PlaybackManager.Instance.SnapToTimestamp(editableWheelTurn.EndTime);
    }
}

public class RemoveNoteFromWheelSequenceAction : UndoRedoAction
{
    public override string Message { get { return "remove note from wheel sequence"; } }
    [SerializeField] private EditableWheelSequence editableWheelSequence;
    [SerializeField] private EditableWheelTurn editableWheelTurn;
    [SerializeField] private bool isStartingVertex;

    public override string ToString()
    {
        EditableWheelTurn prevTurn = editableWheelTurn.PreviousTurn;
        EditableWheelTurn nextTurn = editableWheelTurn.NextTurn;
        return "Sequence ID: " + editableWheelSequence.Id + "\n" +
               "Previous turn ID: " + (prevTurn!=null? prevTurn.Id : "none") + ", next turn ID: " + (nextTurn!=null? nextTurn.Id : "none") + "\n" +
               "ID: " + editableWheelTurn.Id + "\n" +
               "Affects score: " + editableWheelTurn.AffectsScore + "\n" +
               "Start time: " + editableWheelTurn.StartTime + ", end time: " + editableWheelTurn.EndTime + "\n" +
               "Delta angle: " + editableWheelTurn.DeltaAngle + "\n";
    }

    public RemoveNoteFromWheelSequenceAction(EditableWheelTurn editableWheelTurn, EditableWheelSequence editableWheelSequence, bool isStartingVertex=false)
    {
        this.editableWheelSequence = editableWheelSequence;
        this.editableWheelTurn = editableWheelTurn;
        this.isStartingVertex = isStartingVertex;
        selectedElement = editableWheelSequence;
    }

    public override void Redo()
    {
        if (isStartingVertex)
        {
            editableWheelSequence.RemoveNoteFromFront(editableWheelTurn, notify: true, updateEditHistory: false);    
        }
        else
        {
            editableWheelSequence.RemoveNote(editableWheelTurn, notify: true, updateEditHistory: false);
        }
        PlaybackManager.Instance.SnapToTimestamp(editableWheelTurn.EndTime);
    }

    public override void Undo()
    {
        if (isStartingVertex)
        {
            editableWheelSequence.AddNoteToFront(editableWheelTurn, notify: true, updateEditHistory: false);    
        }
        else
        {
            editableWheelSequence.AddNote(editableWheelTurn, notify: true, updateEditHistory: false);
        }
        PlaybackManager.Instance.SnapToTimestamp(editableWheelTurn.EndTime);
    }
}

// MARK: Lever Sequence actions

public class EditLeverSequenceAction : UndoRedoAction
{
    public override string Message { get { return "edit lever sequence"; } }
    [SerializeField] private EditableLeverSequence leverSequence;
    [SerializeField] private int dirDelta;
    [SerializeField] private Dictionary<EditableLeverNote, int> oldStartTimes;
    [SerializeField] private Dictionary<EditableLeverNote, int> newStartTimes;
    [SerializeField] private Dictionary<EditableLeverNote, LeverPos> oldLeverPositions;
    [SerializeField] private Dictionary<EditableLeverNote, LeverPos> newLeverPositions;

    public override string ToString()
    {
        return "Sequence ID: " + leverSequence.Id + "\n" +
               "Affects score: " + leverSequence.AffectsScore + "\n" +
               (dirDelta != 0 ? ("Direction delta: " + dirDelta + "\n") : "") +
               (oldStartTimes != null ? ("Old start times: " + string.Join(", ", oldStartTimes.Select(x => x.Value)) + "\n") : "") +
               (newStartTimes != null ? ("New start times: " + string.Join(", ", newStartTimes.Select(x => x.Value)) + "\n") : "") +
               (oldLeverPositions != null ? ("Old lever positions: " + string.Join(", ", oldLeverPositions.Select(x => x.Value)) + "\n") : "") +
               (newLeverPositions != null ? ("New lever positions: " + string.Join(", ", newLeverPositions.Select(x => x.Value))) : "");
    }

    public EditLeverSequenceAction(EditableLeverSequence leverSequence, int dirDelta=0,
                                   Dictionary<EditableLeverNote, int> oldStartTimes=null, Dictionary<EditableLeverNote, int> newStartTimes=null,
                                   Dictionary<EditableLeverNote, LeverPos> oldLeverPositions=null, Dictionary<EditableLeverNote, LeverPos> newLeverPositions=null)
    {
        this.leverSequence = leverSequence;
        this.dirDelta = dirDelta;
        this.oldStartTimes = oldStartTimes;
        this.newStartTimes = newStartTimes;
        this.oldLeverPositions = oldLeverPositions;
        this.newLeverPositions = newLeverPositions;
        selectedElement = leverSequence;
    }

    public override void Redo()
    {
        leverSequence.Dir += dirDelta;
        if (newStartTimes != null)
        {
            foreach (EditableLeverNote note in newStartTimes.Keys)
            {
                note.StartTime = newStartTimes[note];
                note.Pos = newLeverPositions[note];
            }
        }

        // pass all the changed notes to SnapToTimestamp--will snap to closest if NONE are in view
        List<int> changedTimes = new List<int>();
        // add all start times if the lever sequence was moved to opposite track
        if (dirDelta != 0)
        {
            changedTimes.AddRange(leverSequence.Notes.Select(note => note.StartTime));
        }
        else
        {
            // find all changed notes
            List<EditableLeverNote> changedNotes = leverSequence.Notes.Where(note =>
                (oldStartTimes != null && newStartTimes != null && oldStartTimes[note] != newStartTimes[note]) ||
                (oldLeverPositions != null && newLeverPositions != null && oldLeverPositions[note] != newLeverPositions[note])
            ).ToList();
            // add the start times of the changed notes (use updated start time if the time was changed)
            changedTimes.AddRange(changedNotes.Select(note => newStartTimes != null ? newStartTimes[note] : note.StartTime));
        }
        // snap to closest changed note
        if (changedTimes.Count != 0)
        {
            PlaybackManager.Instance.SnapToTimestamp(changedTimes);
        }
    }

    public override void Undo()
    {
        leverSequence.Dir -= dirDelta;
        if (oldStartTimes != null)
        {
            foreach (EditableLeverNote note in oldStartTimes.Keys)
            {
                note.StartTime = oldStartTimes[note];
                note.Pos = oldLeverPositions[note];
            }
        }

        // pass all the changed notes to SnapToTimestamp--will snap to closest if NONE are in view
        List<int> changedTimes = new List<int>();
        // add all start times if the lever sequence was moved to opposite track
        if (dirDelta != 0)
        {
            changedTimes.AddRange(leverSequence.Notes.Select(note => note.StartTime));
        }
        else
        {
            // find all changed notes
            List<EditableLeverNote> changedNotes = leverSequence.Notes.Where(note =>
                (oldStartTimes != null && newStartTimes != null && oldStartTimes[note] != newStartTimes[note]) ||
                (oldLeverPositions != null && newLeverPositions != null && oldLeverPositions[note] != newLeverPositions[note])
            ).ToList();
            // add the start times of the changed notes (use updated start time if the time was changed)
            changedTimes.AddRange(changedNotes.Select(note => oldStartTimes != null ? oldStartTimes[note] : note.StartTime));
        }
        // snap to closest changed note
        if (changedTimes.Count != 0)
        {
            PlaybackManager.Instance.SnapToTimestamp(changedTimes);
        }
    }
}

public class AddNoteToLeverSequenceAction : UndoRedoAction
{
    public override string Message { get { return "add note to lever sequence"; } }
    [SerializeField] private EditableLeverSequence editableLeverSequence;
    [SerializeField] private EditableLeverNote editableLeverNote;

    public override string ToString()
    {
        EditableLeverNote prevNote = editableLeverNote.PreviousNote;
        EditableLeverNote nextNote = editableLeverNote.NextNote;
        return "Sequence ID: " + editableLeverSequence.Id + "\n" +
               "Previous note: " + (prevNote!=null? prevNote.Id : "none") + ", next note: " + (nextNote!=null? nextNote.Id : "none") + "\n" +
               "ID: " + editableLeverNote.Id + "\n" +
               "Affects score: " + editableLeverNote.AffectsScore + "\n" +
               "Start time: " + editableLeverNote.StartTime + ", end time: " + editableLeverNote.EndTime + "\n" +
               "Lever direction: " + editableLeverNote.Pos + "\n";
    }

    public AddNoteToLeverSequenceAction(EditableLeverNote editableLeverNote, EditableLeverSequence editableLeverSequence)
    {
        this.editableLeverSequence = editableLeverSequence;
        this.editableLeverNote = editableLeverNote;
        selectedElement = editableLeverSequence;
    }

    public override void Redo()
    {
        editableLeverSequence.AddNote(editableLeverNote, notify: true, updateEditHistory: false);
        PlaybackManager.Instance.SnapToTimestamp(editableLeverNote.StartTime);
    }

    public override void Undo()
    {
        editableLeverSequence.RemoveNote(editableLeverNote, notify: true, updateEditHistory: false);
        PlaybackManager.Instance.SnapToTimestamp(editableLeverNote.StartTime);
    }
}

public class RemoveNoteFromLeverSequenceAction : UndoRedoAction
{
    public override string Message { get { return "remove note from lever sequence"; } }
    [SerializeField] private EditableLeverSequence editableLeverSequence;
    [SerializeField] private EditableLeverNote editableLeverNote;

    public override string ToString()
    {
        EditableLeverNote prevNote = editableLeverNote.PreviousNote;
        EditableLeverNote nextNote = editableLeverNote.NextNote;
        return "Sequence ID: " + editableLeverSequence.Id + "\n" +
               "Previous note: " + (prevNote!=null? prevNote.Id : "none") + ", next note: " + (nextNote!=null? nextNote.Id : "none") + "\n" +
               "ID: " + editableLeverNote.Id + "\n" +
               "Affects score: " + editableLeverNote.AffectsScore + "\n" +
               "Start time: " + editableLeverNote.StartTime + ", end time: " + editableLeverNote.EndTime + "\n" +
               "Lever direction: " + editableLeverNote.Pos + "\n";
    }

    public RemoveNoteFromLeverSequenceAction(EditableLeverNote editableLeverNote, EditableLeverSequence editableLeverSequence)
    {
        this.editableLeverSequence = editableLeverSequence;
        this.editableLeverNote = editableLeverNote;
        selectedElement = editableLeverSequence;
    }

    public override void Redo()
    {
        editableLeverSequence.RemoveNote(editableLeverNote, notify: true, updateEditHistory: false);
        PlaybackManager.Instance.SnapToTimestamp(editableLeverNote.StartTime);
    }

    public override void Undo()
    {
        editableLeverSequence.AddNote(editableLeverNote, notify: true, updateEditHistory: false);
        PlaybackManager.Instance.SnapToTimestamp(editableLeverNote.StartTime);
    }
}

// MARK: Multiselect actions
public class ModifyMultiselectionAction : UndoRedoAction
{
    public override string Message { get { return "modify multiselection"; } }
    [SerializeField] SortedSet<Note> oldNotes;
    [SerializeField] SortedSet<Note> newNotes;


    public override string ToString()
    {
        int noteCountDiff = newNotes.Count - oldNotes.Count;
        return $"{Mathf.Abs(noteCountDiff)} notes {(noteCountDiff>0 ? "added to" : "removed from")} selection.";
    }

    public ModifyMultiselectionAction(SortedSet<Note> oldNotes, SortedSet<Note> newNotes, TrackMarqueeSelectionGroup trackMarqueeSelectionGroup)
    {
        this.oldNotes = oldNotes;
        this.newNotes = newNotes;
        selectedElement = trackMarqueeSelectionGroup;
    }

    public override void Redo()
    {
        UIManager.MultiselectElements(addToSelection:true, newNotes, modifyExistingSelection:false, updateEditHistory:false);

        // if oldNotes is bigger than newNotes, snap to nearest note that appears only in oldNotes (nearest deselected note)
        // if newNotes is bigger than oldNotes, snap to nearest note that appears only in newNotes (nearest added note)
        List<Note> changedNotes = oldNotes.Count > newNotes.Count ? oldNotes.Except(newNotes).ToList() : newNotes.Except(oldNotes).ToList();
        if (changedNotes != null && changedNotes.Count != 0)
        {
            // use end time for wheel turns, start time for everything else
            PlaybackManager.Instance.SnapToTimestamp(changedNotes.Select(note => note is EditableWheelTurn ? note.EndTime : note.StartTime).ToList());
        }
    }

    public override void Undo()
    {
        UIManager.MultiselectElements(addToSelection:true, oldNotes, modifyExistingSelection:false, updateEditHistory:false);

        // if oldNotes is bigger than newNotes, snap to nearest note that appears only in oldNotes (nearest deselected note)
        // if newNotes is bigger than oldNotes, snap to nearest note that appears only in newNotes (nearest added note)
        List<Note> changedNotes = oldNotes.Count > newNotes.Count ? oldNotes.Except(newNotes).ToList() : newNotes.Except(oldNotes).ToList();
        if (changedNotes != null && changedNotes.Count != 0)
        {
            // use end time for wheel turns, start time for everything else
            PlaybackManager.Instance.SnapToTimestamp(changedNotes.Select(note => note is EditableWheelTurn ? note.EndTime : note.StartTime).ToList());
        }
    }
}

public class MultiselectMoveAction : GroupUndoRedoAction
{
    public override string Message { get { return "multiselect move"; }}

    public override string ToString()
    {
        return $"{undoRedoActions.Count} notes moved.\n";
    }

    public MultiselectMoveAction(List<UndoRedoAction> undoRedoActions, TrackMarqueeSelectionGroup trackMarqueeSelectionGroup) : base(undoRedoActions)
    {
        selectedElement = trackMarqueeSelectionGroup;
    }

    public override void Redo()
    {
        // cache timestamp
        int timestamp = PlaybackManager.Instance.MillisecondsInChart;
        base.Redo();
        PlaybackManager.Instance.SetTimestamp(timestamp);
        if (UIManager.SelectionGroup != null && UIManager.SelectionGroup.notes.Count != 0)
        {
            List<int> changedTimes = new List<int>();
            // use end time for wheel turns, start time for everything else
            // do not include wheel sequences--the starting note will be considered if it is multiselected
            foreach (Note note in UIManager.SelectionGroup.notes)
            {
                if (note is EditableWheelTurn)
                {
                    changedTimes.Add(note.EndTime);
                }
                else if (note is not EditableWheelSequence)
                {
                    changedTimes.Add(note.StartTime);
                }
            }
            PlaybackManager.Instance.SnapToTimestamp(changedTimes);
        }
    }

    public override void Undo()
    {
        // cache timestamp
        int timestamp = PlaybackManager.Instance.MillisecondsInChart;
        base.Undo();
        PlaybackManager.Instance.SetTimestamp(timestamp);
        if (UIManager.SelectionGroup != null && UIManager.SelectionGroup.notes.Count != 0)
        {
            List<int> changedTimes = new List<int>();
            // use end time for wheel turns, start time for everything else
            // do not include wheel sequences--the starting note will be considered if it is multiselected
            foreach (Note note in UIManager.SelectionGroup.notes)
            {
                if (note is EditableWheelTurn)
                {
                    changedTimes.Add(note.EndTime);
                }
                else if (note is not EditableWheelSequence)
                {
                    changedTimes.Add(note.StartTime);
                }
            }
            PlaybackManager.Instance.SnapToTimestamp(changedTimes);
        }
    }
}

public class MultiselectPasteAction : GroupUndoRedoAction
{
    [SerializeField] public SortedSet<Note> pastedNotes;
    public override string Message { get { return "multiselect paste"; }}

    public override string ToString()
    {
        return $"{undoRedoActions.Count} notes pasted.\n";
    }

    public MultiselectPasteAction(List<UndoRedoAction> undoRedoActions) : base(undoRedoActions) { }

    public override void Redo()
    {
        // cache timestamp
        int timestamp = PlaybackManager.Instance.MillisecondsInChart;
        base.Redo();
        PlaybackManager.Instance.SetTimestamp(timestamp);
        if (pastedNotes != null && pastedNotes.Count != 0)
        {
            List<int> changedTimes = new List<int>();
            // use end time for wheel turns, start time for everything else
            // do not include wheel sequences--the starting note will be considered if it is multiselected
            foreach (Note note in pastedNotes)
            {
                if (note is EditableWheelTurn)
                {
                    changedTimes.Add(note.EndTime);
                }
                else if (note is not EditableWheelSequence)
                {
                    changedTimes.Add(note.StartTime);
                }
            }
            PlaybackManager.Instance.SnapToTimestamp(changedTimes);
        }
    }

    public override void Undo()
    {
        // cache timestamp
        int timestamp = PlaybackManager.Instance.MillisecondsInChart;
        base.Undo();
        PlaybackManager.Instance.SetTimestamp(timestamp);
        if (pastedNotes != null && pastedNotes.Count != 0)
        {
            List<int> changedTimes = new List<int>();
            // use end time for wheel turns, start time for everything else
            // do not include wheel sequences--the starting note will be considered if it is multiselected
            foreach (Note note in pastedNotes)
            {
                if (note is EditableWheelTurn)
                {
                    changedTimes.Add(note.EndTime);
                }
                else if (note is not EditableWheelSequence)
                {
                    changedTimes.Add(note.StartTime);
                }
            }
            PlaybackManager.Instance.SnapToTimestamp(changedTimes);
        }
    }
}

public class MultiselectRemoveAction : GroupUndoRedoAction
{
    public override string Message { get { return "multiselect delete"; }}

    public override string ToString()
    {
        return $"{undoRedoActions.Count} notes deleted.\n";
    }

    public MultiselectRemoveAction(List<UndoRedoAction> undoRedoActions) : base(undoRedoActions) 
    {
        selectedElement = null;
    }

    public override void Redo()
    {
        // cache timestamp
        int timestamp = PlaybackManager.Instance.MillisecondsInChart;
        base.Redo();
        PlaybackManager.Instance.SetTimestamp(timestamp);
        if (UIManager.SelectionGroup != null && UIManager.SelectionGroup.notes.Count != 0)
        {
            List<int> changedTimes = new List<int>();
            // use end time for wheel turns, start time for everything else
            // do not include wheel sequences--the starting note will be considered if it is multiselected
            foreach (Note note in UIManager.SelectionGroup.notes)
            {
                if (note is EditableWheelTurn)
                {
                    changedTimes.Add(note.EndTime);
                }
                else if (note is not EditableWheelSequence)
                {
                    changedTimes.Add(note.StartTime);
                }
            }
            PlaybackManager.Instance.SnapToTimestamp(changedTimes);
        }
    }

    public override void Undo()
    {
        // cache timestamp
        int timestamp = PlaybackManager.Instance.MillisecondsInChart;
        base.Undo();
        PlaybackManager.Instance.SetTimestamp(timestamp);
        if (UIManager.SelectionGroup != null && UIManager.SelectionGroup.notes.Count != 0)
        {
            List<int> changedTimes = new List<int>();
            // use end time for wheel turns, start time for everything else
            // do not include wheel sequences--the starting note will be considered if it is multiselected
            foreach (Note note in UIManager.SelectionGroup.notes)
            {
                if (note is EditableWheelTurn)
                {
                    changedTimes.Add(note.EndTime);
                }
                else if (note is not EditableWheelSequence)
                {
                    changedTimes.Add(note.StartTime);
                }
            }
            PlaybackManager.Instance.SnapToTimestamp(changedTimes);
        }
    }
}

public class MultiselectMirrorAction : GroupUndoRedoAction
{
    public override string Message { get { return "multiselect mirrror"; }}

    public override string ToString()
    {
        return $"{undoRedoActions.Count} notes mirrored.\n";
    }

    public MultiselectMirrorAction(List<UndoRedoAction> undoRedoActions, TrackMarqueeSelectionGroup trackMarqueeSelectionGroup) : base(undoRedoActions) 
    {
        selectedElement = trackMarqueeSelectionGroup;
    } 

    public override void Redo()
    {
        // cache timestamp
        int timestamp = PlaybackManager.Instance.MillisecondsInChart;
        base.Redo();
        PlaybackManager.Instance.SetTimestamp(timestamp);
        if (UIManager.SelectionGroup != null && UIManager.SelectionGroup.notes.Count != 0)
        {
            List<int> changedTimes = new List<int>();
            // use end time for wheel turns, start time for everything else
            // do not include wheel sequences--the starting note will be considered if it is multiselected
            foreach (Note note in UIManager.SelectionGroup.notes)
            {
                if (note is EditableWheelTurn)
                {
                    changedTimes.Add(note.EndTime);
                }
                else if (note is not EditableWheelSequence)
                {
                    changedTimes.Add(note.StartTime);
                }
            }
            PlaybackManager.Instance.SnapToTimestamp(changedTimes);
        }
    }

    public override void Undo()
    {
        // cache timestamp
        int timestamp = PlaybackManager.Instance.MillisecondsInChart;
        base.Undo();
        PlaybackManager.Instance.SetTimestamp(timestamp);
        if (UIManager.SelectionGroup != null && UIManager.SelectionGroup.notes.Count != 0)
        {
            List<int> changedTimes = new List<int>();
            // use end time for wheel turns, start time for everything else
            // do not include wheel sequences--the starting note will be considered if it is multiselected
            foreach (Note note in UIManager.SelectionGroup.notes)
            {
                if (note is EditableWheelTurn)
                {
                    changedTimes.Add(note.EndTime);
                }
                else if (note is not EditableWheelSequence)
                {
                    changedTimes.Add(note.StartTime);
                }
            }
            PlaybackManager.Instance.SnapToTimestamp(changedTimes);
        }
    }
}

// MARK: Environment actions

public class AddEnvironmentChangeAction : UndoRedoAction
{
    public override string Message { get { return "add environment change"; }}
    [SerializeField] EditableEnvironmentChangeEvent editableEnvironmentChangeEvent;
    [SerializeField] EditableChart editableChart;

    public override string ToString()
    {
        return $"Environment: {editableEnvironmentChangeEvent.Environment} \n"+
               $"Start Time: {editableEnvironmentChangeEvent.StartTime} \n"+
               $"End Time: {editableEnvironmentChangeEvent.EndTime} \n"+
               $"Duration: {editableEnvironmentChangeEvent.Duration}";
    }

    public AddEnvironmentChangeAction(EditableEnvironmentChangeEvent editableEnvironmentChangeEvent, EditableChart editableChart)
    {
        this.editableEnvironmentChangeEvent = editableEnvironmentChangeEvent;
        this.editableChart = editableChart;
        selectedElement = editableEnvironmentChangeEvent;
    }

    public override void Redo()
    {
        editableChart.AddEvent(editableEnvironmentChangeEvent);
        PlaybackManager.Instance.SnapToTimestamp(editableEnvironmentChangeEvent.StartTime);
    }

    public override void Undo()
    {
        editableChart.RemoveEvent(editableEnvironmentChangeEvent);
        PlaybackManager.Instance.SnapToTimestamp(editableEnvironmentChangeEvent.StartTime);
    }
}

public class RemoveEnvironmentChangeAction : UndoRedoAction
{
    public override string Message { get { return "remove environment change"; }}
    [SerializeField] EditableEnvironmentChangeEvent editableEnvironmentChangeEvent;
    [SerializeField] EditableChart editableChart;

    public override string ToString()
    {
        return $"Environment: {editableEnvironmentChangeEvent.Environment} \n"+
               $"Start Time: {editableEnvironmentChangeEvent.StartTime} \n"+
               $"End Time: {editableEnvironmentChangeEvent.EndTime} \n"+
               $"Duration: {editableEnvironmentChangeEvent.Duration}";
    }

    public RemoveEnvironmentChangeAction(EditableEnvironmentChangeEvent editableEnvironmentChangeEvent, EditableChart editableChart)
    {
        this.editableEnvironmentChangeEvent = editableEnvironmentChangeEvent;
        this.editableChart = editableChart;
        selectedElement = null;
    }

    public override void Redo()
    {
        editableChart.RemoveEvent(editableEnvironmentChangeEvent);
        PlaybackManager.Instance.SnapToTimestamp(editableEnvironmentChangeEvent.StartTime);
    }

    public override void Undo()
    {
        editableChart.AddEvent(editableEnvironmentChangeEvent);
        PlaybackManager.Instance.SnapToTimestamp(editableEnvironmentChangeEvent.StartTime);
    }
}

public class EditEnvironmentChangeAction : UndoRedoAction
{
    public override string Message { get { return "edit environment change"; }}
    [SerializeField] EditableEnvironmentChangeEvent editableEnvironmentChangeEvent;
    [SerializeField] public int startTimeDelta;
    [SerializeField] public int endTimeDelta;
    [SerializeField] public Environment oldEnvironment;
    [SerializeField] public Environment newEnvironment;

    public override string ToString()
    {
        return $"Old environment: {oldEnvironment}, new environment: {newEnvironment} \n"+
               $"Start Time delta: {startTimeDelta} \n"+
               $"End Time delta: {endTimeDelta} \n";
    }

    public EditEnvironmentChangeAction(EditableEnvironmentChangeEvent editableEnvironmentChangeEvent, int startTimeDelta,
                                       int endTimeDelta, Environment oldEnvironment, Environment newEnvironment)
    {
        this.editableEnvironmentChangeEvent = editableEnvironmentChangeEvent;
        this.startTimeDelta = startTimeDelta;
        this.endTimeDelta = endTimeDelta;
        this.oldEnvironment = oldEnvironment;
        this.newEnvironment = newEnvironment;
        selectedElement = editableEnvironmentChangeEvent;
    }

    public override void Redo()
    {
        if (startTimeDelta > 0 || endTimeDelta > 0)
        {
            editableEnvironmentChangeEvent.EndTime += endTimeDelta;
            editableEnvironmentChangeEvent.StartTime += startTimeDelta;
        }
        else if (startTimeDelta < 0 || endTimeDelta < 0)
        {
            editableEnvironmentChangeEvent.StartTime += startTimeDelta;
            editableEnvironmentChangeEvent.EndTime += endTimeDelta;
        }
        editableEnvironmentChangeEvent.Environment = newEnvironment;
        PlaybackManager.Instance.SnapToTimestamp(editableEnvironmentChangeEvent.StartTime);
    }

    public override void Undo()
    {
        if (-startTimeDelta > 0 || -endTimeDelta > 0)
        {
            editableEnvironmentChangeEvent.EndTime -= endTimeDelta;
            editableEnvironmentChangeEvent.StartTime -= startTimeDelta;
        }
        else if (-startTimeDelta < 0 || -endTimeDelta < 0)
        {
            editableEnvironmentChangeEvent.StartTime -= startTimeDelta;
            editableEnvironmentChangeEvent.EndTime -= endTimeDelta;
        }
        editableEnvironmentChangeEvent.Environment = oldEnvironment;
        PlaybackManager.Instance.SnapToTimestamp(editableEnvironmentChangeEvent.StartTime);
    }
}

// MARK: Metadata actions

public class EditMetadataAction : UndoRedoAction
{
    public override string Message { get { return "edit metadata"; } }
    [SerializeField] public EditableChartPackage editableChartPackage;
    [SerializeField] public string oldCoverImagePath;
    [SerializeField] public string newCoverImagePath;
    [SerializeField] public string oldTitle;
    [SerializeField] public string newTitle;
    [SerializeField] public string oldArtist;
    [SerializeField] public string newArtist;
    [SerializeField] public string oldCharter;
    [SerializeField] public string newCharter;
    [SerializeField] public string oldSongClipPath;
    [SerializeField] public string newSongClipPath;
    [SerializeField] public uint bpmDelta;
    [SerializeField] public int bpmOffsetDelta;
    [SerializeField] public float gainDelta;
    [SerializeField] public uint previewStartDelta;
    [SerializeField] public uint previewDurationDelta;

    public override string ToString()
    {
        return ((oldTitle!=null && newTitle!=null) ? "Old title: "+oldTitle+", new title: "+newTitle+"\n" : "") +
               ((oldArtist!=null && newArtist!=null) ? "Old artist: "+oldArtist+", new artist: "+newArtist+"\n" : "") +
               ((oldCharter!=null && newCharter!=null) ? "Old charter: "+oldCharter+", new charter: "+newCharter+"\n" : "") +
               ((oldSongClipPath!=null || newSongClipPath!=null) ? "Old song clip: "+oldSongClipPath+", new song clip: "+newSongClipPath+"\n" : "") +
               ((oldCoverImagePath!=null || newCoverImagePath!=null) ? "Old cover image: "+oldCoverImagePath+", new cover image: "+newCoverImagePath+"\n" : "") +
               ((bpmDelta!=0) ? "BPM delta: "+bpmDelta+"\n" : "") +
               ((bpmOffsetDelta!=0) ? "BPM offset delta: "+bpmOffsetDelta+"\n" : "") +
               ((gainDelta!=0) ? "Gain delta: "+gainDelta+"\n" : "") +
               ((previewStartDelta!=0) ? "Preview start delta: "+previewStartDelta+"\n" : "") +
               ((previewDurationDelta!=0) ? "Preview duration delta: "+previewDurationDelta+"\n" : "");
    }

    public EditMetadataAction(EditableChartPackage editableChartPackage, string oldCoverImagePath=null, string newCoverImagePath=null,
                              string oldTitle=null, string newTitle=null, string oldArtist=null, string newArtist=null,
                              string oldCharter=null, string newCharter=null, string oldSongPath=null, string newSongPath=null,
                              uint bpmDelta=0, int bpmOffsetDelta=0, float gainDelta=0, uint previewStartDelta=0, uint previewDurationDelta=0)
    {
        this.editableChartPackage = editableChartPackage;
        this.oldTitle = oldTitle;
        this.newTitle = newTitle;
        this.oldArtist = oldArtist;
        this.newArtist = newArtist;
        this.oldCharter = oldCharter;
        this.newCharter = newCharter;
        this.oldCoverImagePath = oldCoverImagePath;
        this.newCoverImagePath = newCoverImagePath;
        this.oldSongClipPath = oldSongPath;
        this.newSongClipPath = newSongPath;
        this.bpmDelta = bpmDelta;
        this.bpmOffsetDelta = bpmOffsetDelta;
        this.gainDelta = gainDelta;
        this.previewStartDelta = previewStartDelta;
        this.previewDurationDelta = previewDurationDelta;        
    }

    public override void Redo()
    {
        editableChartPackage.Title = newTitle ?? editableChartPackage.Title;
        editableChartPackage.Artist = newArtist ?? editableChartPackage.Artist;
        editableChartPackage.Charter = newCharter ?? editableChartPackage.Charter;
        editableChartPackage.BPM += bpmDelta;
        editableChartPackage.BPMOffset += bpmOffsetDelta;
        editableChartPackage.Gain += gainDelta;
        editableChartPackage.PreviewStart += previewStartDelta;
        editableChartPackage.UnclampedPreviewDuration += previewDurationDelta;

        // cover image
        if (!string.IsNullOrEmpty(newCoverImagePath))
        {
            _ = editableChartPackage.SetCoverImage(newCoverImagePath);
        }
        else if (!string.IsNullOrEmpty(oldCoverImagePath))
        {
            editableChartPackage.ResetCoverImage();
        }

        // song file
        if (!string.IsNullOrEmpty(newSongClipPath))
        {
            _ = editableChartPackage.SetSong(newSongClipPath);
        }
        else if (!string.IsNullOrEmpty(oldSongClipPath))
        {
            editableChartPackage.ResetSong();
        }
    }

    public override void Undo()
    {
        editableChartPackage.Title = oldTitle ?? editableChartPackage.Title;
        editableChartPackage.Artist = oldArtist ?? editableChartPackage.Artist;
        editableChartPackage.Charter = oldCharter ?? editableChartPackage.Charter;
        editableChartPackage.BPM -= bpmDelta;
        editableChartPackage.BPMOffset -= bpmOffsetDelta;
        editableChartPackage.Gain -= gainDelta;
        editableChartPackage.PreviewStart -= previewStartDelta;
        editableChartPackage.UnclampedPreviewDuration -= previewDurationDelta;

        // cover image
        if (!string.IsNullOrEmpty(oldCoverImagePath))
        {
            _ = editableChartPackage.SetCoverImage(oldCoverImagePath);
        }
        else if (!string.IsNullOrEmpty(newCoverImagePath))
        {
            editableChartPackage.ResetCoverImage();
        }

        // song file
        if (!string.IsNullOrEmpty(oldSongClipPath))
        {
            _ = editableChartPackage.SetSong(oldSongClipPath);
        }
        else if (!string.IsNullOrEmpty(newSongClipPath))
        {
            editableChartPackage.ResetSong();
        }
    }
}

// MARK: Chart actions

public class AddChartAction : UndoRedoAction
{
    public override string Message { get { return "add new chart"; }}
    [SerializeField] EditableChart editableChart;

    public override string ToString()
    {
        return $"Difficulty rating: {editableChart.DifficultyRating} \n"+
                $"Difficulty name: {editableChart.DifficultyName}";
    }

    public AddChartAction(EditableChart editableChart)
    {
        this.editableChart = editableChart;
    }

    public override void Redo()
    {
        ChartManager.ChartPackage.AddChart(editableChart, updateEditHistory: false);
    }

    public override void Undo()
    {
        ChartManager.ChartPackage.RemoveChart(editableChart, updateEditHistory: false);
    }
}

public class RemoveChartAction : UndoRedoAction
{
    public override string Message { get { return "remove new chart"; }}
    [SerializeField] EditableChart editableChart;

    public override string ToString()
    {
        return $"Difficulty rating: {editableChart.DifficultyRating} \n"+
                $"Difficulty name: {editableChart.DifficultyName}";
    }

    public RemoveChartAction(EditableChart editableChart)
    {
        this.editableChart = editableChart;
    }

    public override void Redo()
    {
        ChartManager.ChartPackage.RemoveChart(editableChart, updateEditHistory: false);
    }

    public override void Undo()
    {
        ChartManager.ChartPackage.AddChart(editableChart, updateEditHistory: false);
    }
}

public class SwitchChartAction : UndoRedoAction
{
    public override string Message { get { return "switch to chart"; }}
    [SerializeField] EditableChart oldEditableChart;
    [SerializeField] EditableChart newEditableChart;

    public override string ToString()
    {
        return $"Old chart difficulty rating: {oldEditableChart.DifficultyRating} \n"+
               $"Old chart difficulty name: {oldEditableChart.DifficultyName} \n --- \n" +
               $"New chart difficulty rating: {newEditableChart.DifficultyRating} \n"+
               $"New chart difficulty name: {newEditableChart.DifficultyName}";
    }

    public SwitchChartAction(EditableChart oldEditableChart, EditableChart newEditableChart)
    {
        this.oldEditableChart = oldEditableChart;
        this.newEditableChart = newEditableChart;
    }

    public override void Redo()
    {
        ChartManager.SelectedChart = newEditableChart;
    }

    public override void Undo()
    {
        ChartManager.SelectedChart = oldEditableChart;
    }
}

public class EditChartAction : UndoRedoAction
{
    public override string Message { get { return "edit chart settings"; }}
    [SerializeField] EditableChart editableChart;
    [SerializeField] int ratingDelta;
    [SerializeField] string oldName;
    [SerializeField] string newName;

    public override string ToString()
    {
        return $"Rating delta: {ratingDelta} \n"+
               $"Old difficulty name: {oldName} \n"+
               $"New difficulty name: {newName}";
    }

    public EditChartAction(EditableChart editableChart, int ratingDelta, string oldName, string newName)
    {
        this.editableChart = editableChart;
        this.ratingDelta = ratingDelta;
        this.oldName = oldName;
        this.newName = newName;
    }

    public override void Redo()
    {
        editableChart.DifficultyRating += ratingDelta;
        editableChart.DifficultyName = newName;
    }

    public override void Undo()
    {
        editableChart.DifficultyRating -= ratingDelta;
        editableChart.DifficultyName = oldName;
    }
}