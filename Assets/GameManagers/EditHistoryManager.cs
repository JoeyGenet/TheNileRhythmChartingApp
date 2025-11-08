using System.Collections.Generic;
using System.Linq;
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
            UndoRedoAction undoRedoAction = editHistory[editHistoryHeadIndex];
            undoRedoAction.Undo();
            Debug.Log("Undid " + undoRedoAction.Message + "\n\n" + undoRedoAction.ToString() + "\n");
            editHistoryHeadIndex -= 1;
        }
    }

    public static void Redo()
    {
        if (editHistoryHeadIndex < editHistory.Count - 1)
        {
            editHistoryHeadIndex += 1;
            UndoRedoAction undoRedoAction = editHistory[editHistoryHeadIndex];
            undoRedoAction.Redo();
            Debug.Log("Redid " + undoRedoAction.Message + "\n\n" + undoRedoAction.ToString() + "\n");
        }
    }
}

// MARK: Abstract action

public abstract class UndoRedoAction
{
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

// MARK: Basic note actions
// specific classes for different actions you can perform in the editor
// each defines its own parameters and behavior for undo/redo

public class AddNoteAction : UndoRedoAction
{
    public override string Message { get {return "add "+note?.GetType(); } }
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
    }

    public override void Redo()
    {
        editableChart.AddNote(note, notify: true, updateEditHistory: false);
    }

    public override void Undo()
    {
        editableChart.RemoveNote(note, notify: true, updateEditHistory: false);
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
    }

    public override void Redo()
    {
        editableChart.RemoveNote(note, notify: true, updateEditHistory: false);
    }

    public override void Undo()
    {
        editableChart.AddNote(note, notify: true, updateEditHistory: false);
    }
}

// MARK: Button note actions
public class EditButtonNoteAction : UndoRedoAction
{
    public override string Message { get {return "edit button note"; } }
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
    }

    public override void Redo()
    {
        buttonNote.StartTime += startTimeDelta;
        buttonNote.LeftPos = newLeftPos;
        buttonNote.RightPos = newRightPos;

    }

    public override void Undo()
    {
        buttonNote.StartTime -= startTimeDelta;
        buttonNote.LeftPos = oldLeftPos;
        buttonNote.RightPos = oldRightPos;
    }
}

// MARK: Wheel Sequence actions

public class EditWheelSequenceAction : UndoRedoAction
{
    public override string Message { get {return "edit wheel sequence"; } }
    [SerializeField] private EditableWheelSequence wheelSequence;
    [SerializeField] private int startTimeDelta;
    [SerializeField] private int PosDelta;
    [SerializeField] private Dictionary<EditableWheelTurn, int> oldEndTimes;
    [SerializeField] private Dictionary<EditableWheelTurn, int> newEndTimes;
    [SerializeField] private Dictionary<EditableWheelTurn, int> oldDeltaAngles;
    [SerializeField] private Dictionary<EditableWheelTurn, int> newDeltaAngles;

    public override string ToString()
    {
        return "ID: " + wheelSequence.Id + "\n" +
               "Affects score: " + wheelSequence.AffectsScore + "\n" +
               "Start time delta: " + startTimeDelta + "\n" +
               "Position delta: " + PosDelta + "\n" + 
               "Old end times: " + string.Join(", ", oldEndTimes.Select(x => x.Value)) + "\n" +
               "New end times: " + string.Join(", ", newEndTimes.Select(x => x.Value)) + "\n" +
               "Old delta angles: " + string.Join(", ", oldDeltaAngles.Select(x => x.Value)) + "\n" +
               "New delta angles: " + string.Join(", ", newDeltaAngles.Select(x => x.Value));
    }

    public EditWheelSequenceAction(EditableWheelSequence wheelSequence, int startTimeDelta, int PosDelta,
                                   Dictionary<EditableWheelTurn, int> oldEndTimes, Dictionary<EditableWheelTurn, int> newEndTimes,
                                   Dictionary<EditableWheelTurn, int> oldDeltaAngles, Dictionary<EditableWheelTurn, int> newDeltaAngles)
    {
        this.wheelSequence = wheelSequence;
        this.startTimeDelta = startTimeDelta;
        this.PosDelta = PosDelta;
        this.oldEndTimes = oldEndTimes;
        this.newEndTimes = newEndTimes;
        this.oldDeltaAngles = oldDeltaAngles;
        this.newDeltaAngles = newDeltaAngles;
    }

    public override void Redo()
    {
        wheelSequence.StartTime += startTimeDelta;
        foreach (var note in newEndTimes.Keys)
        {
            note.EndTime = newEndTimes[note];
        }
        // changing the delta angle and wheel sequence position calls RecomputeStartAngle()
        // so these must be done after all other changes have been made (for some reason)
        foreach (var note in newDeltaAngles.Keys)
        {
            note.DeltaAngle = newDeltaAngles[note];
        }
        wheelSequence.Pos += PosDelta;
    }

    public override void Undo()
    {
        wheelSequence.StartTime -= startTimeDelta;
        foreach (var note in oldEndTimes.Keys)
        {
            note.EndTime = oldEndTimes[note];
        }
        // changing the delta angle and wheel sequence position calls RecomputeStartAngle()
        // so these must be done after all other changes have been made (for some reason)
        foreach (var note in oldDeltaAngles.Keys)
        {
            note.DeltaAngle = oldDeltaAngles[note];
        }
        wheelSequence.Pos -= PosDelta;
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
    }

    public override void Redo()
    {
        editableWheelSequence.AddNote(editableWheelTurn, notify: true, updateEditHistory: false);
    }

    public override void Undo()
    {
        editableWheelSequence.RemoveNote(editableWheelTurn, notify: true, updateEditHistory: false);
    }
}

public class RemoveNoteFromWheelSequenceAction : UndoRedoAction
{
    public override string Message { get {return "remove note from wheel sequence"; } }
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

    public RemoveNoteFromWheelSequenceAction(EditableWheelTurn editableWheelTurn, EditableWheelSequence editableWheelSequence)
    {
        this.editableWheelSequence = editableWheelSequence;
        this.editableWheelTurn = editableWheelTurn;
    }

    public override void Redo()
    {
        editableWheelSequence.RemoveNote(editableWheelTurn, notify: true, updateEditHistory: false);
    }

    public override void Undo()
    {
        editableWheelSequence.AddNote(editableWheelTurn, notify: true, updateEditHistory: false);
    }
}

// MARK: Lever Sequence actions

public class AddNoteToLeverSequenceAction : UndoRedoAction
{
    public override string Message { get {return "add note to lever sequence"; } }
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
    }

    public override void Redo()
    {
        editableLeverSequence.AddNote(editableLeverNote, notify: true, updateEditHistory: false);
    }

    public override void Undo()
    {
        editableLeverSequence.RemoveNote(editableLeverNote, notify: true, updateEditHistory: false);
    }
}

public class RemoveNoteFromLeverSequenceAction : UndoRedoAction
{
    public override string Message { get {return "remove note from lever sequence"; } }
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
    }

    public override void Redo()
    {
        editableLeverSequence.RemoveNote(editableLeverNote, notify: true, updateEditHistory: false);
    }

    public override void Undo()
    {
        editableLeverSequence.AddNote(editableLeverNote, notify: true, updateEditHistory: false);
    }
}
