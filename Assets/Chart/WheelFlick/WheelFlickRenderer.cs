using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class WheelFlickRenderer : NoteRenderer, IPointerClickHandler
{
    public WheelFlick Note;

    [SerializeField] Image image;

    [SerializeField] Sprite leftSprite;
    [SerializeField] Sprite rightSprite;

    [SerializeField] private Material leftSwipeMaterial;
    [SerializeField] private Material rightSwipeMaterial;

    RectTransform rect;

    // TODO: do particles?
    // [SerializeField] private GameObject rightSwipeHitParticles;
    // [SerializeField] private GameObject leftSwipeHitParticles;

    // public override void OnNoteHit(Note note, JudgementGrade grade, int points)
    // {
    //     // base.OnNoteHit(grade, points);
    //     if (note != Note) return;
    //     print("WheelFlickRenderer OnNoteHit() signal!!");

    //     //create particles, despawn note
    //     if (Note.Dir == LeftRight.Right)
    //     {
    //         Instantiate(rightSwipeHitParticles, transform.position, transform.rotation);
    //         FlickRightEvent.Post(gameObject);
    //     }
    //     else
    //     {
    //         Instantiate(leftSwipeHitParticles, transform.position, transform.rotation);
    //         FlickLeftEvent.Post(gameObject);
    //     }

    //     gameObject.SetActive(false);
    // }

    public override void OnNoteMissed(Note note)
    {
        // base.OnNoteHit(grade, points);
        if (note != Note) return;
        print("WheelFlickRenderer OnNoteMissed() signal!!");

        //despawn note
        gameObject.SetActive(false);
    }

    void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 uiPos = PlaybackManager.Instance.TrackToUIPosition(50, Note.StartTime);
        rect.anchorMax = new Vector2(rect.anchorMax.x, uiPos.y);
        rect.anchorMin = new Vector2(rect.anchorMin.x, uiPos.y);
        
        image.sprite = Note.Dir switch
        {
            LeftRight.Left => leftSprite,
            LeftRight.Right => rightSprite,
            _ => null,
        };
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        UIManager.Instance.SelectNote(Note);
    }
}
