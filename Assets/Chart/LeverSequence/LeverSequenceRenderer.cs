using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class LeverSequenceRenderer : NoteRenderer<EditableLeverSequence>, IPointerDownHandler
{
    [SerializeField] RectTransform line;
    [SerializeField] LeverNoteRenderer leverNotePrefab;

    public override void Init(Track track, EditableLeverSequence note)
    {
        base.Init(track, note);

        foreach (EditableLeverNote leverNote in Note.Notes)
        {
            AddNote(leverNote);
        }
        Note.OnNoteAdded += AddNote;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        Note.OnNoteAdded -= AddNote;
    }

    protected override void Update()
    {
        base.Update();

        // Update position
        float anchorX = Note.Dir == LeftRight.Left ? 0 : 1;
        rect.anchorMin = new Vector2(anchorX, rect.anchorMin.y);
        rect.anchorMax = new Vector2(anchorX, rect.anchorMax.y);

        // Update line position
        Vector2 startUIPos = PlaybackManager.Instance.TrackToUIPosition(50, Note.StartTime);
        Vector2 endUIPos = PlaybackManager.Instance.TrackToUIPosition(50, Note.EndTime);
        line.anchorMin = new Vector2(line.anchorMin.x, startUIPos.y);
        line.anchorMax = new Vector2(line.anchorMax.x, endUIPos.y);
    }

    void AddNote(EditableLeverNote leverNote)
    {
        var leverNoteRenderer = Instantiate(leverNotePrefab, rect);
        leverNoteRenderer.Init(Track, leverNote);
    }

    protected override void OnLeftClick() => UIManager.SelectedElement = Note;
}
