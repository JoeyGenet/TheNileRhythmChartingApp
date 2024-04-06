using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeverNoteInspector : NoteContainerInspector<EditableLeverSequence, EditableLeverNote>.ChildInspector
{
    [SerializeField] TMP_InputField timeInput;
    [SerializeField] TMP_Dropdown posDropdown;
    [SerializeField] Button deleteButton;

    [Header("Colors")]
    [SerializeField] private Color leftColor;
    [SerializeField] private Color upColor;
    [SerializeField] private Color rightColor;
    [SerializeField] private Color downColor;
    [SerializeField] private Color middleColor;
    
    void Awake()
    {
        timeInput.onValidateInput += (text, _, c) => c == '-' ? '\0' : c;
        timeInput.onValueChanged.AddListener(text =>
        {
            if (int.TryParse(text, out int time))
            {
                Note.StartTime = Mathf.Clamp(time, 0, int.MaxValue); // TODO: clamp to song length
            }
        });

        posDropdown.onValueChanged.AddListener(value =>
        {
            Note.Pos = (LeverPos) value;
        });

        deleteButton.onClick.AddListener(() =>
        {
            var note = Note;
            try
            {
                Note.Container.RemoveNote(Note);
            }
            catch (Exception e)
            {
                UIManager.Instance.ShowAlert("Exception While Deleting Lever Note", $"Note {note} may not have been deleted.\n{e.Message}");
                Debug.LogException(e);
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
