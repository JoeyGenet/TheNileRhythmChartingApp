using UnityEngine;
using UnityEngine.UI;

public class NoteButtonsUIController : MonoBehaviour
{
    [SerializeField] Toggle buttonNoteToggle;
    [SerializeField] Toggle wheelFlickToggle;
    [SerializeField] Toggle wheelTurnToggle;
    [SerializeField] Toggle leverNoteToggle;
    [SerializeField] EventRelay noneEvent;

    void Awake()
    {
        void checkIfNone()
        {
            if (!buttonNoteToggle.isOn && !wheelFlickToggle.isOn && !wheelTurnToggle.isOn && !leverNoteToggle.isOn)
            {
                noneEvent.SendEvent();
            }
        }
        buttonNoteToggle.onValueChanged.AddListener(isOn => 
        {
            if (isOn) UIManager.CursorMode = UICursorMode.ButtonNote;
            else checkIfNone();
        });
        wheelFlickToggle.onValueChanged.AddListener(isOn => 
        {
            if (isOn) UIManager.CursorMode = UICursorMode.WheelFlick;
            else checkIfNone();
        });
        wheelTurnToggle.onValueChanged.AddListener(isOn => 
        {
            if (isOn) UIManager.CursorMode = UICursorMode.WheelTurn;
            else checkIfNone();
        });
        leverNoteToggle.onValueChanged.AddListener(isOn => 
        {
            if (isOn) UIManager.CursorMode = UICursorMode.LeverNote;
            else checkIfNone();
        });
        noneEvent.Event.AddListener(() => UIManager.CursorMode = UICursorMode.None);

        UIManager.OnCursorModeChanged += OnCursorModeChanged;
    }

    void OnDestroy()
    {
        UIManager.OnCursorModeChanged -= OnCursorModeChanged;
    }

    Toggle GetToggle(UICursorMode mode) => mode switch
    {
        UICursorMode.ButtonNote => buttonNoteToggle,
        UICursorMode.WheelFlick => wheelFlickToggle,
        UICursorMode.WheelTurn => wheelTurnToggle,
        UICursorMode.LeverNote => leverNoteToggle,
        UICursorMode.None => null,
        _ => throw new System.Exception($"Unsupported UI cursor mode {mode}"),
    };

    void OnCursorModeChanged(UICursorMode previousMode, UICursorMode newMode)
    {
        if (GetToggle(newMode) is Toggle onToggle)
        {
            if (!onToggle.isOn) onToggle.isOn = true;
        }
        else if (GetToggle(previousMode) is Toggle offToggle)
        {
            if (offToggle.isOn) offToggle.isOn = false;
        }
    }
}
