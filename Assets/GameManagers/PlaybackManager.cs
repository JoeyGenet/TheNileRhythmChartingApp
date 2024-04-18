using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
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

    [SerializeField] AudioMixer audioMixer;

    public int MillisecondsInChart => IsPaused 
        ? lastSyncTimeInChart
        : (int)(Time.time * 1000) - lastSyncTime + lastSyncTimeInChart;

    public float BeatsInChart => ChartManager.ChartPackage.TimeToBeat(MillisecondsInChart);

    int offset = 0;
    public int Offset
    {
        get => offset;
        set
        {
            offset = value;
            audioSource.time = (MillisecondsInChart - offset) / 1000f;
        }
    }

    public float MasterVolume = 1;

    public uint VerticalViewSize = 5000; // Number of milliseconds the view contains
    public float HorizontalViewSize = 1;

    // public AudioClip CurrentSong => audioSource.clip;
    // public int CurrentSongLength => CurrentSong == null
    //     ? ChartManager.SelectedChart.Notes.Aggregate(10000, (a, n) => Math.Max(a, n.EndTime))
    //     : (int) (CurrentSong.length * 1000);
    
    AudioSource audioSource;

    void Awake()
    {
        Instance = this;

        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        audioSource.clip = ChartManager.ChartPackage.SongClip;
        uint songLength = ChartManager.ChartPackage.SongLength;

        if (!IsPaused)
        {
            int currentTime = (int)(Time.time * 1000);
            if (currentTime - lastSyncTime > syncInterval)
            {
                lastSyncTimeInChart = audioSource.clip != null ? (int) (audioSource.time * 1000) + Offset : MillisecondsInChart;
                lastSyncTime = currentTime;
            }
            if (MillisecondsInChart > songLength)
            {
                SetTimestamp((int) songLength);
                Pause();
            }
        }

        audioMixer.SetFloat("SongGain", ChartManager.ChartPackage.Gain);
        audioMixer.SetFloat("MasterVolume", 20 * Mathf.Log10(Mathf.Max(MasterVolume, 0.0001f)));
    }

    public void SetTimestamp(int time)
    {
        time = Math.Clamp(time, 0, (int) ChartManager.ChartPackage.SongLength);
        if (audioSource.clip != null)
        {
            audioSource.time = (time - Offset) / 1000f;
        }
        lastSyncTime = (int) (Time.time * 1000);
        lastSyncTimeInChart = time;
    }

    public void Play()
    {
        if (!IsPaused) return;
        if (audioSource.clip != null)
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
        if (audioSource.clip != null)
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
        return new Vector2(position / 100f, (time - MillisecondsInChart) / (float)VerticalViewSize);
    }

    public (int position, int time) UIToTrackPosition(Vector2 uiPosition)
    {
        return ((int) (uiPosition.x * 100), (int) (uiPosition.y * VerticalViewSize + MillisecondsInChart));
    }
}
