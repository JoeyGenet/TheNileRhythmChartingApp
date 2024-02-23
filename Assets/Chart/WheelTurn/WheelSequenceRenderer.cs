using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class WheelSequenceRenderer : NoteRenderer
{
    public WheelSequence Note;
    [SerializeField] public List<LineRenderer> lineRenderers;
    [SerializeField] private GameObject wheelTurnParticles;
    private ParticleSystem wheelTurnParticleSystem;

    public override void OnNoteHit(Note note, JudgementGrade grade, int points)
    {
        //handle hit for individual turn notes in the sequence
        if (note.GetType() == typeof(WheelSequence.WheelTurn) && Note.Turns.Contains((WheelSequence.WheelTurn)note)) //note.GetType() == WheelSequence.WheelTurn && ?
        {
            print("'WheelNote' OnNoteHit() signal!!");
            wheelTurnParticleSystem.Play();
        }
        //handle hit for sequence as a whole
        if (note != Note) return;
        print("WheelSequenceRenderer OnNoteHit() signal!!");

        //despawn note
        gameObject.SetActive(false);
    }

    public override void OnNoteMissed(Note note)
    {
        //handle hit for individual turn notes in the sequence
        if ((note.GetType() == typeof(WheelSequence.WheelTurn) && Note.Turns.Contains((WheelSequence.WheelTurn)note)) || note == Note) //note.GetType() == WheelSequence.WheelTurn && ?
        {
            print("'WheelNote' OnNoteMissed() signal!!");
            wheelTurnParticleSystem.Stop();
        }
        //handle miss for sequence as a whole
        if (note != Note) return;
        print("WheelSequenceRenderer OnNoteMissed() signal!!");

        //despawn note
        wheelTurnParticles.transform.parent = null;
        gameObject.SetActive(false);
    }

    // Start is called before the first frame update
    void Start()
    {
        wheelTurnParticleSystem = wheelTurnParticles.GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        //remove the first line renderer if it has no points
        if (lineRenderers[0].positionCount <= 1)
        {
            lineRenderers[0].gameObject.SetActive(false);
            lineRenderers.RemoveAt(0);
        }
        //consider the closest point in the first line renderer
        else
        {
            //calculate positions and times of each point in the turn
            Vector3 closestPoint = lineRenderers[0].GetPosition(0);
            Vector3 nextClosestPoint = lineRenderers[0].GetPosition(1);
            float time = (closestPoint.y * 200f) + Note.StartTime;
            float nextTime = (nextClosestPoint.y * 200f) + Note.StartTime;

            //if it is past the time this point should hit the end of the track,
            //move the point along the line to the next
            //if the next point up is past the line, remove the first
            if (nextTime < GameManager.Instance.MillisecondsInChart)
            {
                for (int i=0; i<lineRenderers[0].positionCount-1; i++)
                {
                    lineRenderers[0].SetPosition(i, lineRenderers[0].GetPosition(i+1));
                }
                lineRenderers[0].positionCount --;
            }
            else if (time <= GameManager.Instance.MillisecondsInChart)
            {
                Vector3 toNextClosestPoint = nextClosestPoint - closestPoint;
                float timeDifference = nextTime - time;
                float ratio = (GameManager.Instance.MillisecondsInChart-time)/(timeDifference);
                Vector3 newPos = (1-ratio)*closestPoint + (ratio)*nextClosestPoint;
                lineRenderers[0].SetPosition(0, newPos); 
                // wheelTurnParticleSystem.Play();
                wheelTurnParticles.transform.localPosition = newPos; //new Vector3(newPos.x, 0, 0);
            }
        }

        transform.position = RenderingManager.Instance.TrackToWorldPosition(50, Note.StartTime);
    }
}
