using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Tells what the cursor does in the UI
public enum UICursorMode
{
    None = 0,
    ButtonNote = 1,
    WheelFlick = 2,
    WheelTurn = 3,
    LeverNote = 4,
}

public class UIManager : MonoBehaviour
{
    public delegate void NoteSelectedEvent(Note previousNote, Note newNote);
    public delegate void CursorModeChangedEvent(UICursorMode previousMode, UICursorMode newMode);
    
    public static event NoteSelectedEvent OnNoteSelected;
    public static event CursorModeChangedEvent OnCursorModeChanged;

    public static UIManager Instance;

    [SerializeField] EventRelay deleteSelectedNoteEvent;
    [SerializeField] EventRelay deselectEvent;

    [Header("Alert Dialog")]
    [SerializeField] AlertDialog alertDialogPrefab;
    [SerializeField] AlertDialog confirmDialogPrefab;

    Note selectedNote;
    public static Note SelectedNote
    {
        get => Instance.selectedNote;
        set
        {
            if (Instance.selectedNote != value)
            {
                Note prevNote = Instance.selectedNote;
                Instance.selectedNote = value;
                OnNoteSelected?.Invoke(prevNote, value);
            }
        }
    }

    UICursorMode cursorMode;
    public static UICursorMode CursorMode
    {
        get => Instance.cursorMode;
        set
        {
            if (Instance.cursorMode != value)
            {
                var prevMode = Instance.cursorMode;
                Instance.cursorMode = value;
                OnCursorModeChanged?.Invoke(prevMode, value);
            }
        }
    }

    public static bool IsSelectingInputField
    {
        get
        {
            GameObject selectedObj = EventSystem.current.currentSelectedGameObject;
            return selectedObj != null && 
                (selectedObj.GetComponent<InputField>() != null || selectedObj.GetComponent<TMP_InputField>() != null);
        }
    }

    void Awake()
    {
        Instance = this;


        // TODO: in the future may want to move this into NoteInspector, 
        // and add a button like with NoteContainerInspector.ChildInspector
        deleteSelectedNoteEvent.AddListener(() =>
        {
            if (selectedNote is IEditableNote<EditableChart, Note> editableNote)
            {
                editableNote.RemoveSelf();
            }
            else if (selectedNote != null)
            {
                Debug.LogWarning($"Deleting of note {selectedNote} not supported");
            }
        });

        deselectEvent.AddListener(() =>
        {
            SelectedNote = null;
            CursorMode = UICursorMode.None;
        });

        ChartManager.OnChartChanged += DeselectOnChartChanged;
        ChartManager.OnNoteAdded += SelectNoteOnNoteAdded;
        ChartManager.OnNoteRemoved += DeselectNoteOnNoteRemoved;
        // ChartManager.OnLeverNoteAdded += SelectNoteOnNoteAdded;
        // ChartManager.OnWheelTurnAdded += SelectNoteOnNoteAdded;
        // TODO: select the sequence when a lever note or wheel turn is removed
    }

    void OnDestroy()
    {
        ChartManager.OnChartChanged -= DeselectOnChartChanged;
        ChartManager.OnNoteAdded -= SelectNoteOnNoteAdded;
        ChartManager.OnNoteRemoved -= DeselectNoteOnNoteRemoved;
        // ChartManager.OnLeverNoteAdded -= SelectNoteOnNoteAdded;
        // ChartManager.OnWheelTurnAdded -= SelectNoteOnNoteAdded;
    }

    public AlertDialog ShowAlert(string title, string message)
    {
        var alert = Instantiate(alertDialogPrefab, transform);
        alert.Title = title;
        alert.Message = message;
        return alert;
    }

    public AlertDialog ShowConfirm(string title, string message)
    {
        var alert = Instantiate(confirmDialogPrefab, transform);
        alert.Title = title;
        alert.Message = message;
        return alert;
    }

    public void DeselectOnChartChanged(EditableChart prevChart, EditableChart newChart) => SelectedNote = null;// SelectNote(null);
    public void SelectNoteOnNoteAdded(Note note) => SelectedNote = note;// SelectNote(note);
    void DeselectNoteOnNoteRemoved(Note note) => SelectedNote = null;// SelectNote(null);
}
