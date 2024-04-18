using System;
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

    [Header("Snapping")]
    [SerializeField] TMP_Dropdown snappingDropdown;

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

        verticalViewSlider.onValueChanged.AddListener(v =>
        {
            uint songLength = ChartManager.ChartPackage.SongLength;
            uint maxVerticalView = Math.Min(songLength, (uint) MaxVerticalView);
            // Debug.Log(((v-0.5f) * (songLength - maxVerticalView) + maxVerticalView))
            PlaybackManager.Instance.VerticalViewSize = (uint) (v < 0.5f 
                ? v*2 * (maxVerticalView-MinVerticalView) + MinVerticalView
                : (v-0.5f)*2f * (songLength - maxVerticalView) + maxVerticalView
            );
        });

        horizontalViewSlider.onValueChanged.AddListener(value => PlaybackManager.Instance.HorizontalViewSize = value);

        snappingDropdown.onValueChanged.AddListener(value => UIManager.Snapping = value switch
        {
            0 => 0,
            1 => 1,
            2 => 2,
            3 => 3,
            4 => 4,
            5 => 6,
            6 => 8,
            _ => 1,
        });
    }

    void Update()
    {
        timestampSlider.maxValue = ChartManager.ChartPackage.SongLength;
        
        bool isPaused = PlaybackManager.Instance.IsPaused;
        playEvent.gameObject.SetActive(isPaused);
        pauseEvent.gameObject.SetActive(!isPaused);

        // Update current timestamp
        int time = PlaybackManager.Instance.MillisecondsInChart;
        var (min, sec, ms) = ChartManager.ChartPackage.TimeToReadable(time);
        timestampText.text = $"Time: {min}:{sec:00}.{ms:000}";
        beatText.text = $"Beat: {PlaybackManager.Instance.BeatsInChart:0.000}";
        timestampSlider.SetValueWithoutNotify(time);
        
        uint songLength = ChartManager.ChartPackage.SongLength;
        uint viewSize = PlaybackManager.Instance.VerticalViewSize;
        uint maxVerticalView = Math.Min(songLength, (uint) MaxVerticalView);
        // Debug.Log($"{songLength} {maxVerticalView}");
        float sliderValue = viewSize < maxVerticalView 
            ? (viewSize - MinVerticalView) / (float) (maxVerticalView-MinVerticalView) / 2f
            : (viewSize - maxVerticalView) / (float) (songLength - maxVerticalView) / 2f + 0.5f;
        if (Mathf.Abs(verticalViewSlider.value - sliderValue) > 0.001f)
            verticalViewSlider.value = sliderValue;
    }
}
