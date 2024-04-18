using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WheelTurnInspector : WheelSequenceInspector.ChildInspector
{
    [SerializeField] TMP_InputField deltaAngleInput;
    [SerializeField] TMP_InputField endTimeInput;
    // [SerializeField] Button deleteButton;
    
    protected override void Awake()
    {
        base.Awake();
        
        deltaAngleInput.onValueChanged.AddListener(text =>
        {
            if (int.TryParse(text, out int deltaAngle))
            {
                Note.DeltaAngle = Mathf.Clamp(deltaAngle, -3600, 3600); // Clamp to prevent too many lines
            }
        });

        endTimeInput.onValidateInput += (text, _, c) => c == '-' ? '\0' : c;
        endTimeInput.onValueChanged.AddListener(text =>
        {
            if (int.TryParse(text, out int endTime))
            {
                if (endTime > Note.Container.StartTime)
                {
                    Note.EndTime = Mathf.Clamp(endTime, 0, int.MaxValue); // TODO: clamp to song length
                }
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
