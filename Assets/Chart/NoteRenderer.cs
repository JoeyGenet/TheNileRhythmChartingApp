using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class NoteRenderer<NoteType> : MonoBehaviour where NoteType : Note
{
    public NoteType Note;

    protected RectTransform rect;

    protected virtual void Awake()
    {
        rect = GetComponent<RectTransform>();

        // GameManager.OnNoteHit += OnNoteHit;
        // GameManager.OnNoteMissed += OnNoteMissed;
    }

    protected virtual void OnDestroy()
    {
        // GameManager.OnNoteHit -= OnNoteHit;
        // GameManager.OnNoteMissed -= OnNoteMissed;
        SaveManager.OnChartLoaded += DestroyOnChartLoaded;
        Chart.OnNoteRemoved += DestroyOnNoteRemoved;
        SaveManager.OnChartLoaded -= DestroyOnChartLoaded;
        Chart.OnNoteRemoved -= DestroyOnNoteRemoved;
    }

    // public virtual void OnNoteHit(Note note, JudgementGrade grade, int points)
    // {

    // }

    public virtual void OnNoteMissed(Note note)
    {
        
    }

    void DestroyOnChartLoaded(string _)
    {
        Destroy(gameObject);
    }

    void DestroyOnNoteRemoved(Note note)
    {
        if (note == Note)
        {
            Destroy(gameObject);
        }
    }
}
