using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WheelFlickInspector : MonoBehaviour
{
    [SerializeField] TMP_InputField timeInput;
    [SerializeField] Button leftButton;
    [SerializeField] Button rightButton;

    [SerializeField] Image leftButtonBorder;
    [SerializeField] Image rightButtonBorder;

    [SerializeField] Color leftButtonHighlight;
    [SerializeField] Color rightButtonHighlight;

    public WheelFlick Note { get; private set; }

    void Awake()
    {
        gameObject.SetActive(false);

        timeInput.onValidateInput += (text, _, c) => c == '-' ? '\0' : c;
        timeInput.onValueChanged.AddListener(text =>
        {
            if (int.TryParse(text, out int time) && time >= 0)
            {
                Note.StartTime = time;
            }
            else
            {
                timeInput.SetTextWithoutNotify($"{Note.StartTime}");
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
        
        UIManager.OnNoteSelected += ShowHide;
    }

    void OnDestroy()
    {
        UIManager.OnNoteSelected -= ShowHide;
    }

    void ShowHide(Note note)
    {
        if (note is WheelFlick flick)
        {
            Note = flick;
            timeInput.text = $"{Note.StartTime}";
            if (Note.Dir == LeftRight.Left) leftButton.onClick.Invoke();
            else if (Note.Dir == LeftRight.Right) rightButton.onClick.Invoke();
            gameObject.SetActive(true);
        }
        else
        {
            Note = null;
            gameObject.SetActive(false);
        }
    }
}
