using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnvironmentChangeInspector : MonoBehaviour
{
    public EditableEnvironmentChangeEvent Event { get; private set; }

    [SerializeField] TMP_InputField startTimeInput;
    private int editStartStartTime;
    [SerializeField] TMP_InputField endTimeInput;
    private int editStartEndTime;
    [SerializeField] TMP_Dropdown environmentDropdown;
    private Environment editStartEnvironment;
    private bool isEditInProgress = false;

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
            // track starting value of edit
            if (!isEditInProgress)
            {
                editStartStartTime = Event.StartTime;
                isEditInProgress = true;
            }
            // process new value
            if (int.TryParse(text, out int time))
            {
                Event.StartTime = time;
            }
        });
        startTimeInput.onEndEdit.AddListener(Text =>
        {
            if (isEditInProgress)
            {
                int startTimeDelta = Event.StartTime - editStartStartTime;
                EditEnvironmentChangeAction editEnvironmentChangeAction = new EditEnvironmentChangeAction
                (
                    Event, startTimeDelta, 0, Event.Environment, Event.Environment
                );
                editEnvironmentChangeAction.CommitAction();
                isEditInProgress = false;
            }
        });

        endTimeInput.onValidateInput += (text, _, c) => c == '-' ? '\0' : c;
        endTimeInput.onValueChanged.AddListener(text =>
        {
            // track starting value of edit
            if (!isEditInProgress)
            {
                editStartStartTime = Event.StartTime;
                editStartEndTime = Event.EndTime;
                editStartEnvironment = Event.Environment;
                isEditInProgress = true;
            }
            // process new value
            if (int.TryParse(text, out int time))
            {
                Event.EndTime = time;
            }
        });
        endTimeInput.onEndEdit.AddListener(Text =>
        {
            if (isEditInProgress)
            {
                int endTimeDelta = Event.EndTime - editStartEndTime;
                EditEnvironmentChangeAction editEnvironmentChangeAction = new EditEnvironmentChangeAction
                (
                    Event, 0, endTimeDelta, Event.Environment, Event.Environment
                );
                editEnvironmentChangeAction.CommitAction();
                isEditInProgress = false;
            }
        });

        environmentDropdown.options = EnvironmentManager.Environments
            .Select(env => new TMP_Dropdown.OptionData(env.DisplayName)).ToList();
        environmentDropdown.onValueChanged.AddListener(value =>
        {
            Environment oldEnvironment = Event.Environment;
            Event.Environment = EnvironmentManager.Environments[value];
            EditEnvironmentChangeAction editEnvironmentChangeAction = new EditEnvironmentChangeAction
            (
                Event, 0, 0, oldEnvironment, Event.Environment
            );
            editEnvironmentChangeAction.CommitAction();
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
