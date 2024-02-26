using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

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
	[SerializeField] TMP_InputField BPMInputField;
	public int BPM { get; private set; }

    [Header("Offset")]
    [SerializeField] private int defaultOffset;
    [SerializeField] TMP_Text offsetText;
	[SerializeField] TMPro.TMP_InputField offsetInputField;
	public int Offset { get; private set; }

    [Header("Alert Dialog")]
    [SerializeField] AlertDialog alertDialogPrefab;

    void Awake()
    {
        Instance = this;

        songLength = defaultSongLength;
    }

    void Start()
    {
        BPMInputField.onValueChanged.AddListener(SetBPM);
        offsetInputField.onValueChanged.AddListener(SetOffset);
        timestampSlider.onValueChanged.AddListener(SetTimestamp);
    }

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
            Offset = newInt;
        }
        else
        {
            offsetInputField.text = Offset.ToString();
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

    public AlertDialog ShowAlert(string title, string message)
    {
        var alert = Instantiate(alertDialogPrefab, transform);
        alert.Title = title;
        alert.Message = message;
        return alert;
    }
}
