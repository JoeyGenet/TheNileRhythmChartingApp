using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BpmChangeInspector : MonoBehaviour
{
    public EditableBpmChangeEvent Event { get; private set; }

    [SerializeField] TMP_InputField startTimeInput;
    private int editStartStartTime;
    [SerializeField] TMP_InputField bpmInput;
    private uint editStartBpm;
    private bool isEditInProgress = false;

    public void Show(EditableBpmChangeEvent evt)
    {
        Event = evt;
        gameObject.SetActive(Event != null);

        bool isChartBpm = Event is ChartBpm;
        startTimeInput.gameObject.SetActive(!isChartBpm);
        bpmInput.gameObject.SetActive(!isChartBpm);
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
                EditBpmChangeAction editBpmChangeAction = new EditBpmChangeAction (Event, startTimeDelta, 0);
                editBpmChangeAction.CommitAction();
                isEditInProgress = false;
            }
        });

        bpmInput.onValidateInput += (text, _, c) => c == '-' ? '\0' : c;
        bpmInput.onValueChanged.AddListener(text =>
        {
            // track starting value of edit
            if (!isEditInProgress)
            {
                editStartBpm = Event.BPM;
                isEditInProgress = true;
            }
            // process new value
            if (uint.TryParse(text, out uint bpm))
            {
                Event.BPM = bpm;
            }
        });
        bpmInput.onEndEdit.AddListener(Text =>
        {
            if (isEditInProgress)
            {
                uint bpmDelta = Event.BPM - editStartBpm;
                EditBpmChangeAction editBpmChangeAction = new EditBpmChangeAction (Event, 0, bpmDelta);
                editBpmChangeAction.CommitAction();
                isEditInProgress = false;
            }
        });
    }

    void Update()
    {
        if (Event == null) return;
        
        if (!startTimeInput.isFocused) startTimeInput.SetTextWithoutNotify($"{Event.StartTime}");
        if (!bpmInput.isFocused) bpmInput.SetTextWithoutNotify($"{Event.BPM}");
    }
}
