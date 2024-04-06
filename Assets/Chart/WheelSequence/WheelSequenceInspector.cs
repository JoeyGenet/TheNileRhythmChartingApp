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
    [SerializeField] TMP_InputField startPosInput;

    // [Header("Wheel Turn")]
    // [SerializeField] RectTransform wheelTurnInspectors;
    // [SerializeField] WheelTurnInspector wheelTurnInspectorPrefab;
    // [SerializeField] Button addTurnButton;

    protected override void Awake()
    {
        base.Awake();

        startTimeInput.onValidateInput += (text, _, c) => c == '-' ? '\0' : c;
        startTimeInput.onValueChanged.AddListener(text =>
        {
            if (int.TryParse(text, out int time))
            {
                Note.StartTime = Mathf.Clamp(time, 0, int.MaxValue); // TODO: clamp to song length
            }
        });

        startPosInput.onValidateInput += (text, _, c) => c == '-' ? '\0' : c;
        startPosInput.onValueChanged.AddListener(text =>
        {
            if (int.TryParse(text, out int pos))
            {
                Note.Pos = Mathf.Clamp(pos, 0, 100);
            }
        });
    }

    protected override EditableWheelTurn NewChildNote()
    {
        int endTime = Note.EndTime + ChartManager.ChartPackage.BeatToTime(1); // place 1 beat after last note
        // TODO: maybe place same distance as prev 2 notes?
        return new EditableWheelTurn(Note, endTime, 0);
    }

    protected override void Update()
    {
        base.Update();

        if (Note == null) return;

        if (!startTimeInput.isFocused) startTimeInput.SetTextWithoutNotify($"{Note.StartTime}");
        if (!startPosInput.isFocused) startPosInput.SetTextWithoutNotify($"{Note.Pos}");
    }
}
