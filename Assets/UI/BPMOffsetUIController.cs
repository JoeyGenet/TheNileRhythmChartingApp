using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BPMOffsetUIController : MonoBehaviour
{
    [Header("BPM")]
    [SerializeField] TMP_Text bpmText;
	[SerializeField] TMP_InputField bpmInputField;

    [Header("Offset")]
    [SerializeField] TMP_Text offsetText;
	[SerializeField] TMP_InputField offsetInputField;

    void Awake()
    {
        bpmInputField.onValueChanged.AddListener(SetBPM);
        offsetInputField.onValueChanged.AddListener(SetOffset);
    }

    void SetBPM(string newBPM)
	{   
        if (uint.TryParse(newBPM, out uint bpmInt))
        {
            SaveManager.Instance.Chart.BPM = bpmInt;
        }
	}

    void SetOffset(string newOffset)
	{
        if (int.TryParse(newOffset, out int offsetInt))
        {
            SaveManager.Instance.Chart.Offset = offsetInt;
        }
	}

    void Update()
    {
        offsetInputField.text = $"{SaveManager.Instance.Chart.Offset}";
        bpmInputField.text = $"{SaveManager.Instance.Chart.BPM}";
    }
}
