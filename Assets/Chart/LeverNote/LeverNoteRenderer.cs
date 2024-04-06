using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class LeverNoteRenderer : NoteRenderer<EditableLeverNote, EditableLeverSequence>
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

    [Header("Hit Sounds")]
    [SerializeField] AudioClip leverSideSound;
    [SerializeField] AudioClip leverPushSound;
    [SerializeField] AudioClip leverPullSound;

    AudioSource audioSource;

    protected override void Awake()
    {
        base.Awake();
        audioSource = GetComponent<AudioSource>();
    }

    protected override void OnNoteStarted()
    {
        audioSource.clip = Note.Pos switch
        {
            LeverPos.Left => leverSideSound,
            LeverPos.Up => leverPushSound,
            LeverPos.Right => leverSideSound,
            LeverPos.Down => leverPullSound,
            LeverPos.Middle => leverPullSound,
            _ => throw new System.Exception($"Lever direction not supported.")
        };
        audioSource.Play();
    }

    protected override void Update()
    {
        base.Update();
        
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
}
