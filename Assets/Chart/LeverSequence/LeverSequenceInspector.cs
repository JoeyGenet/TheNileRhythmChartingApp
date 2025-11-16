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

    protected override void Awake()
    {
        base.Awake();

        leftToggle.Event.AddListener(() => 
        {
            int dirDelta = LeftRight.Left - Note.Dir;
            if (dirDelta != 0)
            {
                Dictionary<EditableLeverNote, int> startTimes = Note.Notes.ToDictionary(n => n, n => n.StartTime);
                Dictionary<EditableLeverNote, LeverPos> leverPositions = Note.Notes.ToDictionary(n => n, n => n.Pos);
                EditLeverSequenceAction editLeverSequenceAction = new EditLeverSequenceAction(Note, dirDelta, startTimes, startTimes, leverPositions, leverPositions);
                editLeverSequenceAction.CommitAction();
            }
            Note.Dir = LeftRight.Left;
        });

        rightToggle.Event.AddListener(() => 
        {
            int dirDelta = LeftRight.Right - Note.Dir;
            if (dirDelta != 0)
            {
                Dictionary<EditableLeverNote, int> startTimes = Note.Notes.ToDictionary(n => n, n => n.StartTime);
                Dictionary<EditableLeverNote, LeverPos> leverPositions = Note.Notes.ToDictionary(n => n, n => n.Pos);
                EditLeverSequenceAction editLeverSequenceAction = new EditLeverSequenceAction(Note, dirDelta, startTimes, startTimes, leverPositions, leverPositions);
                editLeverSequenceAction.CommitAction();
            }
            Note.Dir = LeftRight.Right;
        });
    }

    void Update()
    {
        if (Note == null) return;
        
        if (Note.Dir == LeftRight.Left)
        {
            if (!leftToggle.AreAllTogglesOn) leftToggle.SendEvent(notify: false, playAudio: false);
        }
        else if (Note.Dir == LeftRight.Right)
        {
            if (!rightToggle.AreAllTogglesOn) rightToggle.SendEvent(notify: false, playAudio: false);
        }
    }

    protected override EditableLeverNote NewChildNote()
    {
        LeverNote lastNote = Note.Notes.Last;
        int time = lastNote.EndTime + ChartManager.ChartPackage.BeatToTime(1); // place 1 beat after last note
        // TODO: maybe place same distance as prev 2 notes?
        LeverPos pos = lastNote.Pos == LeverPos.Middle ? LeverPos.Left : LeverPos.Middle;
        return new EditableLeverNote(Note, time, pos);
    }
}
