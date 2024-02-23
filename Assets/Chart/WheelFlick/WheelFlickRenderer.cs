using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelFlickRenderer : NoteRenderer
{
    public WheelFlick Note;

    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Material leftSwipeMaterial;
    [SerializeField] private Material rightSwipeMaterial;
    [SerializeField] private GameObject rightSwipeHitParticles;
    [SerializeField] private GameObject leftSwipeHitParticles;

    public AK.Wwise.Event FlickLeftEvent;
    public AK.Wwise.Event FlickRightEvent;

    public override void OnNoteHit(Note note, JudgementGrade grade, int points)
    {
        // base.OnNoteHit(grade, points);
        if (note != Note) return;
        print("WheelFlickRenderer OnNoteHit() signal!!");

        //create particles, despawn note
        if (Note.Dir == LeftRight.Right)
        {
            Instantiate(rightSwipeHitParticles, transform.position, transform.rotation);
            FlickRightEvent.Post(gameObject);
        }
        else
        {
            Instantiate(leftSwipeHitParticles, transform.position, transform.rotation);
            FlickLeftEvent.Post(gameObject);
        }

        gameObject.SetActive(false);
    }

    public override void OnNoteMissed(Note note)
    {
        // base.OnNoteHit(grade, points);
        if (note != Note) return;
        print("WheelFlickRenderer OnNoteMissed() signal!!");

        //despawn note
        gameObject.SetActive(false);
    }

    // Start is called before the first frame update
    void Start()
    {
        if (Note.Dir == LeftRight.Right)
        {
            lineRenderer.material = rightSwipeMaterial;
        }
        else
        {
            lineRenderer.material = leftSwipeMaterial;
        }
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = RenderingManager.Instance.TrackToWorldPosition(50, Note.StartTime);
        // transform.position = RenderingManager.Instance.TrackToWorldPosition(0, Note.StartTime);
    }
}
