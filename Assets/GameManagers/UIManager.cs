using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq;

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
    public delegate void DragCanceledEvent();
    public delegate void CursorModeChangedEvent(UICursorMode previousMode, UICursorMode newMode);
    
    public static event ElementSelectedEvent OnElementSelected;
    public static event DragCanceledEvent OnDragCanceled;
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
        set { Instance.clickedElement = value; }
    }

    object previousElement;
    private static object PreviousElement
    {
        get => Instance.previousElement;
        set {Instance.previousElement = value; }
    }

    object selectedElement;
    public static object SelectedElement
    {
        get => Instance.selectedElement;
        // no setter. SelectElement() should be used instead
        // so we can commit selection changes to edit history
    }

    bool isDragInProgress;
    public static bool IsDragInProgress
    {
        get => Instance.isDragInProgress;
        set { Instance.isDragInProgress = value; }
    }

    public static void SelectElement(object newElement, bool updateEditHistory=false)
    {
        PreviousElement = Instance.selectedElement;
        if (Instance.selectedElement != newElement)
        {
            Instance.selectedElement = newElement;
            OnElementSelected?.Invoke(PreviousElement, newElement);
            if (PreviousElement is TrackMarqueeSelectionGroup prevTrackMarqueeSelectionGroup)
            {
                Instance.FlagNotes(false, prevTrackMarqueeSelectionGroup.notes.ToList());
            }
            if (newElement is TrackMarqueeSelectionGroup newTrackMarqueeSelectionGroup)
            {
                SelectionGroup = newTrackMarqueeSelectionGroup;
                Instance.FlagNotes(true, newTrackMarqueeSelectionGroup.notes.ToList());
            }
            if (updateEditHistory)
            {
                SelectElementAction selectElementAction = new SelectElementAction(PreviousElement, newElement);
                selectElementAction.CommitAction();
            }
        }
    }

    TrackMarqueeSelectionGroup selectionGroup;
    public static TrackMarqueeSelectionGroup SelectionGroup
    {
        get => Instance.selectionGroup;
        set { Instance.selectionGroup = value; }
    }

    public static void MultiselectElements(bool addToSelection, SortedSet<Note> selectedNotes=null, bool modifyExistingSelection=false, bool createNewSelection=false, bool updateEditHistory=true)
    {
        // if notes are being removed from the selection, cache them
        SortedSet<Note> notesToFlag = selectedNotes == null ? SelectionGroup.notes : selectedNotes;
        if (!addToSelection) SelectionGroup.lastDeselectedNotes = notesToFlag;
        
        // if group is not selected prior to MultiselectElements(), or if not modifying existing multiselection,
        // make new selection
        bool overwriteCurrentSelection = !modifyExistingSelection || SelectedElement!=SelectionGroup;

        // create new list of selected notes
        SortedSet<Note> oldNotes = new SortedSet<Note>(SelectionGroup.notes);
        SortedSet<Note> newNotes = overwriteCurrentSelection ? new SortedSet<Note>() : new SortedSet<Note>(SelectionGroup.notes);
        if (selectedNotes != null && selectedNotes.Count > 0)
        {
            if (addToSelection)
            {
                newNotes.AddRange(selectedNotes);
            }
            else
            {
                // remove all selected notes except for sequences
                newNotes.RemoveWhere(note => selectedNotes.Contains(note) && !(note is EditableWheelSequence || note is EditableLeverSequence));

                // special case for removing sequences:
                // if no notes from these sequences are still selected, remove them
                foreach (Note note in selectedNotes.Intersect(newNotes))
                {
                    if (note is EditableWheelSequence editableWheelSequence && editableWheelSequence.Notes.Intersect(newNotes).Count() == 0)
                    { 
                        newNotes.Remove(editableWheelSequence);
                    }
                    else if (note is EditableLeverSequence editableLeverSequence && editableLeverSequence.Notes.Intersect(newNotes).Count() == 0)
                    {
                        newNotes.Remove(editableLeverSequence);
                    }
                }
            }
        }

        // selection/edit history management
        // only update history if the contents of selection changed, 
        // or if  selection is changing from note to selection group
        bool selectionChanged = !newNotes.All(note => oldNotes.Contains(note)) || newNotes.Count!=oldNotes.Count || SelectedElement!=SelectionGroup;
        if (selectionChanged)
        {
            // if we clear multiselection, treat it like a selection action
            if (newNotes.Count==0)
            {
                SelectElement(null, updateEditHistory);
            }
            else
            {
                // create new multiselection and select it with selection action
                if (createNewSelection || (overwriteCurrentSelection && updateEditHistory))
                {
                    SelectionGroup = new TrackMarqueeSelectionGroup(Instance.track);
                }
                // otherwise, if we modify existing multiselection, commit modify multiselect action
                else if (updateEditHistory)
                {
                    ModifyMultiselectionAction modifyMultiselectionAction = new ModifyMultiselectionAction(oldNotes, newNotes, SelectionGroup);
                    modifyMultiselectionAction.CommitAction();
                }
                // flag all notes that are now selected
                Instance.FlagNotes(false,oldNotes.Except(newNotes).ToList());
                Instance.FlagNotes(true, newNotes.Except(oldNotes).ToList());

                // set notes in selection group and select the group
                SelectionGroup.notes = newNotes;
                SelectElement(SelectionGroup, updateEditHistory);
            }
        }
    }

    public void FlagNotes(bool addToSelection, List<Note> notes)
    {
        if (notes!=null)
        {
            foreach(Note note in notes)
            {
                // get the object associated with each note
                if (track.noteGameObjectMapping.TryGetValue(note, out GameObject gameObject) && gameObject!=null)
                {
                    // flag all selected notes to change their click/drag/drop behavior
                    // only the clickable renderers will be added--no WheelSequences or LeverSequences here
                    switch (note)
                    {
                        case EditableButtonNote:
                            ButtonNoteRenderer buttonNoteRenderer = gameObject.GetComponent<ButtonNoteRenderer>();
                            buttonNoteRenderer.IsInMarqueeSelection = addToSelection;
                            break;
                        case EditableLeverNote:
                            LeverNoteRenderer leverNoteRenderer = gameObject.GetComponent<LeverNoteRenderer>();
                            leverNoteRenderer.IsInMarqueeSelection = addToSelection;
                            break;
                        case EditableWheelFlick:
                            WheelFlickRenderer wheelFlickRenderer = gameObject.GetComponent<WheelFlickRenderer>();
                            wheelFlickRenderer.IsInMarqueeSelection = addToSelection;
                            break;
                        case EditableWheelTurn:
                            WheelSequenceVertexRenderer wheelSequenceVertexRenderer = gameObject.GetComponent<WheelSequenceVertexRenderer>();
                            wheelSequenceVertexRenderer.IsInMarqueeSelection = addToSelection;
                            break; 
                        default:
                            break;
                    }
                }
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
            // if note renderer or env change is currently selected, check if is being dragged, and if so, do not log selection action,
            // just trigger their drag cancel behavior
            if (!isDragInProgress)
            {
                SelectElement(null, updateEditHistory:true);
            }
            // cancel drag
            else
            {
                SelectElement(PreviousElement, updateEditHistory:false);
                OnDragCanceled.Invoke();
                PlaybackManager.Instance.Pause();
            }
            CursorMode = UICursorMode.None;
        });

        ChartManager.OnChartChanged += DeselectOnChartChanged;
        // ChartManager.OnNoteAdded += SelectNoteOnNoteAdded;
        ChartManager.OnNoteRemoved += DeselectNoteOnNoteRemoved;
        // ChartManager.OnLeverNoteAdded += SelectNoteOnNoteAdded;
        // ChartManager.OnWheelTurnAdded += SelectNoteOnNoteAdded;
        // TODO: select the sequence when a lever note or wheel turn is removed
    }

    void OnDestroy()
    {
        ChartManager.OnChartChanged -= DeselectOnChartChanged;
        // ChartManager.OnNoteAdded -= SelectNoteOnNoteAdded;
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

    public void DeselectOnChartChanged(EditableChart prevChart, EditableChart newChart) => SelectElement(null, updateEditHistory:false);
    public void SelectNoteOnNoteAdded(Note note) => SelectElement(note, updateEditHistory:false);
    void DeselectNoteOnNoteRemoved(Note note) => SelectElement(null, updateEditHistory:false);
}
