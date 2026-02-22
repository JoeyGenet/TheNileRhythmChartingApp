using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectionGroupInspector : NoteInspector<EditableWheelFlick>
{
    // TODO: Update title of inspector to reflect number of notes selected
    [SerializeField] TextMeshProUGUI inspectorTitle;
    // [SerializeField] TMP_InputField timeInput;
    // private int editStartTime;
    // [SerializeField] EventRelay mirrorToggle;
    [SerializeField] EventRelay deleteToggle;
    [SerializeField] EventRelay mirrorToggle;
    private bool isEditInProgress;

    public void Show()
    {
        gameObject.SetActive(UIManager.SelectionGroup.notes.Count > 0);
        int displayedNoteCount = 0;
        // only count individual turns or lever notes, not the sequence as a whole
        foreach (Note note in UIManager.SelectionGroup.notes)
        {
            if (note is not EditableWheelSequence && note is not EditableLeverSequence)
            {
                displayedNoteCount++;
            }
        }
        inspectorTitle.text = $"{displayedNoteCount} {(displayedNoteCount>1 ? "Notes" : "Note")} Selected";
    }

    void Awake()
    {
        // TODO: allow users to change time of selected notes via inspector
        // timeInput.onValidateInput += (text, _, c) => c == '-' ? '\0' : c;
        // timeInput.onValueChanged.AddListener(text =>
        // {
        //     // track starting value of edit
        //     if (!isEditInProgress)
        //     {
        //         editStartTime = Note.StartTime;
        //         isEditInProgress = true;
        //     }
        //     // process new value
        //     if (int.TryParse(text, out int time) && time >= 0)
        //     {
        //         Note.StartTime = time;
        //     }
        // });
        // timeInput.onEndEdit.AddListener(text =>
        // {
        //     if (isEditInProgress)
        //     {
        //         int startTimeDelta = Note.StartTime - editStartTime;
        //         EditWheelFlickAction editWheelFlickAction = new EditWheelFlickAction(Note, startTimeDelta, 0);
        //         editWheelFlickAction.CommitAction();
        //         isEditInProgress = false;
        //     }
        // });

        // TODO: allow users to mirror selected notes across the center of the track
        // mirrorToggle.Event.AddListener(() => 
        // {
        //     int dirDelta = LeftRight.Left - Note.Dir;
        //     if (dirDelta != 0)
        //     {
        //         EditWheelFlickAction editWheelFlickAction = new EditWheelFlickAction(Note, 0, dirDelta);
        //         editWheelFlickAction.CommitAction();
        //     }
        //     Note.Dir = LeftRight.Left;
        // });

        deleteToggle.Event.AddListener(() => 
        {
            UIManager.SelectionGroup.CommitMultiselectDeleteAction();
        });

        mirrorToggle.Event.AddListener(() =>
        {
            UIManager.SelectionGroup.MirrorSelectedNotes();
        });
    }

    void Update()
    {
        if (Note == null) return;

        // if (!timeInput.isFocused) timeInput.SetTextWithoutNotify($"{Note.StartTime}");
    }
}
