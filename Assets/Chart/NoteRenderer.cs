using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public abstract class NoteRenderer<NoteType> : MonoBehaviour where NoteType : Note
{
    protected Track Track;
    protected NoteType Note;

    protected RectTransform rect;

    int prevTime = int.MaxValue;

    public virtual void Init(Track track, NoteType note)
    {
        Track = track;
        Note = note;
    }

    protected virtual void Awake()
    {
        rect = GetComponent<RectTransform>();

        SaveManager.OnChartLoaded += DestroyOnChartLoaded;
        Chart.OnNoteRemoved += DestroyOnNoteRemoved;
    }

    protected virtual void OnDestroy()
    {
        SaveManager.OnChartLoaded -= DestroyOnChartLoaded;
        Chart.OnNoteRemoved -= DestroyOnNoteRemoved;
    }

    // TODO: create UpdateOnPlayback that is called when the user drags the playback/zoom or during playback
    // and note is visible or startTime and endTime go in/out of view as a result

    // Methods called when the note starts or ends on playback (not when user is scrolling)
    protected virtual void OnNoteStarted() {}
    protected virtual void OnNoteEnded() {}


    protected virtual void Update()
    {
        int currTime = PlaybackManager.Instance.MillisecondsInChart;
        if (!Track.IsScrolling && prevTime < Note.StartTime && currTime >= Note.StartTime) OnNoteStarted();
        if (!Track.IsScrolling && prevTime < Note.EndTime && currTime >= Note.EndTime) OnNoteStarted();
        prevTime = currTime;
    }

    void DestroyOnChartLoaded(string _)
    {
        Destroy(gameObject);
    }

    void DestroyOnNoteRemoved(Note note)
    {
        if (note == Note)
        {
            Destroy(gameObject);
        }
    }
}
