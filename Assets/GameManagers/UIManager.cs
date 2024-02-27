using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public delegate void NoteSelectedEvent(Note note);
    
    public static event NoteSelectedEvent OnNoteSelected;

    public static UIManager Instance;

    [Header("Alert Dialog")]
    [SerializeField] AlertDialog alertDialogPrefab;

    void Awake()
    {
        Instance = this;
    }

    public AlertDialog ShowAlert(string title, string message)
    {
        var alert = Instantiate(alertDialogPrefab, transform);
        alert.Title = title;
        alert.Message = message;
        return alert;
    }

    public void SelectNote(Note note) => OnNoteSelected?.Invoke(note);
}
