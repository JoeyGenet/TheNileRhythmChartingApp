using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WheelSequenceInspector : NoteInspector<WheelSequence>
{
    [SerializeField] TMP_InputField startTimeInput;
    [SerializeField] TMP_InputField startPosInput;

    [Header("Wheel Turn")]
    [SerializeField] RectTransform wheelTurnInspectors;
    [SerializeField] WheelTurnInspector wheelTurnInspectorPrefab;
    [SerializeField] Button addTurnButton;

    protected override void Awake()
    {
        base.Awake();

        startTimeInput.onValidateInput += (text, _, c) => c == '-' ? '\0' : c;
        startTimeInput.onValueChanged.AddListener(text =>
        {
            if (int.TryParse(text, out int time))
            {
                Note.StartTime = Mathf.Clamp(time, 0, int.MaxValue); // TODO: clamp to song length
            }
        });

        startPosInput.onValidateInput += (text, _, c) => c == '-' ? '\0' : c;
        startPosInput.onValueChanged.AddListener(text =>
        {
            if (int.TryParse(text, out int pos))
            {
                Note.Pos = Mathf.Clamp(pos, 0, 100);
            }
        });

        addTurnButton.onClick.AddListener(() =>
        {
            int endTime = Note.EndTime + SaveManager.Instance.Chart.BeatToTime(1); // place 1 beat after last note
            // TODO: maybe place same distance as prev 2 notes?
            Debug.Log("Add button clicked");
            Note.AddNote(new WheelTurn(Note, endTime, 0));
        });

        WheelSequence.OnWheelTurnAdded += AddWheelTurnInspector;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        WheelSequence.OnWheelTurnAdded -= AddWheelTurnInspector;
    }

    protected override void OnShow()
    {
        // Update note list
        foreach (var inspector in wheelTurnInspectors.GetComponentsInChildren<WheelTurnInspector>())
        {
            DestroyImmediate(inspector.gameObject);
        }
        foreach (WheelTurn note in Note.Notes)
        {
            AddWheelTurnInspector(Note, note);
        }
    }

    void AddWheelTurnInspector(WheelSequence sequence, WheelTurn note)
    {
        Debug.Log($"Add inspector {note}");
        if (sequence == Note)
        {
            var wheelTurnInspector = Instantiate(wheelTurnInspectorPrefab, wheelTurnInspectors);
            wheelTurnInspector.Note = note;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (Note == null) return;

        if (!startTimeInput.isFocused) startTimeInput.SetTextWithoutNotify($"{Note.StartTime}");
        if (!startPosInput.isFocused) startPosInput.SetTextWithoutNotify($"{Note.Pos}");

        // TODO: optimize somehow?
        Dictionary<WheelTurn, WheelTurnInspector> inspectors = wheelTurnInspectors
            .GetComponentsInChildren<WheelTurnInspector>()
            .ToDictionary(i => i.Note, i => i);
        foreach (var (turn, i) in Note.Notes.Select((note, i) => (note, i)))
        {
            inspectors[turn].transform.SetSiblingIndex(i);
        }
    }
}
