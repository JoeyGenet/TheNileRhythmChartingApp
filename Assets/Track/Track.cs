using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class Track : MonoBehaviour
{
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

    void Awake()
    {
        SaveManager.OnChartLoaded += Reload;
        Chart.OnNoteAdded += AddNote;
    }

    void OnDestroy()
    {
        SaveManager.OnChartLoaded -= Reload;
        Chart.OnNoteAdded -= AddNote;
    }

    void Start()
    {
        ReloadBeatMarkers();
        Reload();
    }

    void Reload(string _ = "")
    {
        foreach (Note note in SaveManager.Instance.Chart.Notes) AddNote(note);
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
        
        Chart chart = SaveManager.Instance.Chart;
        Note addedNote = null;

        if (Input.GetKeyUp(KeyCode.Alpha1) || Input.GetKeyUp(KeyCode.B))
        {
            addedNote = new ButtonNote(chart, time, pos, null);
            chart.AddNote(addedNote);
        }
        if (Input.GetKeyUp(KeyCode.Alpha2) || Input.GetKeyUp(KeyCode.F))
        {
            addedNote = new WheelFlick(chart, time, pos < 50 ? LeftRight.Left : LeftRight.Right);
            chart.AddNote(addedNote);
        }
        if (Input.GetKeyUp(KeyCode.Alpha3) || Input.GetKeyUp(KeyCode.L))
        {
            addedNote = new LeverSequence(chart, pos < 50 ? LeftRight.Left : LeftRight.Right, new() { (time, LeverPos.Left) });
            chart.AddNote(addedNote);
        }

        if (addedNote != null) UIManager.Instance.SelectNote(addedNote);
    }

    void Update()
    {
        AddNoteOnKeyPress();
    }

    void AddNote(Note note)
    {
        switch (note)
        {
            case ButtonNote buttonNote:
                var buttonNoteRenderer = Instantiate(buttonNotePrefab, mainTrackArea);
                buttonNoteRenderer.Note = buttonNote;
                break;
            case WheelFlick flick:
                var wheelFlickRenderer = Instantiate(wheelFlickPrefab, mainTrackArea);
                wheelFlickRenderer.Note = flick;
                break;
            case WheelSequence wheelSeq:
                var wheelSequenceRenderer = Instantiate(wheelSequencePrefab, mainTrackArea);
                wheelSequenceRenderer.Note = wheelSeq;
                break;
            case LeverSequence leverSeq:
                var leverSequenceRenderer = Instantiate(leverSequencePrefab, leverTrackArea);
                leverSequenceRenderer.Note = leverSeq;
                break;
            default:
                throw new System.Exception($"Spawning of note {note} not supported");
        }
    }
}
