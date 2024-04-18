using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WheelFlickInspector : NoteInspector<EditableWheelFlick>
{
    [SerializeField] TMP_InputField timeInput;
    [SerializeField] EventRelay leftToggle;
    [SerializeField] EventRelay rightToggle;

    void Awake()
    {
        timeInput.onValidateInput += (text, _, c) => c == '-' ? '\0' : c;
        timeInput.onValueChanged.AddListener(text =>
        {
            if (int.TryParse(text, out int time) && time >= 0)
            {
                Note.StartTime = time;
            }
        });
        leftToggle.Event.AddListener(() => Note.Dir = LeftRight.Left);
        rightToggle.Event.AddListener(() => Note.Dir = LeftRight.Right);
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
