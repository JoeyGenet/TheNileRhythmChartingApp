using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonNoteInspector : NoteInspector<EditableButtonNote>
{
    [SerializeField] TMP_InputField timeInput;
    private int editStartTime;
    [SerializeField] TMP_InputField leftPosInput;
    private int editStartLeftPos;
    [SerializeField] TMP_InputField rightPosInput;
    private bool isEditInProgress;
    private int? editStartRightPos;

    void Awake()
    {
        // start time field
        timeInput.onValidateInput += (text, _, c) => c == '-' ? '\0' : c;
        timeInput.onValueChanged.AddListener(text =>
        {
            // track starting value of edit
            if (!isEditInProgress)
            {
                editStartTime = Note.StartTime;
                isEditInProgress = true;
            } 
            // process new value
            if (int.TryParse(text, out int time))
            {
                Note.StartTime = Mathf.Clamp(time, 0, int.MaxValue); // TODO: clamp to song length
            }
        });
        timeInput.onEndEdit.AddListener(_ =>
        {
            int startTimeDelta = Note.StartTime - editStartTime;
            // commit new UndoRedoAction
            if (isEditInProgress)
            {
                EditButtonNoteAction editButtonNoteAction = new EditButtonNoteAction(Note, startTimeDelta, Note.LeftPos, Note.RightPos, Note.LeftPos, Note.RightPos);
                editButtonNoteAction.CommitAction();
                isEditInProgress = false;
            }
        });   

        // left position field
        leftPosInput.onValidateInput += (text, _, c) => c == '-' ? '\0' : c;
        leftPosInput.onValueChanged.AddListener(text =>
        {
            // track starting value of edit
            if (!isEditInProgress)
            {
                editStartLeftPos = Note.LeftPos;
                editStartRightPos = Note.RightPos;
                isEditInProgress = true;
            } 
            // process new value
            if (int.TryParse(text, out int pos))
            {
                Note.LeftPos = Mathf.Clamp(pos, 0, 100);
            }
        });
        leftPosInput.onEndEdit.AddListener(_ =>
        {
            if (Note.RightPos is int rightPos) Note.RightPos = Mathf.Clamp(rightPos, Note.LeftPos, 100);
            // commit new UndoRedoAction
            if (isEditInProgress)
            {
                EditButtonNoteAction editButtonNoteAction = new EditButtonNoteAction(Note, 0, editStartLeftPos, editStartRightPos, Note.LeftPos, Note.RightPos);
                editButtonNoteAction.CommitAction();
                isEditInProgress = false;
            }
        });

        // right pos field
        rightPosInput.onValidateInput += (text, _, c) => c == '-' ? '\0' : c;
        rightPosInput.onValueChanged.AddListener(text =>
        {
            // track starting value of edit
            if (!isEditInProgress)
            {
                editStartLeftPos = Note.LeftPos;
                editStartRightPos = Note.RightPos;
                isEditInProgress = true;
            } 
            // process new value
            if (text == "") Note.RightPos = null;
            else if (int.TryParse(text, out int pos))
            {
                Note.RightPos = Mathf.Clamp(pos, 0, 100);
            }
        });
        rightPosInput.onEndEdit.AddListener(_ =>
        {
            if (Note.RightPos is int rightPos) Note.LeftPos = Mathf.Clamp(Note.LeftPos, 0, rightPos);
            // commit new UndoRedoAction
            if (isEditInProgress)
            {
                EditButtonNoteAction editButtonNoteAction = new EditButtonNoteAction(Note, 0, editStartLeftPos, editStartRightPos, Note.LeftPos, Note.RightPos);
                editButtonNoteAction.CommitAction();
                isEditInProgress = false;
            }
        });   
    }

    void Update()
    {
        if (Note == null) return;
        
        if (!timeInput.isFocused) timeInput.SetTextWithoutNotify($"{Note.StartTime}");
        if (!leftPosInput.isFocused) leftPosInput.SetTextWithoutNotify($"{Note.LeftPos}");
        if (!rightPosInput.isFocused) rightPosInput.SetTextWithoutNotify($"{Note.RightPos}");
    }
}
