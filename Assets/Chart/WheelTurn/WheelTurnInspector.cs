using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WheelTurnInspector : MonoBehaviour
{
    public WheelTurn Note;

    [SerializeField] TMP_InputField deltaAngleInput;
    [SerializeField] TMP_InputField endTimeInput;
    [SerializeField] Button deleteButton;
    
    void Awake()
    {
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

        deleteButton.onClick.AddListener(() =>
        {
            Note.Container.RemoveNote(Note);
        });

        WheelSequence.OnWheelTurnRemoved += RemoveNote;
    }

    void OnDestroy()
    {
        WheelSequence.OnWheelTurnRemoved -= RemoveNote;
    }

    void Update()
    {
        if (!deltaAngleInput.isFocused) deltaAngleInput.SetTextWithoutNotify($"{Note.DeltaAngle}");
        if (!endTimeInput.isFocused) endTimeInput.SetTextWithoutNotify($"{Note.EndTime}");
    }

    void RemoveNote(WheelSequence sequence, WheelTurn note)
    {
        if (note == Note)
        {
            Destroy(gameObject);
        }
    }
}
