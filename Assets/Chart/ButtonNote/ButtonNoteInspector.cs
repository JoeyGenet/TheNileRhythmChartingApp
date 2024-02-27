using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonNoteInspector : MonoBehaviour
{
    [SerializeField] TMP_InputField timeInput;
    [SerializeField] TMP_InputField leftPosInput;
    [SerializeField] TMP_InputField rightPosInput;

    public ButtonNote Note { get; private set; }

    void Awake()
    {
        gameObject.SetActive(false);

        timeInput.onValidateInput += (text, _, c) => c == '-' ? '\0' : c;
        timeInput.onValueChanged.AddListener(text =>
        {
            if (int.TryParse(text, out int time))
            {
                Note.StartTime = Mathf.Clamp(time, 0, int.MaxValue); // TODO: clamp to song length
            }
        });

        leftPosInput.onValidateInput += (text, _, c) => c == '-' ? '\0' : c;
        leftPosInput.onValueChanged.AddListener(text =>
        {
            if (int.TryParse(text, out int pos))
            {
                Note.LeftPos = Mathf.Clamp(pos, 0, 100);
                
            }
        });
        leftPosInput.onEndEdit.AddListener(_ =>
        {
            if (Note.RightPos is int rightPos) Note.RightPos = Mathf.Clamp(rightPos, Note.LeftPos, 100);
        });

        rightPosInput.onValidateInput += (text, _, c) => c == '-' ? '\0' : c;
        rightPosInput.onValueChanged.AddListener(text =>
        {
            if (text == "") Note.RightPos = null;
            else if (int.TryParse(text, out int pos))
            {
                Note.RightPos = Mathf.Clamp(pos, 0, 100);
            }
        });
        rightPosInput.onEndEdit.AddListener(_ =>
        {
            if (Note.RightPos is int rightPos) Note.LeftPos = Mathf.Clamp(Note.LeftPos, 0, rightPos);
        });
        
        UIManager.OnNoteSelected += ShowHide;
    }

    void OnDestroy()
    {
        UIManager.OnNoteSelected -= ShowHide;
    }

    void ShowHide(Note note)
    {
        if (note is ButtonNote buttonNote)
        {
            Note = buttonNote;
            gameObject.SetActive(true);
        }
        else
        {
            Note = null;
            gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!timeInput.isFocused) timeInput.SetTextWithoutNotify($"{Note.StartTime}");
        if (!leftPosInput.isFocused) leftPosInput.SetTextWithoutNotify($"{Note.LeftPos}");
        if (!rightPosInput.isFocused) rightPosInput.SetTextWithoutNotify($"{Note.RightPos}");

        if ((Input.GetKeyUp(KeyCode.Backspace) || Input.GetKeyUp(KeyCode.Delete)) && Note != null)
        {
            SaveManager.Instance.Chart.RemoveNote(Note);
            ShowHide(null);
        }
    }
}
