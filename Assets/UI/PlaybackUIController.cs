using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlaybackUIController : MonoBehaviour
{
    [SerializeField] private int defaultSongLength;
    [SerializeField] int defaultTimestamp = 0; // in m/s
	[SerializeField] TMP_Text timestampText;
	[SerializeField] Slider timestampSlider;
    public int SongLength { get; private set; }

    [Header("View")]
    [SerializeField] Slider viewSlider;
    public int MaxViewSize;
    public int MinViewSize;
    
    void Awake()
    {
        SongLength = defaultSongLength;
    }

    void Start()
    {
        timestampSlider.value = PlaybackManager.Instance.MillisecondsInChart;
        timestampSlider.onValueChanged.AddListener(time => PlaybackManager.Instance.MillisecondsInChart = (int) time);

        viewSlider.value = PlaybackManager.Instance.ViewSize;
        viewSlider.onValueChanged.AddListener(v => PlaybackManager.Instance.ViewSize = (uint) (
            v < 0.5f 
                ? v*2 * (MaxViewSize-MinViewSize) + MinViewSize
                : (v-0.5f) * (SongLength - MaxViewSize) + MaxViewSize
        ));
    }

    void Update()
    {
        timestampSlider.maxValue = SongLength;

        int timestamp = PlaybackManager.Instance.MillisecondsInChart;
        int minutes = timestamp / 60000;
        int seconds = timestamp%60000 / 1000;
        int milliseconds = timestamp%1000;
        timestampText.text = $"{minutes}:{seconds:00}.{milliseconds:000}";
        timestampSlider.SetValueWithoutNotify(timestamp);
    }
}
