using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class WheelSequenceVertexRenderer : MonoBehaviour, IPointerClickHandler
{
    public EditableWheelSequence WheelSequence { get; private set; }

    RectTransform rect;

    public void Init(EditableWheelSequence sequence)
    {
        WheelSequence = sequence;
    }

    void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    void Update()
    {
        int index = transform.GetSiblingIndex();

        int pos;
        int time;
        if (index == 0)
        {
            pos = WheelTurn.AngleToPos(WheelSequence.Notes.Min.StartAngle);
            time = WheelSequence.StartTime;
        }
        else
        {
            var turn = WheelSequence.Notes.ElementAt(index-1);
            pos = WheelTurn.AngleToPos(WheelTurn.WrapAngle(turn.StartAngle + turn.DeltaAngle));
            time = turn.EndTime;
        }
        rect.anchorMin = rect.anchorMax = PlaybackManager.Instance.TrackToUIPosition(pos, time);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        switch (eventData.button)
        {
            case PointerEventData.InputButton.Left:
                UIManager.SelectedNote = WheelSequence;
                break;
            
            case PointerEventData.InputButton.Right:

                if (WheelSequence.Notes.Count <= 1) 
                {
                    WheelSequence.RemoveSelf();
                    break;
                }

                int index = transform.GetSiblingIndex();
                if (index == 0)
                {
                    var secondTurn = WheelSequence.Notes.ElementAt(1);
                    WheelSequence.Pos = WheelTurn.AngleToPos(WheelTurn.WrapAngle(secondTurn.StartAngle));
                    WheelSequence.StartTime = secondTurn.StartTime;
                    secondTurn.PreviousTurn.DeltaAngle = 0;
                    WheelSequence.RemoveNote(secondTurn.PreviousTurn);
                }
                else
                {
                    WheelSequence.Notes.ElementAt(index-1).RemoveSelf();
                }
                break;
        }
    }
}
