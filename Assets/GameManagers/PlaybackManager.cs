using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlaybackManager : MonoBehaviour
{
    public static PlaybackManager Instance;

    public bool IsPaused { get; private set; } = true;
    
    int lastSyncTime = 0;
    int lastSyncTimeInChart = 0;

    public int MillisecondsInChart => IsPaused 
        ? lastSyncTimeInChart
        : (int)(Time.time * 1000) - lastSyncTime + lastSyncTimeInChart;

    public float BeatsInChart => SaveManager.Instance.Chart.TimeToBeat(MillisecondsInChart);

    int offset;
    public int Offset
    {
        get => offset;
        set
        {
            offset = value;
            audioSource.time = (MillisecondsInChart - offset) / 1000f;
        }
    }

    public uint ViewSize = 5000; // Number of milliseconds the view contains

    [Header("Song")]
    [SerializeField] List<AudioClip> songs;
    public IReadOnlyList<AudioClip> Songs => songs;
    int currentSong;
    public int CurrentSong
    {
        get => currentSong;
        set
        {
            currentSong = value;
            audioSource.clip = currentSong == -1 ? null : Songs[currentSong];
            Pause();
            SetTimestamp(0);
        }
    }
    public int CurrentSongLength => CurrentSong == -1
        ? SaveManager.Instance.Chart.Notes.Aggregate(10000, (a, n) => Math.Max(a, n.EndTime))
        : (int) (Songs[CurrentSong].length * 1000);
    
    AudioSource audioSource;

    void Awake()
    {
        Instance = this;

        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (!IsPaused && MillisecondsInChart > CurrentSongLength)
        {
            SetTimestamp(CurrentSongLength);
            Pause();
        }
    }

    void SyncTime()
    {
    }

    public void SetTimestamp(int time)
    {
        if (currentSong != -1)
        {
            audioSource.time = (time - Offset) / 1000f;
        }
        lastSyncTime = (int) (Time.time * 1000);
        lastSyncTimeInChart = time;
    }

    public void Play()
    {
        if (!IsPaused) return;
        if (currentSong != -1)
        {
            audioSource.time = (lastSyncTimeInChart - Offset) / 1000f;
            audioSource.Play();
        }
        lastSyncTime = (int) (Time.time * 1000);
        IsPaused = false;
    }

    public void Pause()
    {
        if (IsPaused) return;
        if (currentSong != -1)
        {
            audioSource.time = (MillisecondsInChart - Offset) / 1000f;
            audioSource.Pause();
        }
        lastSyncTimeInChart = MillisecondsInChart;
        IsPaused = true;
    }

    // Converts track position to a vector2 between 0,0 and 1,1
    public Vector2 TrackToUIPosition(int position, int time)
    {
        return new Vector2(position / 100f, (time - MillisecondsInChart) / (float)ViewSize);
    }

    public (int position, int time) UIToTrackPosition(Vector2 uiPosition)
    {
        return ((int) (uiPosition.x * 100), (int) (uiPosition.y * ViewSize + MillisecondsInChart));
    }
}
