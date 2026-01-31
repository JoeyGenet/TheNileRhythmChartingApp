using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class WheelSequenceRenderer : NoteRenderer<EditableWheelSequence>, IPointerDownHandler
{
    [SerializeField] RectTransform linesParent;
    [SerializeField] UILine linePrefab;
    [SerializeField] Color color;
    readonly List<UILine> lines = new();

    [SerializeField] RectTransform verticesParent;
    [SerializeField] WheelSequenceVertexRenderer vertexPrefab;

    public override void Init(Track track, EditableWheelSequence note)
    {
        base.Init(track, note);

        AddVertex(null);
        foreach (EditableWheelTurn turn in Note.Notes)
        {
            AddVertex(turn);
        }
        Note.OnNoteAdded += AddVertex;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        Note.OnNoteAdded -= AddVertex;
    }

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
    }

    void AddVertex(EditableWheelTurn turn)
    {
        var vertexRenderer = Instantiate(vertexPrefab, verticesParent);
        vertexRenderer.Init(this, turn);
    }

    protected override void OnLeftClick() => UIManager.SelectedElement = Note;
}
