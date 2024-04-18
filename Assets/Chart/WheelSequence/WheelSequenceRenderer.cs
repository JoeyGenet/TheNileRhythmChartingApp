using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class WheelSequenceRenderer : NoteRenderer<EditableWheelSequence>, IPointerClickHandler
{
    [SerializeField] RectTransform linesParent;
    [SerializeField] UILine linePrefab;
    [SerializeField] Color color;
    readonly List<UILine> lines = new();

    [SerializeField] RectTransform verticesParent;
    [SerializeField] WheelSequenceVertexRenderer vertexPrefab;
    
    readonly List<WheelSequenceVertexRenderer> vertices = new();

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
            var line = Instantiate(linePrefab, linesParent);
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

        // Update vertices
        // Instantiate more vertices if not enough
        for (int i = vertices.Count; i < Note.Notes.Count+1; i++)
        {
            var vertex = Instantiate(vertexPrefab, verticesParent);
            vertex.Init(Note);
            vertices.Add(vertex);
        }
        // Destroy any extra lines
        for (int i = vertices.Count-1; i >= Note.Notes.Count+1; i--)
        {
            Destroy(vertices[i].gameObject);
            vertices.RemoveAt(i);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            UIManager.SelectedNote = Note;
        }
    }
}
