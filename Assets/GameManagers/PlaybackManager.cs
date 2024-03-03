using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(AudioSource))]
public class PlaybackManager : MonoBehaviour
{
    public static PlaybackManager Instance;

    public bool IsPaused { get; private set; } = true;
    
    int lastSyncTime = 0;
    int lastSyncTimeInChart = 0;

    [Tooltip("How often (in ms) the song playback is synced with the game time")]
    [SerializeField] int syncInterval = 500;

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

    public string SongPath { get; private set; } = null;
    public string SongDir => SongPath == null ? null : Path.GetDirectoryName(SongPath);

    public AudioClip CurrentSong => audioSource.clip;
    public int CurrentSongLength => CurrentSong == null
        ? SaveManager.Instance.Chart.Notes.Aggregate(10000, (a, n) => Math.Max(a, n.EndTime))
        : (int) (CurrentSong.length * 1000);
    
    AudioSource audioSource;

    void Awake()
    {
        Instance = this;

        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (!IsPaused)
        {
            int currentTime = (int)(Time.time * 1000);
            if (currentTime - lastSyncTime > syncInterval)
            {
                lastSyncTime = currentTime;
                lastSyncTimeInChart = (int) (audioSource.time * 1000) + Offset;

            }
            if (MillisecondsInChart > CurrentSongLength)
            {
                SetTimestamp(CurrentSongLength);
                Pause();
            }
        }
    }
    
    public void ClearSong()
    {
        audioSource.clip = null;
        Pause();
        SetTimestamp(0);
    }

    public async Task LoadSong(string path)
    {
        var audioType = path[(path.LastIndexOf('.') + 1)..].ToLower() switch
        {
            "wav" => AudioType.WAV,
            "mp3" or "mp2" => AudioType.MPEG,
            "ogg" => AudioType.OGGVORBIS,
            "aiff" => AudioType.AIFF,
            _ => throw new Exception($"The audio file format of {path} is not supported"),
        };

        using UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip("file://" + path, audioType);
        uwr.SendWebRequest();

        while (!uwr.isDone) await Task.Delay(5);
        audioSource.clip = null;

        if (uwr.result != UnityWebRequest.Result.Success)
        {
            throw new Exception(uwr.error);
        }
        else
        {
            audioSource.clip = DownloadHandlerAudioClip.GetContent(uwr);
            SongPath = path;
            Pause();
            SetTimestamp(0);
        }
    }

    public void SetTimestamp(int time)
    {
        if (CurrentSong != null)
        {
            audioSource.time = (time - Offset) / 1000f;
        }
        lastSyncTime = (int) (Time.time * 1000);
        lastSyncTimeInChart = time;
    }

    public void Play()
    {
        if (!IsPaused) return;
        if (CurrentSong != null)
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
        if (CurrentSong != null)
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
