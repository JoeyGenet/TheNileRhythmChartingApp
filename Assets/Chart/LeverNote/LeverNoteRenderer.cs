using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LeverNoteRenderer : NoteRenderer<LeverNote>
{
    [Header("Sprites")]
    [SerializeField] private Image arrow;
    [SerializeField] private Sprite arrowSprite;
    [SerializeField] private Sprite neutralSprite;
    [SerializeField] private Color leftColor;
    [SerializeField] private Color upColor;
    [SerializeField] private Color rightColor;
    [SerializeField] private Color downColor;
    [SerializeField] private Color middleColor;

    [Header("Particles")]
    [SerializeField] private GameObject leftParticles;
    [SerializeField] private GameObject rightParticles;
    [SerializeField] private GameObject upParticles;
    [SerializeField] private GameObject downParticles;
    [SerializeField] private GameObject middleParticles;
    
    // public override void OnNoteHit(Note note, JudgementGrade grade, int points)
    // {
    //     if (note != Note) return;
    //     print("LeverNoteRenderer OnNoteHit() signal!!");
    //     switch (Note.pos)
    //     {
    //         case LeverPos.Left:
    //             Instantiate(leftParticles, transform.position, transform.rotation);
    //             break;
    //         case LeverPos.Up:
    //             Instantiate(upParticles, transform.position, transform.rotation);
    //             break;
    //         case LeverPos.Right:
    //             Instantiate(rightParticles, transform.position, transform.rotation);
    //             break;
    //         case LeverPos.Down:
    //             Instantiate(downParticles, transform.position, transform.rotation);
    //             break;
    //         case LeverPos.Middle:
    //             Instantiate(middleParticles, transform.position, transform.rotation);
    //             break;
    //         default:
    //             throw new System.Exception($"Lever direction not supported.");
    //     }

    //     //despawn note
    //     gameObject.SetActive(false);
    // }

    public override void OnNoteMissed(Note note)
    {
        if (note != Note) return;
        print("LeverNoteRenderer OnNoteMissed() signal!!");

        //despawn note
        gameObject.SetActive(false);
    }

    protected override void Awake()
    {
        base.Awake();

        LeverSequence.OnLeverNoteRemoved += RemoveNote;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        LeverSequence.OnLeverNoteRemoved -= RemoveNote;
    }

    void Update()
    {
        // Update position
        Vector2 uiPos = PlaybackManager.Instance.TrackToUIPosition(50, Note.StartTime);
        rect.anchorMin = new Vector2(rect.anchorMin.x, uiPos.y);
        rect.anchorMax = new Vector2(rect.anchorMax.x, uiPos.y);

        // update lever direction
        float angle;
        (arrow.sprite, angle, arrow.color) = Note.Pos switch
        {
            LeverPos.Left => (arrowSprite, 0, leftColor),
            LeverPos.Up => (arrowSprite, -90, upColor),
            LeverPos.Right => (arrowSprite, 180, rightColor),
            LeverPos.Down => (arrowSprite, 90, downColor),
            LeverPos.Middle => (neutralSprite, 0, middleColor),
            _ => throw new System.Exception($"Lever direction {Note.Pos} not supported."),
        };
        arrow.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void RemoveNote(LeverSequence sequence, LeverNote leverNote)
    {
        if (leverNote == Note) Destroy(gameObject);
    }
}
