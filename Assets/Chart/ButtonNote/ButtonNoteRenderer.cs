using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class ButtonNoteRenderer : NoteRenderer<ButtonNote>, IPointerClickHandler
{
    // public ButtonNote Note;

    [SerializeField] RectTransform leftNote;
    [SerializeField] RectTransform rightNote;
    [SerializeField] Image line;
    [SerializeField] private GameObject hitParticles;

    RectTransform rect;

    protected override void Awake()
    {
        base.Awake();

        rect = GetComponent<RectTransform>();
    }

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
        // Hide right note if note is single
        rightNote.gameObject.SetActive(!Note.IsSingle);
        line.gameObject.SetActive(!Note.IsSingle);

        // Update position
        Vector2 leftUIPos = PlaybackManager.Instance.TrackToUIPosition(Note.LeftPos, Note.StartTime);
        rect.anchorMin = leftUIPos;
        rect.anchorMax = Note.RightPos is int rightPos
            ? PlaybackManager.Instance.TrackToUIPosition(rightPos, Note.StartTime)
            : leftUIPos;

        // // Show/hide note based on whether it has been hit
        // rightNote.SetActive(!Note.IsSingle && !Note.IsHit[LeftRight.Right]);
        // line.gameObject.SetActive(!Note.IsSingle && !Note.IsAnyHit);
        // leftNote.SetActive(!Note.IsHit[LeftRight.Left]);

        // // Update note position
        // transform.position = RenderingManager.Instance.TrackToWorldPosition(Note.LeftPos, Note.StartTime);
        // if (Note.RightPos is int rightPos)
        // {
        //     Vector3 rightWorldPos = RenderingManager.Instance.TrackToWorldPosition(rightPos, Note.StartTime);
        //     rightNote.transform.position = rightWorldPos;
        //     line.SetPosition(0, transform.position);
        //     line.SetPosition(1, rightNote.transform.position);
        // }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        UIManager.Instance.SelectNote(Note);
    }
}
