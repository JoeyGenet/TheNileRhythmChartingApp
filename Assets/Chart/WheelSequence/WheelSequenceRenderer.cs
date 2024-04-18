using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class WheelSequenceRenderer : NoteRenderer<EditableWheelSequence>, IPointerClickHandler
{
    [SerializeField] UILine linePrefab;
    [SerializeField] GameObject wheelTurnParticles;
    private readonly ParticleSystem wheelTurnParticleSystem;
    [SerializeField] Color color;

    readonly List<UILine> lines = new();

    protected override void Update()
    {
        base.Update();

        // Update lines
        var lineSequences = Note.ComputeLines();
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
            Debug.Log("Remove line");
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
        UIManager.SelectedNote = Note;
    }
}
