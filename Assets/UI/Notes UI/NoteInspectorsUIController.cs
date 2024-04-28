using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NoteInspectorsUIController : MonoBehaviour
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

        UIManager.OnElementSelected += OnNoteSelected;
    }

    void OnDestroy()
    {
        UIManager.OnElementSelected -= OnNoteSelected;
    }

    void OnNoteSelected(object prevNote, object note)
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
            inspector.gameObject.SetActive(false);
            noNoteSelectedView.SetActive(true);
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
                default:
                    noNoteSelectedView.SetActive(false);
                    break;
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
                default:
                    noNoteSelectedView.SetActive(true);
                    break;
            };
        }
        catch (Exception e)
        {
            UIManager.Instance.ShowAlert("Exception While Showing Inspector", $"Tried to show inspector for {note}\n{e.Message}");
            Debug.LogException(e);
        }
        
    }
}
