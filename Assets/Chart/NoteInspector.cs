using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteInspector<NoteType> : MonoBehaviour where NoteType : Note
{
    public NoteType Note { get; protected set; }

    protected virtual void Awake()
    {
        gameObject.SetActive(false);

        UIManager.OnNoteSelected += ShowHide;
    }

    protected virtual void OnDestroy()
    {
        UIManager.OnNoteSelected -= ShowHide;
    }

    protected virtual void Update()
    {
        if (Note != null && !UIManager.IsSelectingInputField && 
            (Input.GetKeyUp(KeyCode.Backspace) || Input.GetKeyUp(KeyCode.Delete)))
        {
            SaveManager.Instance.Chart.RemoveNote(Note);
            ShowHide(null);
        }
    }

    protected virtual void OnShow() {}

    void ShowHide(Note note)
    {
        if (note is NoteType myNote)
        {
            Note = myNote;
            OnShow();
            gameObject.SetActive(true);
        }
        else
        {
            Note = null;
            gameObject.SetActive(false);
        }
    }
}
