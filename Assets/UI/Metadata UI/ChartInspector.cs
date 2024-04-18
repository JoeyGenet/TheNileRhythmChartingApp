using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ChartInspector : MonoBehaviour
{
    MetadataUIController metadataUI;
    public EditableChart Chart { get; private set; }

    [SerializeField] TMP_InputField ratingInput;
    [SerializeField] TMP_InputField nameInput;
    [SerializeField] Button editButton;
    [SerializeField] Button deleteButton;

    public void Init(MetadataUIController metadataUI, EditableChart chart)
    {
        this.metadataUI = metadataUI;
        Chart = chart;
    }

    void Awake()
    {
        ratingInput.onValueChanged.AddListener(ratingStr =>
        {
            ratingInput.text = ratingStr = ratingStr.Replace("-", "");
            if (int.TryParse(ratingStr, out int rating)) Chart.DifficultyRating = Math.Abs(rating);
        });
        ratingInput.onEndEdit.AddListener(_ => metadataUI.UpdateChartInspectorsOrder());

        nameInput.onValueChanged.AddListener(name => Chart.DifficultyName = name);
        nameInput.onEndEdit.AddListener(_ => metadataUI.UpdateChartInspectorsOrder());

        editButton.onClick.AddListener(() =>
        {
            ChartManager.SelectedChart = Chart;
        });

        deleteButton.onClick.AddListener(() =>
        {
            var dialog = UIManager.Instance.ShowConfirm("Delete Chart?", "Are you sure you want to delete this chart?");
            dialog.OnOkPressed?.AddListener(() => Chart.Package.RemoveChart(Chart));
        });
    }

    void Update()
    {
        bool isSelected = ChartManager.SelectedChart == Chart;
        editButton.gameObject.SetActive(!isSelected);
        deleteButton.gameObject.SetActive(isSelected);
        deleteButton.interactable = ChartManager.ChartPackage.Charts.Count > 1;

        if (!ratingInput.isFocused) ratingInput.text = $"{Chart.DifficultyRating}";
        if (!nameInput.isFocused) nameInput.text = Chart.DifficultyName;
    }
}
