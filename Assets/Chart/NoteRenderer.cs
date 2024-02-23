using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteRenderer : MonoBehaviour
{
    // public Note Note;

    public virtual void OnNoteHit(Note note, JudgementGrade grade, int points)
    {

    }

    public virtual void OnNoteMissed(Note note)
    {

    }

    void DestroyOnChartEnd() => Destroy(gameObject);

    void Awake()
    {
        GameManager.OnNoteHit += OnNoteHit;
        GameManager.OnNoteMissed += OnNoteMissed;
        GameManager.OnChartEnded += DestroyOnChartEnd;
    }

    void OnDestroy()
    {
        GameManager.OnNoteHit -= OnNoteHit;
        GameManager.OnNoteMissed -= OnNoteMissed;
        GameManager.OnChartEnded -= DestroyOnChartEnd;
    }
    

}
