using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

// Tells what the cursor does in the UI
public enum UICursorMode
{
    None,
    ButtonNote,
    WheelFlick,
    WheelTurn,
    LeverNote,
    EnvironmentChange,
}

public class UIManager : MonoBehaviour
{
    public delegate void ElementSelectedEvent(object previousElement, object newElement);
    public delegate void CursorModeChangedEvent(UICursorMode previousMode, UICursorMode newMode);
    
    public static event ElementSelectedEvent OnElementSelected;
    public static event CursorModeChangedEvent OnCursorModeChanged;

    public static UIManager Instance;

    [SerializeField] EventRelay deleteSelectedNoteEvent;
    [SerializeField] EventRelay deselectEvent;

    [Header("Alert Dialog")]
    [SerializeField] AlertDialog alertDialogPrefab;
    [SerializeField] AlertDialog confirmDialogPrefab;

    [Header("Background")]
    [SerializeField] Image backgroundImage;
    [SerializeField] Color previewEnvironmentColor;
    Color backgroundColor;

    [Header("Track UI")]
    [SerializeField] Track track;
    public static Track Track
    {
        get => Instance.track;
        set { Instance.track = value; }
    }

    object clickedElement;
    public static object ClickedElement
    {
        get => Instance.clickedElement;
        set { Instance.clickedElement = value ;}
    }

    object selectedElement;
    public static object SelectedElement
    {
        get => Instance.selectedElement;
        set
        {
            if (Instance.selectedElement != value)
            {
                object prevElement = Instance.selectedElement;
                Instance.selectedElement = value;
                OnElementSelected?.Invoke(prevElement, value);
            }
        }
    }

    TrackMarqueeSelectionGroup selectionGroup;
    public static TrackMarqueeSelectionGroup SelectionGroup
    {
        get => Instance.selectionGroup;
        set { Instance.selectionGroup = value; }
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

    static int snapping = 1; // Snap to 1/n beats. if 0, no snapping
    public static int Snapping
    {
        get => (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) ? 0 : snapping;
        set => snapping = value;
    }

    static bool previewEnvironment = true;
    public static bool PreviewEnvironment
    {
        get => previewEnvironment;
        set
        {
            previewEnvironment = value;
            if (previewEnvironment) Instance.backgroundImage.color = Instance.previewEnvironmentColor;
            else Instance.backgroundImage.color = Instance.backgroundColor;
        }
    }

    public static int SnapTime(int time)
    {
        if (Snapping > 0)
        {
            float beat = ChartManager.ChartPackage.TimeToBeat(time);
            beat = Mathf.Round(beat * Snapping) / Snapping;
            return ChartManager.ChartPackage.BeatToTime(beat);
        }
        return time;
    }

    void Awake()
    {
        Instance = this;

        backgroundColor = backgroundImage.color;
        PreviewEnvironment = previewEnvironment;
        selectionGroup = new TrackMarqueeSelectionGroup(track);

        // TODO: in the future may want to move this into NoteInspector, 
        // and add a button like with NoteContainerInspector.ChildInspector
        deleteSelectedNoteEvent.AddListener(() =>
        {
            if (SelectedElement is IEditableNote<EditableChart, Note> editableNote)
            {
                editableNote.RemoveSelf();
            }
            else if (SelectedElement == SelectionGroup)
            {
                SelectionGroup.CommitMultiselectDeleteAction();
            }
            else if (SelectedElement != null)
            {
                Debug.LogWarning($"Deleting of note {SelectedElement} not supported");
            }
        });

        deselectEvent.AddListener(() =>
        {
            SelectedElement = null;
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

    public void DeselectOnChartChanged(EditableChart prevChart, EditableChart newChart) => SelectedElement = null;// SelectNote(null);
    public void SelectNoteOnNoteAdded(Note note) => SelectedElement = note;// SelectNote(note);
    void DeselectNoteOnNoteRemoved(Note note) => SelectedElement = null;// SelectNote(null);
}
