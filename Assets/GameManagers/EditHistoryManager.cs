using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EditHistoryManager
{
    private static List<UndoRedoAction> editHistory = new List<UndoRedoAction>();
    private static int editHistoryHeadIndex = -1;

    public static void AddAction(UndoRedoAction newAction)
    {
        Debug.Log("AddAction called");
        // remove any history entries above the current head when we make a new change
        if (editHistory.Count != 0 && editHistoryHeadIndex + 1 < editHistory.Count)
        {
            int firstOverwrittenIndex = editHistoryHeadIndex + 1;
            int numberOfEntriesToRemove = editHistory.Count - firstOverwrittenIndex;
            editHistory.RemoveRange(firstOverwrittenIndex, numberOfEntriesToRemove);
        }
        editHistory.Add(newAction);
        editHistoryHeadIndex += 1;
    }

    public static void Undo()
    {
        Debug.Log("Undo button pressed");
        if (editHistoryHeadIndex >= 0)
        {
            editHistory[editHistoryHeadIndex].Undo();
            editHistoryHeadIndex -= 1;
        }
    }

    public static void Redo()
    {
        Debug.Log("Redo button pressed");
        if (editHistoryHeadIndex < editHistory.Count - 1)
        {
            editHistoryHeadIndex += 1;
            editHistory[editHistoryHeadIndex].Redo();
        }
    }
}

public abstract class UndoRedoAction
{
    // each action has its own arguments that are serialized here

    // serialize the arguments necessary for the action and call Redo() to perform it
    public void AddAction()
    {
        EditHistoryManager.AddAction(this);
        // Redo();
    }

    // called when the action is first performed, or when it is redone
    public abstract void Redo();

    // called when undoing the action, opposite of Redo, uses same args
    public abstract void Undo();
}

// MARK: UndoRedoAction Implementor classes
// specific classes for different actions you can perform in the editor
// each defines its own parameters and behavior for undo/redo

public class MoveButtonNoteAction : UndoRedoAction
{
    [SerializeField] private EditableButtonNote buttonNote;
    [SerializeField] private int startTimeDelta;
    [SerializeField] private int leftPosDelta;
    [SerializeField] private int rightPosDelta;

    public MoveButtonNoteAction(EditableButtonNote buttonNote, int startTimeDelta, int leftPosDelta, int rightPosDelta)
    {
        this.buttonNote = buttonNote;
        this.startTimeDelta = startTimeDelta;
        this.leftPosDelta = leftPosDelta;
        this.rightPosDelta = rightPosDelta;
    }

    public override void Redo()
    {
        Debug.Log("Redo button note move");
        buttonNote.StartTime += startTimeDelta;
        buttonNote.LeftPos += leftPosDelta;
        buttonNote.RightPos += rightPosDelta;

    }

    public override void Undo()
    {
        Debug.Log("Undo button note move");
        buttonNote.StartTime -= startTimeDelta;
        buttonNote.LeftPos -= leftPosDelta;
        buttonNote.RightPos -= rightPosDelta;
    }
}