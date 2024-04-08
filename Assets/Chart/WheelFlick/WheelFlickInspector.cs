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

    // [SerializeField] Button leftButton;
    // [SerializeField] Button rightButton;

    // [SerializeField] Image leftButtonBorder;
    // [SerializeField] Image rightButtonBorder;

    // [SerializeField] Color leftButtonHighlight;
    // [SerializeField] Color rightButtonHighlight;

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
        // leftButton.onClick.AddListener(() =>
        // {
        //     Note.Dir = LeftRight.Left;
        //     leftButtonBorder.color = leftButtonHighlight;
        //     rightButtonBorder.color = Color.black;
        // });
        // rightButton.onClick.AddListener(() =>
        // {
        //     Note.Dir = LeftRight.Right;
        //     rightButtonBorder.color = rightButtonHighlight;
        //     leftButtonBorder.color = Color.black;
        // });
    }

    void OnEnable()
    {
        timeInput.text = $"{Note.StartTime}";

        if (Note.Dir == LeftRight.Left) leftToggle.SendEvent(notify: false, playAudio: false);
        else if (Note.Dir == LeftRight.Right) rightToggle.SendEvent(notify: false, playAudio: false);
    }

    protected override void Update()
    {
        base.Update();

        if (Note == null) return;

        if (!timeInput.isFocused) timeInput.SetTextWithoutNotify($"{Note.StartTime}");
    }
}
