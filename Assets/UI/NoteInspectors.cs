using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NoteInspectors : MonoBehaviour
{
    [SerializeField] ButtonNoteInspector buttonNoteInspector;
    [SerializeField] WheelFlickInspector wheelFlickInspector;
    [SerializeField] WheelSequenceInspector wheelSequenceInspector;
    [SerializeField] LeverSequenceInspector leverSequenceInspector;
    [SerializeField] GameObject noNoteSelectedView;

    void Awake()
    {
        buttonNoteInspector.gameObject.SetActive(false);
        wheelFlickInspector.gameObject.SetActive(false);
        wheelSequenceInspector.gameObject.SetActive(false);
        leverSequenceInspector.gameObject.SetActive(false);
        noNoteSelectedView.SetActive(true);

        UIManager.OnNoteSelected += OnNoteSelected;
    }

    void OnDestroy()
    {
        UIManager.OnNoteSelected -= OnNoteSelected;
    }

    void OnNoteSelected(Note prevNote, Note note)
    {
        void show<NoteType>(NoteInspector<NoteType> inspector, NoteType note)
        where NoteType : Note, IEditableNote<EditableChart, Note>
        {
            inspector.Show(note);
            inspector.gameObject.SetActive(true);
            noNoteSelectedView.SetActive(false);
        }
        void hide<NoteType>(NoteInspector<NoteType> inspector)
        where NoteType : Note, IEditableNote<EditableChart, Note>
        {
            inspector.Show(null);
            inspector.gameObject.SetActive(true);
        }


        try
        {
            switch (prevNote)
            {
                case EditableButtonNote buttonNote:
                    hide(buttonNoteInspector);
                    break;
                case EditableWheelFlick wheelFlick:
                    hide(wheelFlickInspector);
                    break;
                case EditableWheelSequence wheelSequence:
                    hide(wheelSequenceInspector);
                    break;
                case EditableLeverSequence leverSequence:
                    hide(leverSequenceInspector);
                    break;
                case null:
                    noNoteSelectedView.SetActive(false);
                    break;
                default:
                    throw new Exception($"Unsupported note ${note} for showing an inspector");
            };
            switch (note)
            {
                case EditableButtonNote buttonNote:
                    show(buttonNoteInspector, buttonNote);
                    break;
                case EditableWheelFlick wheelFlick:
                    show(wheelFlickInspector, wheelFlick);
                    break;
                case EditableWheelSequence wheelSequence:
                    show(wheelSequenceInspector, wheelSequence);
                    break;
                case EditableLeverSequence leverSequence:
                    show(leverSequenceInspector, leverSequence);
                    break;
                case null:
                    noNoteSelectedView.SetActive(true);
                    break;
                default:
                    throw new Exception($"Unsupported note ${note} for showing an inspector");
            };
        }
        catch (Exception e)
        {
            UIManager.Instance.ShowAlert("Exception While Showing Inspector", $"Tried to show inspector for {note}\n{e.Message}");
            Debug.LogException(e);
        }
        
    }
}
