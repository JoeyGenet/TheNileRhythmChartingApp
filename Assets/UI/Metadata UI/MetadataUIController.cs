using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

    [Header("Song Config")]
    [SerializeField] TMP_InputField bpmInputField;
    [SerializeField] TMP_InputField bpmOffsetInputField;
    [SerializeField] Slider gainSlider;
    [SerializeField] TMP_Text gainText;
    [SerializeField] EventRelay gainResetEvent;

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
}
