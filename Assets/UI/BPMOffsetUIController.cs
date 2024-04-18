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
        bpmInputField.onEndEdit.AddListener(SetBPM);
        offsetInputField.onEndEdit.AddListener(SetOffset);
    }

    void SetBPM(string newBPM)
	{   
        if (int.TryParse(newBPM, out int bpmInt))
        {
            ChartManager.ChartPackage.BPM = (uint) (bpmInt < 1 ? 1 : bpmInt);
        }
	}

    void SetOffset(string newOffset)
	{
        if (int.TryParse(newOffset, out int offsetInt))
        {
            ChartManager.ChartPackage.BPMOffset = offsetInt;
        }
	}

    void Update()
    {
        if (!bpmInputField.isFocused) bpmInputField.text = $"{ChartManager.ChartPackage.BPM}";
        if (!offsetInputField.isFocused) offsetInputField.text = $"{ChartManager.ChartPackage.BPMOffset}";
    }
}
