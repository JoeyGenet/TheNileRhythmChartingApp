using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class WheelSequenceRenderer : NoteRenderer<WheelSequence>, IPointerClickHandler
{
    // public WheelSequence Note;
    // [SerializeField] public List<LineRenderer> lineRenderers;
    [SerializeField] UILine linePrefab;
    [SerializeField] GameObject wheelTurnParticles;
    private ParticleSystem wheelTurnParticleSystem;
    [SerializeField] Color color;

    readonly List<UILine> lines = new();

    // public override void OnNoteHit(Note note, JudgementGrade grade, int points)
    // {
    //     //handle hit for individual turn notes in the sequence
    //     if (note.GetType() == typeof(WheelSequence.WheelTurn) && Note.Turns.Contains((WheelSequence.WheelTurn)note)) //note.GetType() == WheelSequence.WheelTurn && ?
    //     {
    //         print("'WheelNote' OnNoteHit() signal!!");
    //         wheelTurnParticleSystem.Play();
    //     }
    //     //handle hit for sequence as a whole
    //     if (note != Note) return;
    //     print("WheelSequenceRenderer OnNoteHit() signal!!");

    //     //despawn note
    //     gameObject.SetActive(false);
    // }

    public override void OnNoteMissed(Note note)
    {
        //handle hit for individual turn notes in the sequence
        if ((note.GetType() == typeof(WheelTurn) && Note.ContainsNote((WheelTurn)note)) || note == Note) //note.GetType() == WheelSequence.WheelTurn && ?
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

    void Start()
    {
        // var turn = new WheelTurn(null, 5000, startAngle, deltaAngle);
        // turn.StartTime = 1000;
        // var lines = turn.ComputeLines();
        // foreach (var (pos1, time1, pos2, time2) in lines)
        // {
        //     var l = Instantiate(linePrefab, rect);
        //     Debug.Log($"{pos1} {time1} {pos2} {time2}");
        //     l.pos1 = PlaybackManager.Instance.TrackToUIPosition(pos1, time1);
        //     l.pos2 = PlaybackManager.Instance.TrackToUIPosition(pos2, time2);
        // }
        // new WheelSequence(null, 500, 50, new() { 180 }, new() { 1000 }).ComputeLines();
        // new WheelSequence(null, 1000, 25, new() { 90, 0, -90 }, new() { 2000, 3000, 4000 }).ComputeLines();
    }

    void Update()
    {
        // return;
        // Update lines
        var lineSequences = Note.ComputeLines();
		foreach (var line in lineSequences) Debug.Log(string.Join(", ", line));
        var linePositions = lineSequences.SelectMany(lineSequence =>
        {
            List<(int pos1, int time1, int pos2, int time2)> positions = new();
            for (int i = 0; i < lineSequence.Count-1; i++)
            {
                positions.Add((lineSequence[i].pos, lineSequence[i].time, lineSequence[i+1].pos, lineSequence[i+1].time));
            }
            return positions;
        }).ToList();
        
        // Instantiate more lines if not enough
        for (int i = lines.Count; i < linePositions.Count; i++)
        {
            var line = Instantiate(linePrefab, rect);
            line.Image.color = color;
            lines.Add(line);
        }
        // Destroy any extra lines
        for (int i = lines.Count-1; i >= linePositions.Count; i--)
        {
            Destroy(lines[i].gameObject);
            lines.RemoveAt(i);
        }
        // Update line positions
        for (int i = 0; i < linePositions.Count; i++)
        {
            var (pos1, time1, pos2, time2) = linePositions[i];
            lines[i].pos1 = PlaybackManager.Instance.TrackToUIPosition(pos1, time1);
            lines[i].pos2 = PlaybackManager.Instance.TrackToUIPosition(pos2, time2);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        UIManager.Instance.SelectNote(Note);
    }
}
