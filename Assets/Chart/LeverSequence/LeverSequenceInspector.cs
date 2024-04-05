using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LeverSequenceInspector : NoteInspector<LeverSequence>
{
    [Header("Left Right Position")]
    [SerializeField] Button leftButton;
    [SerializeField] Button rightButton;

    [SerializeField] Image leftButtonBorder;
    [SerializeField] Image rightButtonBorder;

    [SerializeField] Color leftButtonHighlight;
    [SerializeField] Color rightButtonHighlight;

    [Header("Lever Notes")]
    [SerializeField] RectTransform leverNoteInspectors;
    [SerializeField] LeverNoteInspector leverNoteInspectorPrefab;
    [SerializeField] Button addNoteButton;

    protected override void Awake()
    {
        base.Awake();

        leftButton.onClick.AddListener(() =>
        {
            Note.Dir = LeftRight.Left;
            leftButtonBorder.color = leftButtonHighlight;
            rightButtonBorder.color = Color.black;
        });
        rightButton.onClick.AddListener(() =>
        {
            Note.Dir = LeftRight.Right;
            rightButtonBorder.color = rightButtonHighlight;
            leftButtonBorder.color = Color.black;
        });

        addNoteButton.onClick.AddListener(() =>
        {
            LeverNote lastNote = Note.LastNote;
            int time = lastNote.EndTime + SaveManager.Instance.ChartPackage.BeatToTime(1); // place 1 beat after last note
            // TODO: maybe place same distance as prev 2 notes?
            LeverPos pos = lastNote.Pos == LeverPos.Middle ? LeverPos.Left : LeverPos.Middle;
            try
            {
                Note.AddNote(new LeverNote(Note, time, pos));
            }
            catch (Exception e)
            {
                UIManager.Instance.ShowAlert("Exception While Adding Lever Note", $"Note may not have been added.\n{e.Message}");
                Debug.LogException(e);
            }
            
        });

        SaveManager.OnLeverNoteAdded += AddLeverNoteInspector;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        SaveManager.OnLeverNoteAdded -= AddLeverNoteInspector;
    }

    protected override void OnShow()
    {
        // Update left right
        if (Note.Dir == LeftRight.Left) leftButton.onClick.Invoke();
        else if (Note.Dir == LeftRight.Right) rightButton.onClick.Invoke();

        // Update note list
        foreach (var inspector in leverNoteInspectors.GetComponentsInChildren<LeverNoteInspector>())
        {
            DestroyImmediate(inspector.gameObject);
        }
        foreach (LeverNote note in Note.Notes)
        {
            AddLeverNoteInspector(Note, note);
        }
    }

    void AddLeverNoteInspector(LeverSequence sequence, LeverNote note)
    {
        if (sequence == Note)
        {
            var leverNoteInspector = Instantiate(leverNoteInspectorPrefab, leverNoteInspectors);
            leverNoteInspector.Note = note;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (Note == null) return;

        // TODO: optimize somehow?
        Dictionary<LeverNote, LeverNoteInspector> inspectors = leverNoteInspectors
            .GetComponentsInChildren<LeverNoteInspector>()
            .ToDictionary(i => i.Note, i => i);
        foreach (var (leverNote, i) in Note.Notes.Select((note, i) => (note, i)))
        {
            inspectors[leverNote].transform.SetSiblingIndex(i);
        }
    }
}
