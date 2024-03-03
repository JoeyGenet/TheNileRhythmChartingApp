using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlaybackUIController : MonoBehaviour
{
	[SerializeField] TMP_Text timestampText;
	[SerializeField] Button playButton;
	[SerializeField] Button pauseButton;
	[SerializeField] Slider timestampSlider;

    [Header("View")]
    [SerializeField] Slider viewSlider;
    public int MaxViewSize;
    public int MinViewSize;

    void Start()
    {
        playButton.onClick.AddListener(() => PlaybackManager.Instance.Play());
        pauseButton.onClick.AddListener(() => PlaybackManager.Instance.Pause());

        timestampSlider.value = PlaybackManager.Instance.MillisecondsInChart;
        timestampSlider.onValueChanged.AddListener(time => PlaybackManager.Instance.SetTimestamp((int) time));

        int songLength = PlaybackManager.Instance.CurrentSongLength;
        uint viewSize = PlaybackManager.Instance.ViewSize;
        viewSlider.value = viewSize < MaxViewSize 
            ? (viewSize - MinViewSize) / (float) (MaxViewSize-MinViewSize) / 2f
            : (viewSize - MaxViewSize) / (float) (songLength - MaxViewSize) / 2f + 0.5f;
        viewSlider.onValueChanged.AddListener(v => PlaybackManager.Instance.ViewSize = (uint) (
            v < 0.5f 
                ? v*2 * (MaxViewSize-MinViewSize) + MinViewSize
                : (v-0.5f) * (songLength - MaxViewSize) + MaxViewSize
        ));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (PlaybackManager.Instance.IsPaused) PlaybackManager.Instance.Play();
            else PlaybackManager.Instance.Pause();
        }

        timestampSlider.maxValue = PlaybackManager.Instance.CurrentSongLength;
        
        bool isPaused = PlaybackManager.Instance.IsPaused;
        playButton.gameObject.SetActive(isPaused);
        pauseButton.gameObject.SetActive(!isPaused);

        // Update current timestamp
        int timestamp = PlaybackManager.Instance.MillisecondsInChart;
        int minutes = timestamp / 60000;
        int seconds = timestamp%60000 / 1000;
        int milliseconds = timestamp%1000;
        timestampText.text = $"{minutes}:{seconds:00}.{milliseconds:000}";
        timestampSlider.SetValueWithoutNotify(timestamp);
    }
}
