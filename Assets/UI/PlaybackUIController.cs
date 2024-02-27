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
    
    void Awake()
    {
        SongLength = defaultSongLength;
    }

    void Start()
    {
        timestampSlider.onValueChanged.AddListener(SetTimestamp);
    }

    void Update()
    {
        timestampSlider.maxValue = SongLength;

        int timestamp = PlaybackManager.Instance.MillisecondsInChart;
        int minutes = timestamp / 60000;
        int seconds = timestamp%60000 / 1000;
        int milliseconds = timestamp%1000;
        timestampText.text = $"{minutes}:{seconds}.{milliseconds}";
        timestampSlider.SetValueWithoutNotify(timestamp);
    }

    public void SetTimestamp(float newTimestamp)
	{
        PlaybackManager.Instance.MillisecondsInChart = (int) newTimestamp;
	}
}
