using System.Collections;
using System.Collections.Generic;
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
        Debug.Log("Performed " + newAction.Message);
        editHistoryHeadIndex += 1;
    }

    public static void Undo()
    {
        if (editHistoryHeadIndex >= 0)
        {
            editHistory[editHistoryHeadIndex].Undo();
            Debug.Log("Undid " + editHistory[editHistoryHeadIndex].Message);
            editHistoryHeadIndex -= 1;
        }
    }

    public static void Redo()
    {
        if (editHistoryHeadIndex < editHistory.Count - 1)
        {
            editHistoryHeadIndex += 1;
            editHistory[editHistoryHeadIndex].Redo();
            Debug.Log("Redid " + editHistory[editHistoryHeadIndex].Message);
        }
    }
}

// MARK: Abstract action

public abstract class UndoRedoAction
{
    // message that describes the action performed, i.e. "add note"
    public abstract string Message { get; }

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

// MARK: Sequence actions

public class AddNoteToWheelSequenceAction : UndoRedoAction
{
    public override string Message { get { return "add note to wheel sequence"; } }
    [SerializeField] private EditableWheelSequence editableWheelSequence;
    [SerializeField] private EditableWheelTurn editableWheelTurn;

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

public class AddNoteToLeverSequenceAction : UndoRedoAction
{
    public override string Message { get {return "add note to lever sequence"; } }
    [SerializeField] private EditableLeverSequence editableLeverSequence;
    [SerializeField] private EditableLeverNote editableLeverNote;

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
