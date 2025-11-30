using System.Collections.Generic;
using UnityEngine;

public class UndoRedoUIController : MonoBehaviour
{   
    [SerializeField] EventRelay undoEvent;
    [SerializeField] List<MultikeyShortcutEventRelay> undoShortcuts;
    [SerializeField] EventRelay redoEvent;
    [SerializeField] List<MultikeyShortcutEventRelay> redoShortcuts;

    void Awake()
    {
        // link undo button event to EditHistoryManager.Undo
        undoEvent.Event.AddListener(EditHistoryManager.Undo);
        // link shortcuts
        foreach (MultikeyShortcutEventRelay shortcut in undoShortcuts)
        {
            shortcut.Event.AddListener(EditHistoryManager.Undo);
        }

        // link redo button event to EditHistoryManager.Redo
        redoEvent.Event.AddListener(EditHistoryManager.Redo);
        // link shortcuts
        foreach (MultikeyShortcutEventRelay shortcut in redoShortcuts)
        {
            shortcut.Event.AddListener(EditHistoryManager.Redo);
        }
    }

}
