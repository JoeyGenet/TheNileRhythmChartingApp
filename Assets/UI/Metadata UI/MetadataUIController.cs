using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MetadataUIController : MonoBehaviour
{
    [Header("Image")]
    [SerializeField] EventRelay loadImageEvent;
    [SerializeField] RawImage coverImage;

    [Header("Song")]
    [SerializeField] EventRelay loadSongEvent;
    [SerializeField] TMP_Text songText;
    string defaultSongText;

    [Header("Names")]
    [SerializeField] TMP_InputField titleInputField;
    [SerializeField] TMP_InputField artistInputField;
    [SerializeField] TMP_InputField charterInputField;

    [Header("Charts")]
    [SerializeField] RectTransform chartInspectorsParent;
    [SerializeField] ChartInspector chartInspectorPrefab;
    [SerializeField] Button addChartButton;
    readonly SortedDictionary<EditableChart, ChartInspector> chartInspectors = new();

    [Header("Song Config")]
    [SerializeField] TMP_InputField bpmInputField;
    [SerializeField] TMP_InputField bpmOffsetInputField;
    [SerializeField] Slider gainSlider;
    [SerializeField] TMP_Text gainText;
    [SerializeField] EventRelay gainResetEvent;

    [Header("Preview")]
    [SerializeField] TMP_InputField previewStartField;
    [SerializeField] TMP_InputField previewDurationField;

    void Awake()
    {
        loadImageEvent.AddListener(LoadImage);

        loadSongEvent.AddListener(LoadSong);
        defaultSongText = songText.text;

        titleInputField.onValueChanged.AddListener(title =>
            ChartManager.ChartPackage.Title = titleInputField.text = title.Replace("\n", ""));

        artistInputField.onValueChanged.AddListener(artist =>
            ChartManager.ChartPackage.Artist = artistInputField.text = artist.Replace("\n", ""));

        charterInputField.onValueChanged.AddListener(charter =>
            ChartManager.ChartPackage.Charter = charterInputField.text = charter.Replace("\n", ""));

        addChartButton.onClick.AddListener(() => ChartManager.ChartPackage.AddChart());
        
        bpmInputField.onValueChanged.AddListener(bpmStr =>
        {
            if (int.TryParse(bpmStr, out int bpm)) ChartManager.ChartPackage.BPM = (uint) Math.Abs(bpm);
        });

        bpmOffsetInputField.onValueChanged.AddListener(offsetStr =>
        {
            if (int.TryParse(offsetStr, out int offset)) ChartManager.ChartPackage.BPMOffset = offset;
        });

        gainSlider.onValueChanged.AddListener(gain => ChartManager.ChartPackage.Gain = gain);
        gainResetEvent.AddListener(() => gainSlider.value = 0);

        previewStartField.onValidateInput = (text, charIndex, addedChar) =>
        {
            if (char.IsDigit(addedChar) || 
                (addedChar == ':' && !text.Contains(':') && !text[..Math.Min(charIndex+1, text.Length)].Contains('.')) ||
                (addedChar == '.' && !text.Contains('.') && !text[Math.Max(charIndex-1, 0)..].Contains(':')))
            {
                return addedChar;
            }
            return '\0';
        };
        previewStartField.onValueChanged.AddListener(value =>
        {
            if (value == "") return;

            uint minutes = 0, seconds = 0, milliseconds = 0;

            int colonPos = value.IndexOf(':');
            if (colonPos != -1) uint.TryParse(value[..colonPos], out minutes);

            if (double.TryParse(value[(colonPos+1)..], out double v))
            {
                seconds = (uint) v;
                milliseconds = (uint) (v * 1000) % 1000;
            }

            ChartManager.ChartPackage.PreviewStart = minutes * 60000 + seconds * 1000 + milliseconds;
        });

        previewDurationField.onValueChanged.AddListener(value =>
        {
            previewDurationField.text = value = value.Replace("-", "");
            if (value == "") return;

            uint seconds = 0, milliseconds = 0;

            if (double.TryParse(value, out double v))
            {
                seconds = (uint) v;
                milliseconds = (uint) (v * 1000) % 1000;
            }

            ChartManager.ChartPackage.PreviewDuration = seconds * 1000 + milliseconds;
        });

        OnChartPackageChanged(null, ChartManager.ChartPackage);
        ChartManager.OnChartPackageChanged += OnChartPackageChanged;
        ChartManager.OnChartAdded += OnChartAdded;
        ChartManager.OnChartRemoved += OnChartRemoved;
    }

    void OnDestroy()
    {
        ChartManager.OnChartPackageChanged -= OnChartPackageChanged;

        ChartManager.OnChartAdded -= OnChartAdded;
        ChartManager.OnChartRemoved -= OnChartRemoved;
    }

    void Update()
    {
        var pkg = ChartManager.ChartPackage;

        coverImage.enabled = pkg.CoverImage != null;
        coverImage.texture = pkg.CoverImage;

        songText.text = pkg.SongName ?? defaultSongText;

        if (!titleInputField.isFocused) titleInputField.SetTextWithoutNotify($"{pkg.Title}");
        if (!artistInputField.isFocused) artistInputField.SetTextWithoutNotify($"{pkg.Artist}");
        if (!charterInputField.isFocused) charterInputField.SetTextWithoutNotify($"{pkg.Charter}");

        if (!bpmInputField.isFocused) bpmInputField.SetTextWithoutNotify($"{pkg.BPM}");
        if (!bpmOffsetInputField.isFocused) bpmOffsetInputField.SetTextWithoutNotify($"{pkg.BPMOffset}");

        if (!previewStartField.isFocused)
        {
            var (min, sec, ms) = ChartManager.ChartPackage.TimeToReadable((int) ChartManager.ChartPackage.PreviewStart);
            previewStartField.SetTextWithoutNotify($"{min}:{sec:00}.{ms:000}");
        }
        if (!previewDurationField.isFocused)
        {
            uint duration = ChartManager.ChartPackage.PreviewDuration;
            previewDurationField.SetTextWithoutNotify($"{duration/1000}.{duration%1000:000}");
        }

        gainSlider.SetValueWithoutNotify(pkg.Gain);
        gainText.text = $"{pkg.Gain:+#;-#;0}";
    }
    
    void LoadImage() => StandaloneFileBrowser.OpenFilePanelAsync("Choose Image", ChartManager.ChartDir, "", false, async paths =>
    {
        if (paths.Length == 0 || string.IsNullOrEmpty(paths[0])) return; // User pressed cancel. Don't load
        if (!File.Exists(paths[0]))
        {
            UIManager.Instance.ShowAlert("File Path Invalid or Does Not Exist", "No image will be loaded.");
            return;
        }

        try
        {
            await ChartManager.ChartPackage.SetCoverImage(paths[0]);
        }
        catch (Exception e)
        {
            UIManager.Instance.ShowAlert("Error Loading Image", $"Loading from {paths[0]}.\n{e.Message}");
            Debug.LogException(e);
        }
    });

    void LoadSong() => StandaloneFileBrowser.OpenFilePanelAsync("Choose Song", ChartManager.ChartDir, "", false, async paths =>
    {
        if (paths.Length == 0 || string.IsNullOrEmpty(paths[0])) return; // User pressed cancel. Don't load
        if (!File.Exists(paths[0]))
        {
            UIManager.Instance.ShowAlert("File Path Invalid or Does Not Exist", "No song will be loaded.");
            return;
        }

        try
        {
            await ChartManager.ChartPackage.SetSong(paths[0]);
        }
        catch (Exception e)
        {
            UIManager.Instance.ShowAlert("Error Loading Song", $"Loading from {paths[0]}.\n{e.Message}");
            Debug.LogException(e);
        }
    });

    void OnChartPackageChanged(EditableChartPackage prevPackage, EditableChartPackage newPackage)
    {
        // Update chart list
        for (int i = chartInspectorsParent.childCount-1; i >= 0; i--)
        {
            Destroy(chartInspectorsParent.GetChild(i).gameObject);
        }
        chartInspectors.Clear();
        foreach (var chart in newPackage.Charts) OnChartAdded(chart);

        // Select the first chart of the new package
        ChartManager.SelectedChart = newPackage.Charts.First;
        // ChartManager.SelectedChart = newPackage.Charts.OrderBy() chartInspectorsParent.GetChild(0).GetComponent<ChartInspector>().Chart;
    }

    void OnChartAdded(EditableChart chart)
    {
        var chartInspector = Instantiate(chartInspectorPrefab, chartInspectorsParent);
        chartInspector.Init(this, chart);
        chartInspectors[chart] = chartInspector;
        UpdateChartInspectorsOrder();
        ChartManager.SelectedChart = chart;
    }

    void OnChartRemoved(EditableChart chart)
    {
        var chartInspector = chartInspectors[chart];
        chartInspectors.Remove(chart);

        int newSelectedIndex = Math.Clamp(chartInspector.transform.GetSiblingIndex()-1, 0, chartInspectors.Count-1);
        if (newSelectedIndex == chartInspector.transform.GetSiblingIndex()) newSelectedIndex++;
        ChartManager.SelectedChart = chartInspectorsParent.GetChild(newSelectedIndex).GetComponent<ChartInspector>().Chart;
        Destroy(chartInspector.gameObject);
    }

    public void UpdateChartInspectorsOrder()
    {
        // var charts = chartInspectors.Keys.ToList();
        // charts.Sort((a, b) => a.DifficultyRating == b.DifficultyRating ? 
        //     a.DifficultyName.CompareTo(b.DifficultyName) : a.DifficultyRating - b.DifficultyRating);
        var charts = chartInspectors.Keys;
        int index = 0;
        foreach (var chart in charts)
        {
            chartInspectors[chart].transform.SetSiblingIndex(index++);
        }
    }
}
