using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WheelFlickInspector : NoteInspector<EditableWheelFlick>
{
    [SerializeField] TMP_InputField timeInput;
    [SerializeField] Button leftButton;
    [SerializeField] Button rightButton;

    [SerializeField] Image leftButtonBorder;
    [SerializeField] Image rightButtonBorder;

    [SerializeField] Color leftButtonHighlight;
    [SerializeField] Color rightButtonHighlight;

    protected override void Awake()
    {
        base.Awake();

        timeInput.onValidateInput += (text, _, c) => c == '-' ? '\0' : c;
        timeInput.onValueChanged.AddListener(text =>
        {
            if (int.TryParse(text, out int time) && time >= 0)
            {
                Note.StartTime = time;
            }
        });
        leftButton.onClick.AddListener(() =>
        {
            Note.Dir = LeftRight.Left;
            leftButtonBorder.color = leftButtonHighlight;
            rightButtonBorder.color = Color.black;
        });
        rightButton.onClick.AddListener(() =>
        {
            Note.Dir = LeftRight.Right;
            rightButtonBorder.color = rightButtonHighlight;
            leftButtonBorder.color = Color.black;
        });
    }

    protected override void OnShow()
    {
        timeInput.text = $"{Note.StartTime}";

        if (Note.Dir == LeftRight.Left) leftButton.onClick.Invoke();
        else if (Note.Dir == LeftRight.Right) rightButton.onClick.Invoke();
    }

    protected override void Update()
    {
        base.Update();

        if (Note == null) return;

        if (!timeInput.isFocused) timeInput.SetTextWithoutNotify($"{Note.StartTime}");
    }
}
