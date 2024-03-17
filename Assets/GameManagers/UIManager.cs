using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    public delegate void NoteSelectedEvent(Note note);
    
    public static event NoteSelectedEvent OnNoteSelected;

    public static UIManager Instance;

    [Header("Alert Dialog")]
    [SerializeField] AlertDialog alertDialogPrefab;

    public static bool IsSelectingInputField
    {
        get
        {
            GameObject selectedObj = EventSystem.current.currentSelectedGameObject;
            return selectedObj != null && 
                (selectedObj.GetComponent<InputField>() != null || selectedObj.GetComponent<TMP_InputField>());
        }
    }

    void Awake()
    {
        Instance = this;

        SaveManager.OnChartLoaded += DeselectOnChartLoaded;
        Chart.OnNoteAdded += SelectNote;
        Chart.OnNoteRemoved += DeselectNoteOnNoteRemoved;
        LeverSequence.OnLeverNoteAdded += SelectNoteOnLeverNoteAdded;
        WheelSequence.OnWheelTurnAdded += SelectNoteOnWheelTurnAdded;
    }

    void OnDestroy()
    {
        SaveManager.OnChartLoaded -= DeselectOnChartLoaded;
        Chart.OnNoteAdded -= SelectNote;
        Chart.OnNoteRemoved -= DeselectNoteOnNoteRemoved;
        LeverSequence.OnLeverNoteAdded -= SelectNoteOnLeverNoteAdded;
        WheelSequence.OnWheelTurnAdded -= SelectNoteOnWheelTurnAdded;
    }

    public AlertDialog ShowAlert(string title, string message)
    {
        var alert = Instantiate(alertDialogPrefab, transform);
        alert.Title = title;
        alert.Message = message;
        return alert;
    }

    public void DeselectOnChartLoaded(string chart) => SelectNote(null);

    public void SelectNote(Note note) => OnNoteSelected?.Invoke(note);

    void DeselectNoteOnNoteRemoved(Note _) => SelectNote(null);
    void SelectNoteOnLeverNoteAdded(LeverSequence seq, LeverNote _) => SelectNote(seq);
    void SelectNoteOnWheelTurnAdded(WheelSequence seq, WheelTurn _) => SelectNote(seq);
}
