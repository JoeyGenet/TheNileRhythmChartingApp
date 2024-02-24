using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Playback")]
    [SerializeField] private int defaultSongLength;
    [SerializeField] int defaultTimestamp = 0; // in m/s
	[SerializeField] TMP_Text timestampText;
	[SerializeField] Slider timestampSlider;
    public int songLength { get; private set; }
	public int timestamp { get; private set; }

    [Header("BPM")]
    [SerializeField] private int defaultBPM;
    [SerializeField] TMP_Text BPMText;
	[SerializeField] TMPro.TMP_InputField BPMInputField;
	public int BPM { get; private set; }

    [Header("Offset")]
    [SerializeField] private int defaultOffset;
    [SerializeField] TMP_Text offsetText;
	[SerializeField] TMPro.TMP_InputField offsetInputField;
	public int offset { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        BPMInputField.onValueChanged.AddListener(SetBPM);
        offsetInputField.onValueChanged.AddListener(SetOffset);
        timestampSlider.onValueChanged.AddListener(SetTimestamp);

        songLength = defaultSongLength;
    }

    // Update is called once per frame
    void Update()
    {
        timestampSlider.maxValue = songLength;
    }

    public void SetBPM(string newBPM)
	{
        int newInt;
        bool success = int.TryParse(newBPM, out newInt);
        if (success)
        {
            BPM = newInt;
        }
        else
        {
            BPMInputField.text = BPM.ToString();
        }
	}

    public void SetOffset(string newOffset)
	{
        int newInt;
        bool success = int.TryParse(newOffset, out newInt);
        if (success)
        {
            offset = newInt;
        }
        else
        {
            offsetInputField.text = offset.ToString();
        }
	}

    public void SetTimestamp(float newTimestamp)
	{
        int intTimestamp = (int)newTimestamp;
        timestamp = intTimestamp;
        int minutes = intTimestamp / 60000;
        int seconds = (intTimestamp%60000) / 1000;
        int milliseconds = intTimestamp%1000;
        timestampText.text = $"{minutes}:{seconds}.{milliseconds}";
	}
}
