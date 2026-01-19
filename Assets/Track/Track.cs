using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class Track : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Amount of view moved for every scroll unit")]
    public float ScrollSpeed = 0.1f;

    [Header("Track Areas")]
    [SerializeField] RectTransform mainTrackArea;
    [SerializeField] RectTransform leverTrackArea;
    [SerializeField] RectTransform beatMarkers;

    [Header("Note Prefabs")]
    [SerializeField] ButtonNoteRenderer buttonNotePrefab;
    [SerializeField] WheelFlickRenderer wheelFlickPrefab;
    [SerializeField] LeverSequenceRenderer leverSequencePrefab;
    [SerializeField] WheelSequenceRenderer wheelSequencePrefab;

    [Header("Note Cursors")]
    [SerializeField] RectTransform buttonNoteCursor;
    [SerializeField] RectTransform leftWheelFlickCursor;
    [SerializeField] RectTransform rightWheelFlickCursor;
    [SerializeField] UILine wheelTurnCursor;
    [SerializeField] RectTransform leftLeverNoteCursor;
    [SerializeField] RectTransform middleLeverNoteCursor;

    [Header("Beat Markers")]
    [SerializeField] BeatMarker mainBeatMarkerPrefab;
    [SerializeField] BeatMarker minorBeatMarkerPrefab;
    [SerializeField] int maxMainBeatMarkers = 50;
    [SerializeField] int maxMinorBeatMarkers = 50;

    [Header("Environment Change Event")]
    [SerializeField] RectTransform environmentTrack;
    [SerializeField] RectTransform environmentTrackArea;
    [SerializeField] EventRelay addEnvironmentChangeEvent;
    [SerializeField] EnvironmentTrackColor environmentTrackColorPrefab;
    [SerializeField] EnvironmentChangeRenderer environmentChangePrefab;

    public bool IsPointerOverTrack { get; private set; } = false;
    public bool IsScrolling { get; set; } = false;
    public RectTransform LeverTrackArea { get => leverTrackArea; }
    public RectTransform MainTrackArea { get => mainTrackArea; }

    RectTransform rect;

    void Awake()
    {
        rect = GetComponent<RectTransform>();

        addEnvironmentChangeEvent.AddListener(AddEnvironmentChange);
        
        ChartManager.OnChartChanged += Reload;
        ChartManager.OnNoteAdded += AddNote;
        ChartManager.OnEventAdded += AddChartEvent;
    }

    void OnDestroy()
    {
        ChartManager.OnChartChanged -= Reload;
        ChartManager.OnNoteAdded -= AddNote;
        ChartManager.OnEventAdded -= AddChartEvent;
    }

    void Start()
    {
        ReloadBeatMarkers();
        Reload();
    }

    void Reload(EditableChart oldChart = null, EditableChart newChart = null)
    {
        foreach (var note in ChartManager.SelectedChart.Notes) AddNote(note);
        foreach (var evt in ChartManager.SelectedChart.Events) AddChartEvent(evt);
    }

    void ReloadBeatMarkers()
    {
        foreach (BeatMarker beatMarker in beatMarkers.GetComponentsInChildren<BeatMarker>())
        {
            Destroy(beatMarker.gameObject);
        }

        for (int i = 0; i < maxMainBeatMarkers; i++)
        {
            var beatMarker = Instantiate(mainBeatMarkerPrefab, beatMarkers);
            beatMarker.Beat = i;
            beatMarker.BeatWrap = 50;
        }

        for (int i = 0; i < maxMinorBeatMarkers; i++)
        {
            var beatMarker = Instantiate(minorBeatMarkerPrefab, beatMarkers);
            beatMarker.Beat = i%3 * 0.25f + 0.25f + i / 3;
            beatMarker.BeatWrap = 50 / 3;
        }
    }

    public (int, int)? PointerToTrackPosition(Vector2 pointerPosition, bool allowOutOfBounds = true, bool clamp = true)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mainTrackArea, pointerPosition, null, out Vector2 localPos))
        {
            Vector2 absPos = localPos - rect.rect.position;
            if (!allowOutOfBounds && (absPos.x < 0 || absPos.x > rect.rect.width || absPos.y < 0 || absPos.y > rect.rect.height))
            {
                return null;
            }

            Vector2 uiPos = (localPos - mainTrackArea.rect.position) / mainTrackArea.rect.size;
            var (pos, time) = PlaybackManager.Instance.UIToTrackPosition(uiPos);
            if (!clamp) return (pos, time);
            return (Math.Clamp(pos, 0, 100), Math.Clamp(time, 0, (int) ChartManager.ChartPackage.SongLength));
        }
        return null;

    }

    // returns local UI position relative to bottom right corner of the whole track area (i.e. (0,0) in the bottom right)
    public Vector2 TrackToUIPosition(int trackPosition, int trackTime, bool allowOutOfBounds = true, bool clamp = true)
    {
        Vector2 uiPos = PlaybackManager.Instance.TrackToUIPosition(trackPosition, trackTime);
        // scale normalized uiPos to mainTrackArea, then convert to whole track coordinate space
        Vector2 localPos = (uiPos * mainTrackArea.rect.size) - (rect.rect.position - mainTrackArea.rect.position);
        if (!clamp) return uiPos;
        float clampedX = Math.Clamp(localPos.x, 0, rect.rect.width);
        float clampedY = Math.Clamp(localPos.y, 0, rect.rect.height);
        return new Vector2(clampedX, clampedY);
    }

    void DisplayCursorNote()
    {
        // buttonNoteCursor.gameObject.SetActive(false);
        // leftWheelFlickCursor.gameObject.SetActive(false);
        // rightWheelFlickCursor.gameObject.SetActive(false);
        // wheelTurnCursor.gameObject.SetActive(false);
        // leftLeverNoteCursor.gameObject.SetActive(false);
        // middleLeverNoteCursor.gameObject.SetActive(false);

        bool showButtonNoteCursor = false;
        bool showLeftWheelFlickCursor = false;
        bool showRightWheelFlickCursor = false;
        bool showWheelTurnCursor = false;
        bool showLeftLeverNoteCursor = false;
        bool showMiddleLeverNoteCursor = false;

        // Only add notes through key presses if not currently typing in input field
        if (UIManager.IsSelectingInputField || PointerToTrackPosition(Input.mousePosition, allowOutOfBounds: false) is not (int pos, int time))
        {
            buttonNoteCursor.gameObject.SetActive(false);
            leftWheelFlickCursor.gameObject.SetActive(false);
            rightWheelFlickCursor.gameObject.SetActive(false);
            wheelTurnCursor.gameObject.SetActive(false);
            leftLeverNoteCursor.gameObject.SetActive(false);
            middleLeverNoteCursor.gameObject.SetActive(false);
            return;
        }

        time = UIManager.SnapTime(time);

        bool placeNote = Input.GetMouseButtonDown(0);
        
        var chart = ChartManager.SelectedChart;

        switch (UIManager.CursorMode)
        {
            case UICursorMode.ButtonNote:
            {
                var lastButtonNote = chart.GetNotesBefore(time).OfType<EditableButtonNote>().LastOrDefault(n => n.IsSingle);
                var nextButtonNote = chart.GetNotesAfter(time).OfType<EditableButtonNote>().FirstOrDefault(n => n.IsSingle);
                var closestButtonNote = lastButtonNote;
                if (lastButtonNote == null || (nextButtonNote != null 
                    && Math.Abs(nextButtonNote.StartTime-time) < Math.Abs(lastButtonNote.StartTime-time)))
                        closestButtonNote = nextButtonNote;

                int bounds = ChartManager.ChartPackage.BeatToTime(1);
                bool addToButtonNote = closestButtonNote != null && Math.Abs(closestButtonNote.StartTime-time) < bounds
                                    && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

                showButtonNoteCursor = true;
                if (addToButtonNote)
                {
                    int oldLeftPos = closestButtonNote.LeftPos;
                    int? oldRightPos = closestButtonNote.RightPos; 
                    var existingAnchor = PlaybackManager.Instance.TrackToUIPosition(closestButtonNote.LeftPos, closestButtonNote.StartTime);
                    var newAnchor = PlaybackManager.Instance.TrackToUIPosition(pos, closestButtonNote.StartTime);
                    if (closestButtonNote.LeftPos > pos)
                    {
                        buttonNoteCursor.anchorMin = newAnchor;
                        buttonNoteCursor.anchorMax = existingAnchor;
                        if (placeNote)
                        {
                            closestButtonNote.RightPos = closestButtonNote.LeftPos;
                            closestButtonNote.LeftPos = pos;
                            UIManager.SelectedElement = closestButtonNote;
                        }
                    }
                    else
                    {
                        buttonNoteCursor.anchorMin = existingAnchor;
                        buttonNoteCursor.anchorMax = newAnchor;
                        if (placeNote)
                        {
                            closestButtonNote.RightPos = pos;
                            UIManager.SelectedElement = closestButtonNote;
                        }
                    }
                    
                    // adding a linked button note is essentially changing the left and/or right note position
                    // log this action in edit history as MoveButtonNoteAction
                    if (placeNote)
                    {
                        int newLeftPos = closestButtonNote.LeftPos;
                        int? newRightPos = closestButtonNote.RightPos; 
                        EditButtonNoteAction moveButtonNoteAction = new EditButtonNoteAction(closestButtonNote, 0, oldLeftPos, oldRightPos, newLeftPos, newRightPos);
                        moveButtonNoteAction.CommitAction();
                    }
                }
                else
                {
                    buttonNoteCursor.anchorMin = buttonNoteCursor.anchorMax = PlaybackManager.Instance.TrackToUIPosition(pos, time);
                    if (placeNote) chart.AddNote(new EditableButtonNote(chart, time, pos, null));
                }
                
                break;
            }
            case UICursorMode.WheelFlick:
            {
                if (pos < 50) showLeftWheelFlickCursor = true;
                else showRightWheelFlickCursor = true;

                var cursor = pos < 50 ? leftWheelFlickCursor : rightWheelFlickCursor;
                // cursor.gameObject.SetActive(true);
                var uiPos = PlaybackManager.Instance.TrackToUIPosition(50, time);
                cursor.anchorMax = new Vector2(rect.anchorMax.x, uiPos.y);
                cursor.anchorMin = new Vector2(rect.anchorMin.x, uiPos.y);
                
                if (placeNote) chart.AddNote(new EditableWheelFlick(chart, time, pos < 50 ? LeftRight.Left : LeftRight.Right));
                break;
            }
            case UICursorMode.WheelTurn:
            {
                // If placing inside an existing sequence or holding shift, add to the previous sequence. Otherwise create a new sequence
                var lastWheelSequence = chart.GetNotesBefore(time).OfType<EditableWheelSequence>().LastOrDefault();
                bool addToWheelSequence = lastWheelSequence != null && (lastWheelSequence.EndTime > time 
                    || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
                
                // wheelTurnCursor.gameObject.SetActive(true);
                showWheelTurnCursor = true;
                wheelTurnCursor.pos1 = PlaybackManager.Instance.TrackToUIPosition(pos, time);
                if (addToWheelSequence)
                {
                    wheelTurnCursor.pos2 = PlaybackManager.Instance.TrackToUIPosition(pos, time+1);

                    if (placeNote)
                    {
                        var prevNote = lastWheelSequence.GetNotesBefore(time).LastOrDefault() as EditableWheelTurn;
                        int prevEndAngle = prevNote == null ? WheelTurn.PosToAngle(lastWheelSequence.Pos)
                            : WheelTurn.WrapAngle(prevNote.StartAngle + prevNote.DeltaAngle);
                        int endAngle = WheelTurn.PosToAngle(pos);

                        lastWheelSequence.AddNote(new EditableWheelTurn(lastWheelSequence, time, endAngle - prevEndAngle));
                        UIManager.SelectedElement = lastWheelSequence;
                    }
                }
                else
                {
                    int endTime = time+ChartManager.ChartPackage.BeatToTime(1);
                    wheelTurnCursor.pos2 = PlaybackManager.Instance.TrackToUIPosition(pos, endTime);

                    if (placeNote) chart.AddNote(new EditableWheelSequence(chart, time, pos, new() { (0, endTime) }));
                }
                break;
            }
            case UICursorMode.LeverNote:
            {
                LeftRight side = pos < 50 ? LeftRight.Left : LeftRight.Right;

                // If placing inside an existing sequence or holding shift, add to the previous sequence. Otherwise create a new sequence
                var lastLeverSequence = chart.GetNotesBefore(time).OfType<EditableLeverSequence>().LastOrDefault(seq => seq.Dir == side);
                bool addToLeverSequence = lastLeverSequence != null && (lastLeverSequence.EndTime > time 
                    || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

                LeverPos leverPos = LeverPos.Left;
                
                if (addToLeverSequence)
                {
                    LeverPos prevPos = lastLeverSequence.GetNotesBefore(time).Last().Pos;
                    if (prevPos != LeverPos.Middle) leverPos = LeverPos.Middle;

                    if (placeNote)
                    {
                        lastLeverSequence.AddNote(new EditableLeverNote(lastLeverSequence, time, leverPos));
                        UIManager.SelectedElement = lastLeverSequence;
                    }
                }
                else
                {
                    if (placeNote) chart.AddNote(new EditableLeverSequence(chart, side, new() { (time, leverPos) }));
                }

                if (leverPos == LeverPos.Left) showLeftLeverNoteCursor = true;
                else showMiddleLeverNoteCursor = true;

                var cursor = leverPos == LeverPos.Left ? leftLeverNoteCursor : middleLeverNoteCursor;
                // cursor.gameObject.SetActive(true);
                float anchorX = side == LeftRight.Left ? 0 : 1;
                Vector2 uiPos = PlaybackManager.Instance.TrackToUIPosition(50, time);
                cursor.anchorMin = cursor.anchorMax = new Vector2(anchorX, uiPos.y);

                break;
            }
        }

        buttonNoteCursor.gameObject.SetActive(showButtonNoteCursor);
        leftWheelFlickCursor.gameObject.SetActive(showLeftWheelFlickCursor);
        rightWheelFlickCursor.gameObject.SetActive(showRightWheelFlickCursor);
        wheelTurnCursor.gameObject.SetActive(showWheelTurnCursor);
        leftLeverNoteCursor.gameObject.SetActive(showLeftLeverNoteCursor);
        middleLeverNoteCursor.gameObject.SetActive(showMiddleLeverNoteCursor);
    }

    void Update()
    {
        float hView = PlaybackManager.Instance.HorizontalViewSize;
        rect.anchorMin = new Vector2((1 - hView) / 2f, rect.anchorMin.y);
        rect.anchorMax = new Vector2((1 + hView) / 2f, rect.anchorMax.y);

        try
        {
            DisplayCursorNote();
        }
        catch (Exception e)
        {
            UIManager.Instance.ShowAlert("Exception While Adding Note", $"Note may not have been added.\n{e.Message}");
            Debug.LogException(e);
        }

        // Update environment track color ordering
        // FIXME: inefficient
        var trackColors = environmentTrack.GetComponentsInChildren<EnvironmentTrackColor>()
            .Where(c => c.EnvironmentChange != null)
            .OrderBy(c => c.EnvironmentChange.StartTime);
        foreach (var (i, trackColor) in trackColors.Select((c, i) => (i, c)))
        {
            trackColor.transform.SetSiblingIndex(i+2);
        }
    }

    void OnGUI()
    {
        float scrollDelta = Mathf.Abs(Input.mouseScrollDelta.y) > Mathf.Abs(Input.mouseScrollDelta.x) 
            ? Input.mouseScrollDelta.y : Input.mouseScrollDelta.x;
        int scrollAmount = (int) (PlaybackManager.Instance.VerticalViewSize * scrollDelta * ScrollSpeed);
        if (IsPointerOverTrack && scrollAmount != 0)
        {
            PlaybackManager.Instance.SetTimestamp(PlaybackManager.Instance.MillisecondsInChart + scrollAmount);
            IsScrolling = true;
        }
        else
        {
            IsScrolling = false;
        }
    }

    void AddNote(Note note)
    {
		NoteRenderer<NoteType> instantiateNoteRenderer<NoteType>(NoteRenderer<NoteType> prefab, NoteType note, Transform parent)
		where NoteType : Note, IEditableNote<EditableChart, Note>
		{
			var noteRenderer = Instantiate(prefab, parent);

			// For some reason the renderer's Update isn't called until 1 frame after it is instantiated,
			// so this is added to not let the player see the note on the first frame the note is spawned
			// noteRenderer.transform.position = new Vector3(0, 1000, 0); 

			noteRenderer.Init(this, note);

			return noteRenderer;
		};

		MonoBehaviour _ = note switch
		{
			EditableButtonNote buttonNote => instantiateNoteRenderer(buttonNotePrefab, buttonNote, mainTrackArea),
			EditableWheelFlick wheelFlick => instantiateNoteRenderer(wheelFlickPrefab, wheelFlick, mainTrackArea),
			EditableLeverSequence leverSequence => instantiateNoteRenderer(leverSequencePrefab, leverSequence, leverTrackArea),
			EditableWheelSequence wheelSequence => instantiateNoteRenderer(wheelSequencePrefab, wheelSequence, mainTrackArea),
			_ => throw new Exception($"Spawning of note {note} not supported"),
		};
    }

    void AddEnvironmentChange()
    {
        var chart = ChartManager.SelectedChart;
        int startTime = PlaybackManager.ClampTime((int) (PlaybackManager.Instance.MillisecondsInChart + PlaybackManager.Instance.VerticalViewSize / 2));
        var evt = new EditableEnvironmentChangeEvent(chart, startTime, startTime + chart.Package.BeatToTime(1), EnvironmentManager.DefaultEnvironment);
        // track the addition for undo/redo
        AddEnvironmentChangeAction editEnvironmentChangeAction = new AddEnvironmentChangeAction(evt, chart);
        editEnvironmentChangeAction.CommitAction();
        chart.AddEvent(evt);
        UIManager.SelectedElement = evt;
    }
    void AddChartEvent(EnvironmentChangeEvent evt)
    {
        var renderer = Instantiate(environmentChangePrefab, environmentTrackArea);
        renderer.Init(this, (EditableEnvironmentChangeEvent) evt);

        var trackColor = Instantiate(environmentTrackColorPrefab, environmentTrack);
        trackColor.Init((EditableEnvironmentChangeEvent) evt);
    }

    public void OnPointerEnter(PointerEventData eventData) => IsPointerOverTrack = true;
    public void OnPointerExit(PointerEventData eventData) => IsPointerOverTrack = false;
}
