using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeverNoteInspector : LeverSequenceInspector.ChildInspector
{
    [SerializeField] TMP_InputField timeInput;
    private Dictionary<EditableLeverNote, int> startEditStartTimes;
    [SerializeField] TMP_Dropdown posDropdown;
    private Dictionary<EditableLeverNote, LeverPos> startEditLeverPositions;
    private bool isEditInProgress;

    [Header("Colors")]
    [SerializeField] private Color leftColor;
    [SerializeField] private Color upColor;
    [SerializeField] private Color rightColor;
    [SerializeField] private Color downColor;
    [SerializeField] private Color middleColor;
    
    protected override void Awake()
    {
        base.Awake();
        
        timeInput.onValidateInput += (text, _, c) => c == '-' ? '\0' : c;
        timeInput.onValueChanged.AddListener(text =>
        {
            // track starting value of edit
            if (!isEditInProgress)
            {
                startEditStartTimes = Note.Container.Notes.ToDictionary(n => n, n => n.StartTime);
                isEditInProgress = true;
            }
            // process new value
            if (int.TryParse(text, out int time))
            {
                Note.StartTime = Mathf.Clamp(time, 0, int.MaxValue); // TODO: clamp to song length
            }
        });
        timeInput.onEndEdit.AddListener(text => 
        {
            if (isEditInProgress)
            {
                Dictionary<EditableLeverNote, int> newStartTimes = Note.Container.Notes.ToDictionary(n => n, n => n.StartTime);
                Dictionary<EditableLeverNote, LeverPos> leverPositions = Note.Container.Notes.ToDictionary(n => n, n => n.Pos);
                EditLeverSequenceAction editLeverSequenceAction = new EditLeverSequenceAction(Note.Container, 0, startEditStartTimes, newStartTimes, leverPositions, leverPositions);
                editLeverSequenceAction.CommitAction();
                isEditInProgress = false;
            }
        });

        posDropdown.onValueChanged.AddListener(value =>
        {
            if (Note.Pos != (LeverPos) value)
            {
                Dictionary<EditableLeverNote, int> startTimes = Note.Container.Notes.ToDictionary(n => n, n => n.StartTime);
                // set dictionary of lever positions before and after setting the new position
                startEditLeverPositions = Note.Container.Notes.ToDictionary(n => n, n => n.Pos);
                Note.Pos = (LeverPos) value;
                Dictionary<EditableLeverNote, LeverPos> newLeverPositions = Note.Container.Notes.ToDictionary(n => n, n => n.Pos);
                EditLeverSequenceAction editLeverSequenceAction = new EditLeverSequenceAction(Note.Container, 0, startTimes, startTimes, startEditLeverPositions, newLeverPositions);
                editLeverSequenceAction.CommitAction();
            }

        });
    }

    void Start()
    {
        posDropdown.value = (int) Note.Pos;
    }

    void Update()
    {   
        if (!timeInput.isFocused) timeInput.SetTextWithoutNotify($"{Note.StartTime}");

        posDropdown.SetValueWithoutNotify((int) Note.Pos);

        float angle;
        (angle, posDropdown.captionImage.color) = Note.Pos switch
        {
            LeverPos.Left => (0, leftColor),
            LeverPos.Up => (-90, upColor),
            LeverPos.Right => (180, rightColor),
            LeverPos.Down => (90, downColor),
            LeverPos.Middle => (0, middleColor),
            _ => throw new Exception($"Lever direction {Note.Pos} not supported."),
        };
        posDropdown.captionImage.rectTransform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
