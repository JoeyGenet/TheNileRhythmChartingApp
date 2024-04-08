using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LeverSequenceInspector : NoteContainerInspector<EditableLeverSequence, EditableLeverNote>
{
    [Header("Left Right Position")]
    [SerializeField] EventRelay leftToggle;
    [SerializeField] EventRelay rightToggle;

    // [SerializeField] Button leftButton;
    // [SerializeField] Button rightButton;

    // [SerializeField] Image leftButtonBorder;
    // [SerializeField] Image rightButtonBorder;

    // [SerializeField] Color leftButtonHighlight;
    // [SerializeField] Color rightButtonHighlight;

    // [Header("Lever Notes")]
    // [SerializeField] RectTransform leverNoteInspectorsParent;
    // [SerializeField] LeverNoteInspector leverNoteInspectorPrefab;
    // [SerializeField] Button addNoteButton;

    protected override void Awake()
    {
        base.Awake();

        leftToggle.Event.AddListener(() => Note.Dir = LeftRight.Left);
        rightToggle.Event.AddListener(() => Note.Dir = LeftRight.Right);

        // leftButton.onClick.AddListener(() =>
        // {
        //     Note.Dir = LeftRight.Left;
        //     leftButtonBorder.color = leftButtonHighlight;
        //     rightButtonBorder.color = Color.black;
        // });
        // rightButton.onClick.AddListener(() =>
        // {
        //     Note.Dir = LeftRight.Right;
        //     rightButtonBorder.color = rightButtonHighlight;
        //     leftButtonBorder.color = Color.black;
        // });
    }

    void OnEnable()
    {
        // Update left right
        if (Note.Dir == LeftRight.Left) leftToggle.SendEvent(notify: false, playAudio: false);
        else if (Note.Dir == LeftRight.Right) rightToggle.SendEvent(notify: false, playAudio: false);
        // if (Note.Dir == LeftRight.Left) leftButton.onClick.Invoke();
        // else if (Note.Dir == LeftRight.Right) rightButton.onClick.Invoke();
    }

    protected override EditableLeverNote NewChildNote()
    {
        LeverNote lastNote = Note.LastNote;
        int time = lastNote.EndTime + ChartManager.ChartPackage.BeatToTime(1); // place 1 beat after last note
        // TODO: maybe place same distance as prev 2 notes?
        LeverPos pos = lastNote.Pos == LeverPos.Middle ? LeverPos.Left : LeverPos.Middle;
        return new EditableLeverNote(Note, time, pos);
    }
}
