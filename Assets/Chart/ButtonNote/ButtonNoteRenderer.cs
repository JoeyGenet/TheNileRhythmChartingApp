using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonNoteRenderer : NoteRenderer
{
    public ButtonNote Note;

    [SerializeField] GameObject leftNote;
    [SerializeField] GameObject rightNote;
    [SerializeField] LineRenderer line;
    [SerializeField] private GameObject hitParticles;

    // public override void OnNoteHit(Note note, JudgementGrade grade, int points)
    // {
    //     if (note != Note) return;
    //     print("ButtonNoteRenderer OnNoteHit() signal!!");
    //     Instantiate(hitParticles, transform.position, transform.rotation);

    //     //despawn note
    //     gameObject.SetActive(false);
    // }

    public override void OnNoteMissed(Note note)
    {
        if (note != Note) return;
        print("ButtonNoteRenderer OnNoteMissed() signal!!");

        //despawn note
        gameObject.SetActive(false);
    }

    void Update()
    {
        // Show/hide note based on whether it has been hit
        rightNote.SetActive(!Note.IsSingle && !Note.IsHit[LeftRight.Right]);
        line.gameObject.SetActive(!Note.IsSingle && !Note.IsAnyHit);
        leftNote.SetActive(!Note.IsHit[LeftRight.Left]);

        // Update note position
        transform.position = RenderingManager.Instance.TrackToWorldPosition(Note.LeftPos, Note.StartTime);
        if (Note.RightPos is int rightPos)
        {
            Vector3 rightWorldPos = RenderingManager.Instance.TrackToWorldPosition(rightPos, Note.StartTime);
            rightNote.transform.position = rightWorldPos;
            line.SetPosition(0, transform.position);
            line.SetPosition(1, rightNote.transform.position);
        }
    }
}
