using System;
using System.Linq;
using TMPro;
using UnityEngine;

public class EnvironmentChangeInspector : MonoBehaviour
{
    public EditableEnvironmentChangeEvent Event { get; private set; }

    [SerializeField] TMP_InputField startTimeInput;
    [SerializeField] TMP_InputField endTimeInput;
    [SerializeField] TMP_Dropdown environmentDropdown;

    public void Show(EditableEnvironmentChangeEvent evt)
    {
        Event = evt;
        gameObject.SetActive(Event != null);

        bool isChartEnvironment = Event is ChartEnvironment;
        startTimeInput.gameObject.SetActive(!isChartEnvironment);
        endTimeInput.gameObject.SetActive(!isChartEnvironment);
    }

    void Start()
    {
        startTimeInput.onValidateInput += (text, _, c) => c == '-' ? '\0' : c;
        startTimeInput.onValueChanged.AddListener(text =>
        {
            if (int.TryParse(text, out int time))
            {
                Event.StartTime = time;
            }
        });

        endTimeInput.onValidateInput += (text, _, c) => c == '-' ? '\0' : c;
        endTimeInput.onValueChanged.AddListener(text =>
        {
            if (int.TryParse(text, out int time))
            {
                Event.EndTime = time;
            }
        });

        environmentDropdown.options = EnvironmentManager.Environments
            .Select(env => new TMP_Dropdown.OptionData(env.DisplayName)).ToList();
        environmentDropdown.onValueChanged.AddListener(value =>
        {
            Event.Environment = EnvironmentManager.Environments[value];
        });
    }

    void Update()
    {
        if (Event == null) return;
        
        if (!startTimeInput.isFocused) startTimeInput.SetTextWithoutNotify($"{Event.StartTime}");
        if (!endTimeInput.isFocused) endTimeInput.SetTextWithoutNotify($"{Event.EndTime}");
        environmentDropdown.SetValueWithoutNotify(Math.Max(EnvironmentManager.GetEnvironmentIndex(Event.Environment), 0));
    }
}
