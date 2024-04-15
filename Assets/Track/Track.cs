using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

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

    [Header("Beat Markers")]
    [SerializeField] BeatMarker mainBeatMarkerPrefab;
    [SerializeField] BeatMarker minorBeatMarkerPrefab;
    [SerializeField] int maxMainBeatMarkers = 50;
    [SerializeField] int maxMinorBeatMarkers = 50;

    public bool IsPointerOverTrack { get; private set; } = false;
    public bool IsScrolling { get; private set; } = false;

    RectTransform rect;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        
        ChartManager.OnChartChanged += Reload;
        ChartManager.OnNoteAdded += AddNote;
    }

    void OnDestroy()
    {
        ChartManager.OnChartChanged -= Reload;
        ChartManager.OnNoteAdded -= AddNote;
    }

    void Start()
    {
        ReloadBeatMarkers();
        Reload();
    }

    void Reload(EditableChart oldChart = null, EditableChart newChart = null)
    {
        foreach (Note note in ChartManager.SelectedChart.Notes) AddNote(note);
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

    (int, int)? MouseToTrackPosition()
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mainTrackArea, Input.mousePosition, null, out Vector2 mousePos))
        {
            Vector2 uiPos = (mousePos - mainTrackArea.rect.position) / mainTrackArea.rect.size;
            var (pos, time) = PlaybackManager.Instance.UIToTrackPosition(uiPos);
            return (Mathf.Clamp(pos, 0, 100), Mathf.Clamp(time, 0, int.MaxValue)); // TODO: clamp to song length
        }
        return null;
    }

    void AddNoteOnKeyPress()
    {
        // Only add notes through key presses if not currently typing in input field
        if (UIManager.IsSelectingInputField) return;
        if (MouseToTrackPosition() is not (int pos, int time)) return;
        
        var chart = ChartManager.SelectedChart;

        if (Input.GetKeyUp(KeyCode.Alpha1) || Input.GetKeyUp(KeyCode.B))
        {
            chart.AddNote(new EditableButtonNote(chart, time, pos, null));
        }
        if (Input.GetKeyUp(KeyCode.Alpha2) || Input.GetKeyUp(KeyCode.F))
        {
            chart.AddNote(new EditableWheelFlick(chart, time, pos < 50 ? LeftRight.Left : LeftRight.Right));
        }
        if (Input.GetKeyUp(KeyCode.Alpha3) || Input.GetKeyUp(KeyCode.W))
        {
            // If placing inside an existing sequence or holding shift, add to the previous sequence. Otherwise create a new sequence
            if (chart.GetNotesBefore(time).LastOrDefault(note => note is EditableWheelSequence) is EditableWheelSequence lastWheelSequence
                && (lastWheelSequence.EndTime > time || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
            {
                var prevNote = lastWheelSequence.GetNotesBefore(time).LastOrDefault() as EditableWheelTurn;
                int prevEndAngle = prevNote == null ? WheelTurn.PosToAngle(lastWheelSequence.Pos)
                    : WheelTurn.WrapAngle(prevNote.StartAngle + prevNote.DeltaAngle);
                int endAngle = WheelTurn.PosToAngle(pos);
                lastWheelSequence.AddNote(new EditableWheelTurn(lastWheelSequence, time, endAngle - prevEndAngle));
            }
            else
            {
                chart.AddNote(new EditableWheelSequence(chart, time, pos, new() { (0, time + ChartManager.ChartPackage.BeatToTime(1)) }));
            }
        }
        if (Input.GetKeyUp(KeyCode.Alpha4) || Input.GetKeyUp(KeyCode.L))
        {
            // If placing inside an existing sequence or holding shift, add to the previous sequence. Otherwise create a new sequence
            LeftRight side = pos < 50 ? LeftRight.Left : LeftRight.Right;
            if (chart.GetNotesBefore(time).LastOrDefault(note => note is EditableLeverSequence seq && seq.Dir == side) is EditableLeverSequence lastLeverSequence
                && (lastLeverSequence.EndTime > time || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
            {
                LeverPos prevPos = lastLeverSequence.GetNotesBefore(time).Last().Pos;
                lastLeverSequence.AddNote(new EditableLeverNote(lastLeverSequence, time, prevPos == LeverPos.Middle ? LeverPos.Left : LeverPos.Middle));
            }
            else
            {
                chart.AddNote(new EditableLeverSequence(chart, pos < 50 ? LeftRight.Left : LeftRight.Right, new() { (time, LeverPos.Left) }));
            }
        }
    }

    void Update()
    {
        float hView = PlaybackManager.Instance.HorizontalViewSize;
        rect.anchorMin = new Vector2((1 - hView) / 2f, rect.anchorMin.y);
        rect.anchorMax = new Vector2((1 + hView) / 2f, rect.anchorMax.y);

        try
        {
            AddNoteOnKeyPress();
        }
        catch (Exception e)
        {
            UIManager.Instance.ShowAlert("Exception While Adding Note", $"Note may not have been added.\n{e.Message}");
            Debug.LogException(e);
        }
    }

    void OnGUI()
    {
        int scrollAmount = (int) (PlaybackManager.Instance.VerticalViewSize * Input.mouseScrollDelta.y * ScrollSpeed);
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

    public void OnPointerEnter(PointerEventData eventData) => IsPointerOverTrack = true;
    public void OnPointerExit(PointerEventData eventData) => IsPointerOverTrack = false;
}
