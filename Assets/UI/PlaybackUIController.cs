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
	public int Timestamp { get; private set; }
    
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
    }

    public void SetTimestamp(float newTimestamp)
	{
        int intTimestamp = (int)newTimestamp;
        Timestamp = intTimestamp;
        int minutes = intTimestamp / 60000;
        int seconds = intTimestamp%60000 / 1000;
        int milliseconds = intTimestamp%1000;
        timestampText.text = $"{minutes}:{seconds}.{milliseconds}";
	}
}
