using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InspectorsUIController : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] GameObject noteInspectorHeader;
    [SerializeField] GameObject eventInspectorHeader;

    [Header("Inspectors")]
    [SerializeField] ButtonNoteInspector buttonNoteInspector;
    [SerializeField] WheelFlickInspector wheelFlickInspector;
    [SerializeField] WheelSequenceInspector wheelSequenceInspector;
    [SerializeField] LeverSequenceInspector leverSequenceInspector;
    [SerializeField] SelectionGroupInspector selectionGroupInspector;
    [SerializeField] EnvironmentChangeInspector environmentChangeInspector;
    [SerializeField] BpmChangeInspector bpmChangeInspector;
    [SerializeField] GameObject noElementSelectedView;

    void Awake()
    {
        noteInspectorHeader.SetActive(true);
        eventInspectorHeader.SetActive(false);

        buttonNoteInspector.gameObject.SetActive(false);
        wheelFlickInspector.gameObject.SetActive(false);
        wheelSequenceInspector.gameObject.SetActive(false);
        leverSequenceInspector.gameObject.SetActive(false);
        selectionGroupInspector.gameObject.SetActive(false);
        environmentChangeInspector.gameObject.SetActive(false);
        bpmChangeInspector.gameObject.SetActive(false);
        noElementSelectedView.SetActive(true);

        UIManager.OnElementSelected += OnElementSelected;
    }

    void OnDestroy()
    {
        UIManager.OnElementSelected -= OnElementSelected;
    }

    void OnElementSelected(object prevElement, object element)
    {
        void show<NoteType>(NoteInspector<NoteType> inspector, NoteType note)
        where NoteType : Note, IEditableNote<EditableChart, Note>
        {
            inspector.Show(note);
            inspector.gameObject.SetActive(true);
            noElementSelectedView.SetActive(false);
            noteInspectorHeader.SetActive(true);
            eventInspectorHeader.SetActive(false);
        }
        void hide<NoteType>(NoteInspector<NoteType> inspector)
        where NoteType : Note, IEditableNote<EditableChart, Note>
        {
            inspector.Show(null);
            inspector.gameObject.SetActive(false);
            noElementSelectedView.SetActive(true);
            noteInspectorHeader.SetActive(true);
            eventInspectorHeader.SetActive(false);
        }


        try
        {
            switch (prevElement)
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
                case TrackMarqueeSelectionGroup trackMarqueeSelectionGroup:
                    // hide(selectionGroupInspector);
                    selectionGroupInspector.Show(null);
                    selectionGroupInspector.gameObject.SetActive(false);
                    noElementSelectedView.SetActive(true);
                    break;
                case EditableEnvironmentChangeEvent environmentChange:
                    environmentChangeInspector.Show(null);
                    environmentChangeInspector.gameObject.SetActive(false);
                    noElementSelectedView.SetActive(true);
                    break;
                case EditableBpmChangeEvent bpmChange:
                    bpmChangeInspector.Show(null);
                    bpmChangeInspector.gameObject.SetActive(false);
                    noElementSelectedView.SetActive(true);
                    break;
                case null:
                    noElementSelectedView.SetActive(false);
                    break;
                default:
                    throw new Exception($"Unsupported element ${element} for showing an inspector");
            };
            switch (element)
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
                case TrackMarqueeSelectionGroup trackMarqueeSelectionGroup:
                    selectionGroupInspector.Show();
                    selectionGroupInspector.gameObject.SetActive(true);
                    noElementSelectedView.SetActive(false);
                    noteInspectorHeader.SetActive(true);
                    eventInspectorHeader.SetActive(false);
                    break;
                case EditableEnvironmentChangeEvent environmentChange:
                    environmentChangeInspector.Show(environmentChange);
                    environmentChangeInspector.gameObject.SetActive(true);
                    noElementSelectedView.SetActive(false);
                    noteInspectorHeader.SetActive(false);
                    eventInspectorHeader.SetActive(true);
                    break;
                case EditableBpmChangeEvent bpmChange:
                    bpmChangeInspector.Show(bpmChange);
                    bpmChangeInspector.gameObject.SetActive(true);
                    noElementSelectedView.SetActive(false);
                    noteInspectorHeader.SetActive(false);
                    eventInspectorHeader.SetActive(true);
                    break;
                case null:
                    noElementSelectedView.SetActive(true);
                    noteInspectorHeader.SetActive(true);
                    eventInspectorHeader.SetActive(false);
                    break;
                default:
                    throw new Exception($"Unsupported element ${element} for showing an inspector");
            };
        }
        catch (Exception e)
        {
            UIManager.Instance.ShowAlert("Exception While Showing Inspector", $"Tried to show inspector for {element}\n{e.Message}");
            Debug.LogException(e);
        }
        
    }
}
