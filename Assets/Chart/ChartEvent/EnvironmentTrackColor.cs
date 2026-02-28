using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class EnvironmentTrackColor : MonoBehaviour, IPointerClickHandler
{
    public EditableEnvironmentChangeEvent EnvironmentChange { get; private set; }

    [SerializeField] Image startColor;
    [SerializeField] Image endColor;
    [SerializeField] Image environmentColor;

    public void Init(EditableEnvironmentChangeEvent environment)
    {
        EnvironmentChange = environment;

        if (EnvironmentChange != null) EnvironmentChange.Chart.OnEventRemoved += DestroyOnNoteRemoved;
    }

    void Awake()
    {
        ChartManager.OnChartChanged += DestroyOnChartChanged;
    }

    void OnDestroy()
    {
        ChartManager.OnChartChanged -= DestroyOnChartChanged;
        if (EnvironmentChange != null) EnvironmentChange.Chart.OnEventRemoved -= DestroyOnNoteRemoved;
    }

    void Update()
    {
        // Update line position
        Vector2 startUIPos = PlaybackManager.Instance.TrackToUIPosition(50, EnvironmentChange?.StartTime ?? 0);
        Vector2 endUIPos = PlaybackManager.Instance.TrackToUIPosition(50, EnvironmentChange?.EndTime ?? 0);
        startColor.rectTransform.anchorMin = new Vector2(startColor.rectTransform.anchorMin.x, startUIPos.y);
        startColor.rectTransform.anchorMax = new Vector2(startColor.rectTransform.anchorMax.x, endUIPos.y);
        endColor.rectTransform.anchorMin = new Vector2(endColor.rectTransform.anchorMin.x, startUIPos.y);
        endColor.rectTransform.anchorMax = new Vector2(endColor.rectTransform.anchorMax.x, endUIPos.y);
        environmentColor.rectTransform.anchorMin = new Vector2(environmentColor.rectTransform.anchorMin.x, startUIPos.y);

        // Update colors
        startColor.color = (EnvironmentChange == null ? ChartManager.SelectedChart.Environment : EnvironmentChange.PreviousEnvironment).Color;
        endColor.color = environmentColor.color = (EnvironmentChange == null ? ChartManager.SelectedChart.Environment : EnvironmentChange.Environment).Color;
    }

    void DestroyOnChartChanged(EditableChart prevChart, EditableChart newChart)
    {
        if (EnvironmentChange != null)
        {
            Destroy(gameObject);
        }
    }

    void DestroyOnNoteRemoved(EditableEnvironmentChangeEvent evt)
    {
        if (evt == EnvironmentChange)
        {
            Destroy(gameObject);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        UIManager.SelectedElement = EnvironmentChange ?? new ChartEnvironment(ChartManager.SelectedChart);
        UIManager.SelectionGroup.ClearSelection();
    }
}
