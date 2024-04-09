using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlaybackUIController : MonoBehaviour
{
    [Header("Playback")]
	[SerializeField] TMP_Text timestampText;
	[SerializeField] TMP_Text beatText;
	[SerializeField] EventRelay playEvent;
	[SerializeField] EventRelay pauseEvent;
	[SerializeField] Slider timestampSlider;

    [Header("Audio")]
	[SerializeField] TMP_InputField audioOffsetInputField;
	[SerializeField] Slider masterVolumeSlider;

    [Header("View")]
    [SerializeField] Slider horizontalViewSlider;
    [SerializeField] Slider verticalViewSlider;
    public int MaxVerticalView;
    public int MinVerticalView;

    void Start()
    {
        playEvent.AddListener(() => PlaybackManager.Instance.Play());
        pauseEvent.AddListener(() => PlaybackManager.Instance.Pause());

        timestampSlider.value = PlaybackManager.Instance.MillisecondsInChart;
        timestampSlider.onValueChanged.AddListener(time => PlaybackManager.Instance.SetTimestamp((int) time));

        audioOffsetInputField.text = $"{PlaybackManager.Instance.Offset}";
        audioOffsetInputField.onValueChanged.AddListener(offsetStr =>
        {
            if (int.TryParse(offsetStr, out var offset))
                PlaybackManager.Instance.Offset = offset;
        });

        masterVolumeSlider.value = PlaybackManager.Instance.MasterVolume;
        masterVolumeSlider.onValueChanged.AddListener(value => PlaybackManager.Instance.MasterVolume = value);

        uint songLength = ChartManager.ChartPackage.SongLength;
        uint viewSize = PlaybackManager.Instance.VerticalViewSize;
        verticalViewSlider.value = viewSize < MaxVerticalView 
            ? (viewSize - MinVerticalView) / (float) (MaxVerticalView-MinVerticalView) / 2f
            : (viewSize - MaxVerticalView) / (float) (songLength - MaxVerticalView) / 2f + 0.5f;
        verticalViewSlider.onValueChanged.AddListener(v => PlaybackManager.Instance.VerticalViewSize = (uint) (
            v < 0.5f 
                ? v*2 * (MaxVerticalView-MinVerticalView) + MinVerticalView
                : (v-0.5f) * (songLength - MaxVerticalView) + MaxVerticalView
        ));

        horizontalViewSlider.onValueChanged.AddListener(value => PlaybackManager.Instance.HorizontalViewSize = value);
    }

    void Update()
    {
        timestampSlider.maxValue = ChartManager.ChartPackage.SongLength;
        // timestampSlider.value = Mathf.Clamp(timestampSlider.value, 0, songLength);
        
        bool isPaused = PlaybackManager.Instance.IsPaused;
        playEvent.gameObject.SetActive(isPaused);
        pauseEvent.gameObject.SetActive(!isPaused);

        // Update current timestamp
        int timestamp = PlaybackManager.Instance.MillisecondsInChart;
        int minutes = timestamp / 60000;
        int seconds = timestamp%60000 / 1000;
        int milliseconds = timestamp%1000;
        timestampText.text = $"Time: {minutes}:{seconds:00}.{milliseconds:000}";
        beatText.text = $"Beat: {PlaybackManager.Instance.BeatsInChart:0.000}";
        timestampSlider.SetValueWithoutNotify(timestamp);
    }
}
