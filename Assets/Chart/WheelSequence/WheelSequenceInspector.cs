using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WheelSequenceInspector : NoteContainerInspector<EditableWheelSequence, EditableWheelTurn>
{
    [SerializeField] TMP_InputField startTimeInput;
    private int editStartTime;
    [SerializeField] TMP_InputField startPosInput;
    private int editStartPos;
    private bool isEditInProgress;

    protected override void Awake()
    {
        base.Awake();

        startTimeInput.onValidateInput += (text, _, c) => c == '-' ? '\0' : c;
        startTimeInput.onValueChanged.AddListener(text =>
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
        startTimeInput.onEndEdit.AddListener(text =>
        {
            if (isEditInProgress)
            {
                int startTimeDelta = Note.StartTime - editStartTime;
                Dictionary<EditableWheelTurn, int> endTimes = Note.Notes.ToDictionary(n => n, n => n.EndTime);
                Dictionary<EditableWheelTurn, int> deltaAngles = Note.Notes.ToDictionary(n => n, n => n.DeltaAngle);
                EditWheelSequenceAction editWheelSequenceAction = new EditWheelSequenceAction(Note, startTimeDelta, 0, endTimes, endTimes, deltaAngles, deltaAngles);
                editWheelSequenceAction.CommitAction();
                isEditInProgress = false;
            }
        });

        startPosInput.onValidateInput += (text, _, c) => c == '-' ? '\0' : c;
        startPosInput.onValueChanged.AddListener(text =>
        {
            // track starting value of edit
            if (!isEditInProgress)
            {
                editStartPos = Note.Pos;
                isEditInProgress = true;
            }
            // process new value
            if (int.TryParse(text, out int pos))
            {
                Note.Pos = Mathf.Clamp(pos, 0, 100);
            }
        });
        startPosInput.onEndEdit.AddListener(text =>
        {
            if (isEditInProgress)
            {
                int posDelta = Note.Pos - editStartPos;
                Dictionary<EditableWheelTurn, int> endTimes = Note.Notes.ToDictionary(n => n, n => n.EndTime);
                Dictionary<EditableWheelTurn, int> deltaAngles = Note.Notes.ToDictionary(n => n, n => n.DeltaAngle);
                EditWheelSequenceAction editWheelSequenceAction = new EditWheelSequenceAction(Note, 0, posDelta, endTimes, endTimes, deltaAngles, deltaAngles);
                editWheelSequenceAction.CommitAction();
                isEditInProgress = false;
            }
        });
    }

    protected override EditableWheelTurn NewChildNote()
    {
        int endTime = Note.EndTime + ChartManager.ChartPackage.BeatToTime(1); // place 1 beat after last note
        // TODO: maybe place same distance as prev 2 notes?
        return new EditableWheelTurn(Note, endTime, 0);
    }

    void Update()
    {
        if (Note == null) return;

        if (!startTimeInput.isFocused) startTimeInput.SetTextWithoutNotify($"{Note.StartTime}");
        if (!startPosInput.isFocused) startPosInput.SetTextWithoutNotify($"{Note.Pos}");
    }
}
