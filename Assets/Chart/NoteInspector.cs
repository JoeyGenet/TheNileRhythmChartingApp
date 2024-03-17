using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteInspector<NoteType> : MonoBehaviour where NoteType : Note
{
    public NoteType Note;

    protected virtual void Awake()
    {
        gameObject.SetActive(false);

        UIManager.OnNoteSelected += ShowHide;
        // Chart.OnNoteRemoved += 
    }

    protected virtual void OnDestroy()
    {
        UIManager.OnNoteSelected -= ShowHide;
    }

    // TODO: create UpdateOnNoteChanged that is called when the note changes or note's value changes
    protected virtual void Update()
    {
        if (Note != null && !UIManager.IsSelectingInputField && 
            (Input.GetKeyUp(KeyCode.Backspace) || Input.GetKeyUp(KeyCode.Delete)))
        {
            var note = Note;
            try
            {
                SaveManager.Instance.Chart.RemoveNote(Note);
                ShowHide(null);
            }
            catch (Exception e)
            {
                UIManager.Instance.ShowAlert("Exception While Deleting Note", $"Note {note} may not have been deleted.\n{e.Message}");
                Debug.LogException(e);
            }
        }
    }

    protected virtual void OnShow() {}

    void ShowHide(Note note)
    {
        if (note is NoteType myNote)
        {
            if (Note == myNote) return; // Inspector is already showing this note. don't show again
            Note = myNote;
            try
            {
                OnShow();
                gameObject.SetActive(true);
            }
            catch (Exception e)
            {
                Note = null;
                UIManager.Instance.ShowAlert("Exception While Showing Inspector", $"Tried to show inspector for {myNote}\n{e.Message}");
                Debug.LogException(e);
            }
        }
        else
        {
            gameObject.SetActive(false);
            Note = null;
        }
    }
}
