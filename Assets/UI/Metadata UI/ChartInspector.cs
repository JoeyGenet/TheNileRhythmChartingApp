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
    private int editStartRating;
    [SerializeField] TMP_InputField nameInput;
    private string editStartName;
    [SerializeField] Button editButton;
    [SerializeField] Button deleteButton;
    private bool isEditInProgress = false;

    public void Init(MetadataUIController metadataUI, EditableChart chart)
    {
        this.metadataUI = metadataUI;
        Chart = chart;
    }

    void Awake()
    {
        ratingInput.onValueChanged.AddListener(ratingStr =>
        {
            // track starting value of edit
            if (!isEditInProgress)
            {
                editStartRating = Chart.DifficultyRating;
                isEditInProgress = true;
            }
            // process new value
            ratingInput.text = ratingStr = ratingStr.Replace("-", "");
            if (int.TryParse(ratingStr, out int rating)) Chart.DifficultyRating = Math.Abs(rating);
        });
        ratingInput.onEndEdit.AddListener(_ => 
        {
            if (isEditInProgress)
            {
                int ratingDelta = Chart.DifficultyRating - editStartRating;
                EditChartAction editChartAction = new EditChartAction(Chart, ratingDelta, Chart.DifficultyName, Chart.DifficultyName);
                editChartAction.CommitAction();
                isEditInProgress = false;
            }
            metadataUI.UpdateChartInspectorsOrder();
        });

        nameInput.onValueChanged.AddListener(name => 
        {
            // track starting value of edit
            if (!isEditInProgress)
            {
                editStartName = Chart.DifficultyName;
                isEditInProgress = true;
            }
            // process new value
            Chart.DifficultyName = name;
        });
        nameInput.onEndEdit.AddListener(_ => 
        {
            if (isEditInProgress)
            {
                EditChartAction editChartAction = new EditChartAction(Chart, 0, editStartName, Chart.DifficultyName);
                editChartAction.CommitAction();
                isEditInProgress = false;
            }
            metadataUI.UpdateChartInspectorsOrder();
        });

        editButton.onClick.AddListener(() =>
        {
            SwitchChartAction switchChartAction = new SwitchChartAction(ChartManager.SelectedChart, Chart);
            switchChartAction.CommitAction();
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
