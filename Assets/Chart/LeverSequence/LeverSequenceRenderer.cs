using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class LeverSequenceRenderer : NoteRenderer<LeverSequence>, IPointerClickHandler
{
    [SerializeField] RectTransform line;
    [SerializeField] LeverNoteRenderer leverNotePrefab;

    protected override void Awake()
    {
        base.Awake();

        LeverSequence.OnLeverNoteAdded += AddNote;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        LeverSequence.OnLeverNoteAdded -= AddNote;
    }

    void Start()
    {
        foreach (LeverNote leverNote in Note.Notes)
        {
            AddNote(Note, leverNote);
        }
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

    void AddNote(LeverSequence sequence, LeverNote leverNote)
    {
        if (sequence == Note)
        {
            var leverNoteRenderer = Instantiate(leverNotePrefab, rect);
            leverNoteRenderer.Init(Track, leverNote);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        UIManager.Instance.SelectNote(Note);
    }
}
