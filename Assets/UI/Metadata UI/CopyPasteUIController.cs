using System.Collections.Generic;
using UnityEngine;

public class CopyPasteUIController : MonoBehaviour
{   
    [SerializeField] EventRelay copyEvent;
    [SerializeField] List<MultikeyShortcutEventRelay> copyShortcuts;
    [SerializeField] EventRelay pasteEvent;
    [SerializeField] List<MultikeyShortcutEventRelay> pasteShortcuts;

    void Awake()
    {
        // link copy button event to EditHistoryManager.Undo
        copyEvent.Event.AddListener(EditHistoryManager.Undo);
        // link shortcuts
        foreach (MultikeyShortcutEventRelay shortcut in copyShortcuts)
        {
            shortcut.Event.AddListener(EditHistoryManager.Undo);
        }

        // link redo button event to EditHistoryManager.Redo
        pasteEvent.Event.AddListener(EditHistoryManager.Redo);
        // link shortcuts
        foreach (MultikeyShortcutEventRelay shortcut in pasteShortcuts)
        {
            shortcut.Event.AddListener(EditHistoryManager.Redo);
        }
    }

}
