using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class EnvironmentTrackColor : MonoBehaviour
{
    RectTransform rect;

    [SerializeField] Image startColor;
    [SerializeField] Image endColor;

    public Color StartColor
    {
        get => startColor.color;
        set => startColor.color = value;
    }
    public Color EndColor
    {
        get => endColor.color;
        set => endColor.color = value;
    }

    public int StartTime;
    public int EndTime;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    void Update()
    {
        // Update line position
        Vector2 startUIPos = PlaybackManager.Instance.TrackToUIPosition(50, StartTime);
        Vector2 endUIPos = PlaybackManager.Instance.TrackToUIPosition(50, EndTime);
        rect.anchorMin = new Vector2(rect.anchorMin.x, startUIPos.y);
        rect.anchorMax = new Vector2(rect.anchorMax.x, endUIPos.y);
    }
}
