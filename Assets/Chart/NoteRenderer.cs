using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class NoteRenderer<NoteType> : MonoBehaviour where NoteType : Note
{
    public NoteType Note;

    protected virtual void Awake()
    {
        // GameManager.OnNoteHit += OnNoteHit;
        // GameManager.OnNoteMissed += OnNoteMissed;
        SaveManager.OnChartLoaded += DestroyOnChartLoaded;
        Chart.OnNoteRemoved += DestroyOnNoteRemoved;
    }

    protected virtual void OnDestroy()
    {
        // GameManager.OnNoteHit -= OnNoteHit;
        // GameManager.OnNoteMissed -= OnNoteMissed;
        SaveManager.OnChartLoaded -= DestroyOnChartLoaded;
        Chart.OnNoteRemoved -= DestroyOnNoteRemoved;
    }

    // public virtual void OnNoteHit(Note note, JudgementGrade grade, int points)
    // {

    // }

    public virtual void OnNoteMissed(Note note)
    {
        
    }

    void DestroyOnChartLoaded(string _) => Destroy(gameObject);

    void DestroyOnNoteRemoved(Note note)
    {
        if (note == Note) Destroy(gameObject);
    }
}
