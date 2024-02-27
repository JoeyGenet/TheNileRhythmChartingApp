using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Track : MonoBehaviour
{
    [Header("Track Areas")]
    [SerializeField] RectTransform mainTrackArea;
    [SerializeField] RectTransform leverTrackArea;
    [SerializeField] RectTransform beatMarkers;

    [Header("Note Prefabs")]
    [SerializeField] WheelFlickRenderer wheelFlickPrefab;

    [Header("Beat Markers")]
    [SerializeField] BeatMarker mainBeatMarkerPrefab;
    [SerializeField] BeatMarker minorBeatMarkerPrefab;
    [SerializeField] int maxMainBeatMarkers = 50;
    [SerializeField] int maxMinorBeatMarkers = 50;

    void Awake()
    {
        SaveManager.OnChartLoaded += Reload;
    }

    void OnDestroy()
    {
        SaveManager.OnChartLoaded -= Reload;
    }

    void Start()
    {
        ReloadBeatMarkers();
        Reload();
    }

    void Reload(string _ = "")
    {
        foreach (Note note in SaveManager.Instance.Chart.Notes)
        {
            switch (note)
            {
                case WheelFlick flick:
                    var wheelFlickRenderer = Instantiate(wheelFlickPrefab, mainTrackArea);
                    wheelFlickRenderer.Note = flick;
                    break;
                default:
                    throw new System.Exception($"Spawning of note {note} not supported");
            }
        }
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
}
