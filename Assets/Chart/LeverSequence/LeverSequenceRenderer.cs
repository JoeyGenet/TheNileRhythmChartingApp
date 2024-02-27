using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LeverSequenceRenderer : NoteRenderer<LeverSequence>
{
    // public LeverSequence Note;

    [SerializeField] LineRenderer line;

    // public override void OnNoteHit(Note note, JudgementGrade grade, int points)
    // {
    //     if (note != Note) return;
    //     print("LeverSequenceRenderer OnNoteHit() signal!!");

    //     //despawn note
    //     gameObject.SetActive(false);
    // }

    public override void OnNoteMissed(Note note)
    {
        if (note != Note) return;
        print("LeverSequenceRenderer OnNoteMissed() signal!!");

        //despawn note
        gameObject.SetActive(false);
    }

    // Start is called before the first frame update
    void Start()
    {
        // foreach (LeverSequence.LeverNote Note in Note.Notes)
        // {
        //     var leverNoteRenderer = Instantiate(leverNotePrefab);
        //     leverNoteRenderer.Note = leverNote;
        //     notes.Add(leverNoteRenderer);
        // }
    }

    // Update is called once per frame
    void Update()
    {
        line.SetPosition(0, RenderingManager.Instance.LeverTrackToWorldPosition(Note.Dir, Note.StartTime));
        line.SetPosition(1, RenderingManager.Instance.LeverTrackToWorldPosition(Note.Dir, Note.EndTime));
    }
}
