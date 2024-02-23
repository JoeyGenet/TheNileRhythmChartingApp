using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeverNoteRenderer : NoteRenderer
{
    public LeverSequence.LeverNote Note;
    public LeftRight Dir;

    [Header("Sprites")]
    [SerializeField] private GameObject arrow;
    [SerializeField] private SpriteRenderer arrowSpriteRenderer;
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
    
    // Start is called before the first frame update
    void Start()
    {
        //lever direction
        switch (Note.pos)
        {
            case LeverPos.Left:
                arrow.transform.Rotate(0, 0, 0);
                arrowSpriteRenderer.color = leftColor;
                break;
            case LeverPos.Up:
                arrow.transform.Rotate(0, 0, -90);
                arrowSpriteRenderer.color = upColor;
                break;
            case LeverPos.Right:
                arrow.transform.Rotate(0, 0, 180);
                arrowSpriteRenderer.color = rightColor;
                break;
            case LeverPos.Down:
                arrow.transform.Rotate(0, 0, 90);
                arrowSpriteRenderer.color = downColor;
                break;
            case LeverPos.Middle:
                arrowSpriteRenderer.sprite = neutralSprite;
                arrowSpriteRenderer.color = middleColor;
                break;
            default:
                throw new System.Exception($"Lever direction {Note.pos} not supported.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // transform.position = RenderingManager.Instance.LeverTrackToWorldPosition(Dir, Note.StartTime);
    }
}
