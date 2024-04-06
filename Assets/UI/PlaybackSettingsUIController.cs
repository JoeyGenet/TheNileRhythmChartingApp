using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class PlaybackSettingsUIController : MonoBehaviour
{
    [SerializeField] TMP_Text songPathText;
    [SerializeField] Button loadSongButton;
    [SerializeField] Button clearSongButton;
    
    [SerializeField] TMP_InputField offsetInput;

    [SerializeField] Slider gainSlider;
    [SerializeField] TMP_Text gainText;
    [SerializeField] AudioMixer audioMixer;

    string defaultSongPathText;

    void Awake()
    {
        defaultSongPathText = songPathText.text;
    }
    
    void Start()
    {
        loadSongButton.onClick.AddListener(LoadSong);
        clearSongButton.onClick.AddListener(() => PlaybackManager.Instance.ClearSong());

        offsetInput.onEndEdit.AddListener(text =>
        {
            if (int.TryParse(text, out int offset))
            {
                PlaybackManager.Instance.Offset = offset;
            }
        });

        gainSlider.onValueChanged.AddListener(value =>
        {  
            audioMixer.SetFloat("SongGain", value);
        });
        if (audioMixer.GetFloat("SongGain", out float gain))
        {
            gainSlider.value = gain;
        }
    }

    void LoadSong()
    {
        StandaloneFileBrowser.OpenFilePanelAsync("Load Song", ChartManager.ChartDir, "", false, async paths =>
        {
            if (paths.Length == 0 || string.IsNullOrEmpty(paths[0])) return; // User pressed cancel. Don't load
            if (!File.Exists(paths[0]))
            {
                UIManager.Instance.ShowAlert("File Path Invalid or Does Not Exist", "No song will be loaded.");
                return;
            }

            try 
            {
                await PlaybackManager.Instance.LoadSong(paths[0]);
            }
            catch (Exception e)
            {
                UIManager.Instance.ShowAlert("Error Loading Song", $"Loading from {paths[0]}.\n{e.Message}");
                Debug.LogException(e);
            }
        });
        }

    // Update is called once per frame
    void Update()
    {
        string path = PlaybackManager.Instance.SongPath;
        bool hasPath = !string.IsNullOrEmpty(path);
        songPathText.text = hasPath ? path : defaultSongPathText;

        clearSongButton.interactable = hasPath;

        if (!offsetInput.isFocused) offsetInput.text = $"{PlaybackManager.Instance.Offset}";

        gainText.text = $"{gainSlider.value:0.0}";
    }
}
