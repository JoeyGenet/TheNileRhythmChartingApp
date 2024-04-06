using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class ButtonNoteRenderer : NoteRenderer<EditableButtonNote>, IPointerClickHandler
{
    [SerializeField] RectTransform leftNote;
    [SerializeField] RectTransform rightNote;
    [SerializeField] Image line;
    
    AudioSource hitSound;

    protected override void Awake()
    {
        base.Awake();
        hitSound = GetComponent<AudioSource>();
    }

    protected override void OnNoteStarted()
    {
        hitSound.Play();
    }

    protected override void Update()
    {
        base.Update();
        
        // Hide right note if note is single
        rightNote.gameObject.SetActive(!Note.IsSingle);
        line.gameObject.SetActive(!Note.IsSingle);

        // Update position
        Vector2 leftUIPos = PlaybackManager.Instance.TrackToUIPosition(Note.LeftPos, Note.StartTime);
        rect.anchorMin = leftUIPos;
        rect.anchorMax = Note.RightPos is int rightPos
            ? PlaybackManager.Instance.TrackToUIPosition(rightPos, Note.StartTime)
            : leftUIPos;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        UIManager.Instance.SelectNote(Note);
    }
}
