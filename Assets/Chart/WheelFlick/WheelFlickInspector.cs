using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WheelFlickInspector : NoteInspector<EditableWheelFlick>
{
    [SerializeField] TMP_InputField timeInput;
    private int editStartTime;
    [SerializeField] EventRelay leftToggle;
    [SerializeField] EventRelay rightToggle;
    private bool isEditInProgress;

    void Awake()
    {
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
            if (int.TryParse(text, out int time) && time >= 0)
            {
                Note.StartTime = time;
            }
        });
        timeInput.onEndEdit.AddListener(text =>
        {
            if (isEditInProgress)
            {
                int startTimeDelta = Note.StartTime - editStartTime;
                EditWheelFlickAction editWheelFlickAction = new EditWheelFlickAction(Note, startTimeDelta, 0);
                editWheelFlickAction.CommitAction();
                isEditInProgress = false;
            }
        });

        leftToggle.Event.AddListener(() => 
        {
            int dirDelta = LeftRight.Left - Note.Dir;
            if (dirDelta != 0)
            {
                EditWheelFlickAction editWheelFlickAction = new EditWheelFlickAction(Note, 0, dirDelta);
                editWheelFlickAction.CommitAction();
            }
            Note.Dir = LeftRight.Left;
        });

        rightToggle.Event.AddListener(() => 
        {
            int dirDelta = LeftRight.Right - Note.Dir;
            if (dirDelta != 0)
            {
                EditWheelFlickAction editWheelFlickAction = new EditWheelFlickAction(Note, 0, dirDelta);
                editWheelFlickAction.CommitAction();
            }
            Note.Dir = LeftRight.Right;
        });
    }

    void Update()
    {
        if (Note == null) return;

        if (!timeInput.isFocused) timeInput.SetTextWithoutNotify($"{Note.StartTime}");
        
        if (Note.Dir == LeftRight.Left)
        {
            if (!leftToggle.AreAllTogglesOn) leftToggle.SendEvent(notify: false, playAudio: false);
        }
        else if (Note.Dir == LeftRight.Right)
        {
            if (!rightToggle.AreAllTogglesOn) rightToggle.SendEvent(notify: false, playAudio: false);
        }
    }
}
