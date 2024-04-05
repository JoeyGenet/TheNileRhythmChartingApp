using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(Image))]
public class BeatMarker : MonoBehaviour
{
    public float Beat = 0;

    // If this beat has passed the judgement line, wrap around by this number of beats
    public float BeatWrap = 1;

    RectTransform rect;
    Image image;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
    }

    void Update()
    {
        float beatsInChart = PlaybackManager.Instance.BeatsInChart;
        float beat = Mathf.Ceil((beatsInChart-Beat) / BeatWrap) * BeatWrap + Beat;
        int time = SaveManager.Instance.ChartPackage.BeatToTime(beat);
        Vector2 uiPos = PlaybackManager.Instance.TrackToUIPosition(50, time);
        rect.anchorMax = new Vector2(rect.anchorMax.x, uiPos.y);
        rect.anchorMin = new Vector2(rect.anchorMin.x, uiPos.y);

        // Hide if view is too big to wrap around fully
        image.enabled = SaveManager.Instance.ChartPackage.BeatToTime(BeatWrap) > PlaybackManager.Instance.ViewSize;
        
    }
}
