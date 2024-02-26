using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class NoteRenderer : MonoBehaviour
{
    // public Note Note;

    void Awake()
    {
        // GameManager.OnNoteHit += OnNoteHit;
        // GameManager.OnNoteMissed += OnNoteMissed;
        SaveManager.OnChartLoaded += DestroyOnChartLoaded;
    }

    void OnDestroy()
    {
        // GameManager.OnNoteHit -= OnNoteHit;
        // GameManager.OnNoteMissed -= OnNoteMissed;
        SaveManager.OnChartLoaded -= DestroyOnChartLoaded;
    }

    // public virtual void OnNoteHit(Note note, JudgementGrade grade, int points)
    // {

    // }

    public virtual void OnNoteMissed(Note note)
    {
        
    }

    void DestroyOnChartLoaded(string _) => Destroy(gameObject);
}
