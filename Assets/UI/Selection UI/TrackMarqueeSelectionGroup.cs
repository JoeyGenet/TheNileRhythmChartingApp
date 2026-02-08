using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.EventSystems.ExecuteEvents;

[RequireComponent(typeof(RectTransform))]
public class TrackMarqueeSelectionGroup : MonoBehaviour
{
    [SerializeField] RectTransform scrollUpArea;
    [SerializeField] RectTransform scrollDownArea;
    [SerializeField] float scrollSpeed = 0.1f;
    [SerializeField] Track Track;
    public List<Note> notes { get; private set; }
    public Dictionary<GameObject, Transform> rendererTransformMapping { get; private set; }

    protected RectTransform rect;

    int prevTime = int.MaxValue;

    public void Init(Track track)
    {
        Track = track;
    }

    protected void Awake()
    {
        rect = GetComponent<RectTransform>();
        notes = new List<Note>();
        rendererTransformMapping = new Dictionary<GameObject, Transform>();
    }

    protected void OnDestroy() {}

    // TODO: create UpdateOnPlayback that is called when the user drags the playback/zoom or during playback
    // and note is visible or startTime and endTime go in/out of view as a result

    protected void Update()
    {
        int currTime = PlaybackManager.Instance.MillisecondsInChart;
        prevTime = currTime;

        // scroll the track if you mouse over the scroll areas
        // DragScrollTrack();
    }


    public void DragScrollTrack()
    {
        // check if cursor is above bottom edge of the "scroll up" bounds
        RectTransformUtility.ScreenPointToLocalPointInRectangle(scrollUpArea, Input.mousePosition, null, out Vector2 localScrollUpPos);
        bool isScrollingUp = localScrollUpPos.y >= scrollUpArea.rect.yMin;
        // check if cursor is below top edge of the "scroll down" bounds
        RectTransformUtility.ScreenPointToLocalPointInRectangle(scrollDownArea, Input.mousePosition, null, out Vector2 localScrollDownPos);
        bool isScrollingDown = localScrollDownPos.y <= scrollDownArea.rect.yMax;

        int scrollAmount = isScrollingUp ? (int) (PlaybackManager.Instance.VerticalViewSize * scrollSpeed) :
                           isScrollingDown ? (int) (PlaybackManager.Instance.VerticalViewSize * -scrollSpeed) : 0;
        if (scrollAmount != 0)
        {
            PlaybackManager.Instance.SetTimestamp(PlaybackManager.Instance.MillisecondsInChart + scrollAmount);
            Track.IsScrolling = true;
        }
        else
        {
            Track.IsScrolling = false;
        }
    }

    public void SetSelectedNoteRenderersFlag(bool isInMarqueeSelection, List<Note> selectedNotes=null)
    {
        if (selectedNotes != null)
        {
            notes = selectedNotes;
        }
        foreach(Note note in notes)
        {
            // get the object associated with each note
            if (Track.noteGameObjectMapping.TryGetValue(note, out GameObject gameObject) && gameObject!=null)
            {
                // flag all selected notes to change their click/drag/drop behavior
                switch (note)
                {
                    case EditableButtonNote:
                        ButtonNoteRenderer buttonNoteRenderer = gameObject.GetComponent<ButtonNoteRenderer>();
                        buttonNoteRenderer.isInMarqueeSelection = isInMarqueeSelection;
                        break;
                    case EditableLeverNote:
                        LeverNoteRenderer leverNoteRenderer = gameObject.GetComponent<LeverNoteRenderer>();
                        leverNoteRenderer.isInMarqueeSelection = isInMarqueeSelection;
                        break;
                    case EditableWheelFlick:
                        WheelFlickRenderer wheelFlickRenderer = gameObject.GetComponent<WheelFlickRenderer>();
                        wheelFlickRenderer.isInMarqueeSelection = isInMarqueeSelection;
                        break;
                    case EditableWheelTurn:
                        WheelSequenceVertexRenderer wheelSequenceVertexRenderer = gameObject.GetComponent<WheelSequenceVertexRenderer>();
                        wheelSequenceVertexRenderer.isInMarqueeSelection = isInMarqueeSelection;
                        break; 
                    default:
                        break;
                }
            }
        }
    }

    public void PassEventDataToAllSelectedNotes<T>(BaseEventData eventData, EventFunction<T> functor)
    where T : IEventSystemHandler
    {
        foreach(Note note in notes)
        {
            // pass the event data to all notes, including the selected one
            // the selected note does not execute the event initially, instead waiting until now
            // so that it can execute sequentially with related notes (i.e. wheel sequences)
            if (Track.noteGameObjectMapping.TryGetValue(note, out GameObject gameObject) && gameObject!=null)
            {
                Execute(gameObject, eventData, functor);
            }
        }
    }
    
}
