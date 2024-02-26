using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Track : MonoBehaviour
{
    [Header("Track Areas")]
    [SerializeField] RectTransform mainTrackArea;
    [SerializeField] RectTransform leverTrackArea;

    [Header("Note Prefabs")]
    [SerializeField] WheelFlickRenderer wheelFlickPrefab;

    void Awake()
    {
        SaveManager.OnChartLoaded += Reload;
    }

    void OnDestroy()
    {
        SaveManager.OnChartLoaded -= Reload;
    }

    void Start()
    {
        Reload();
    }

    void Reload(string _ = "")
    {
        foreach (Note note in SaveManager.Instance.Chart.Notes)
        {
            switch (note)
            {
                case WheelFlick flick:
                    var wheelFlickRenderer = Instantiate(wheelFlickPrefab, mainTrackArea);
                    wheelFlickRenderer.Note = flick;
                    break;
                default:
                    throw new System.Exception($"Spawning of note {note} not supported");
            }
        }
    }
}
