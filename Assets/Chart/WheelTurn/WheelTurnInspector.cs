using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WheelTurnInspector : WheelSequenceInspector.ChildInspector
{
    [SerializeField] TMP_InputField deltaAngleInput;
    private Dictionary<EditableWheelTurn, int> editStartDeltaAngles;
    [SerializeField] TMP_InputField endTimeInput;
    private Dictionary<EditableWheelTurn, int> editStartEndTimes;
    private bool isEditInProgress;
    
    protected override void Awake()
    {
        base.Awake();
        
        deltaAngleInput.onValueChanged.AddListener(text =>
        {
            // track starting value of edit
            if (!isEditInProgress)
            {
                editStartDeltaAngles = Note.Container.Notes.ToDictionary(n => n, n => n.DeltaAngle);
                isEditInProgress = true;
            }
            // process new value
            if (int.TryParse(text, out int deltaAngle))
            {
                Note.DeltaAngle = Mathf.Clamp(deltaAngle, -3600, 3600); // Clamp to prevent too many lines
            }
        });
        deltaAngleInput.onEndEdit.AddListener(text =>
        {
            if (isEditInProgress)
            {
                Dictionary<EditableWheelTurn, int> endTimes = Note.Container.Notes.ToDictionary(n => n, n => n.EndTime);
                Dictionary<EditableWheelTurn, int> newDeltaAngles = Note.Container.Notes.ToDictionary(n => n, n => n.DeltaAngle);
                EditWheelSequenceAction editWheelSequenceAction = new EditWheelSequenceAction(Note.Container, 0, 0, endTimes, endTimes, editStartDeltaAngles, newDeltaAngles);
                editWheelSequenceAction.CommitAction();
                isEditInProgress = false;
            }
        });

        endTimeInput.onValidateInput += (text, _, c) => c == '-' ? '\0' : c;
        endTimeInput.onValueChanged.AddListener(text =>
        {
            // track starting value of edit
            if (!isEditInProgress)
            {
                editStartEndTimes = Note.Container.Notes.ToDictionary(n => n, n => n.EndTime);
                isEditInProgress = true;
            }
            // process new value
            if (int.TryParse(text, out int endTime))
            {
                if (endTime > Note.Container.StartTime)
                {
                    Note.EndTime = Mathf.Clamp(endTime, 0, int.MaxValue); // TODO: clamp to song length
                }
            }
        });
        endTimeInput.onEndEdit.AddListener(text =>
        {
            if (isEditInProgress)
            {
                Dictionary<EditableWheelTurn, int> newEndTimes = Note.Container.Notes.ToDictionary(n => n, n => n.EndTime);
                Dictionary<EditableWheelTurn, int> deltaAngles = Note.Container.Notes.ToDictionary(n => n, n => n.DeltaAngle);
                EditWheelSequenceAction editWheelSequenceAction = new EditWheelSequenceAction(Note.Container, 0, 0, editStartEndTimes, newEndTimes, deltaAngles, deltaAngles);
                editWheelSequenceAction.CommitAction();
                isEditInProgress = false;
            }
        });

        // deleteButton.onClick.AddListener(() =>
        // {

        //     var note = Note;
        //     try
        //     {
        //         Note.Container.RemoveNote(Note);
        //     }
        //     catch (Exception e)
        //     {
        //         UIManager.Instance.ShowAlert("Exception While Deleting Wheel Turn", $"Note {note} may not have been deleted.\n{e.Message}");
        //         Debug.LogException(e);
        //     }
        // });

        // SaveManager.OnWheelTurnRemoved += RemoveNote;
    }

    void Update()
    {
        if (!deltaAngleInput.isFocused) deltaAngleInput.SetTextWithoutNotify($"{Note.DeltaAngle}");
        if (!endTimeInput.isFocused) endTimeInput.SetTextWithoutNotify($"{Note.EndTime}");
    }
}
