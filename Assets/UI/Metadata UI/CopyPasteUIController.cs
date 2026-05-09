using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class CopyPasteUIController : MonoBehaviour
{   
    [SerializeField] EventRelay copyEvent;
    [SerializeField] List<MultikeyShortcutEventRelay> copyShortcuts;
    [SerializeField] EventRelay pasteEvent;
    [SerializeField] List<MultikeyShortcutEventRelay> pasteShortcuts;

    void Awake()
    {
        // link copy button event to Copy, which calls CopyPasteManager.CopySelectedNotesToClipboard
        copyEvent.Event.AddListener(Copy);
        // link shortcuts
        foreach (MultikeyShortcutEventRelay shortcut in copyShortcuts)
        {
            shortcut.Event.AddListener(Copy);
        }

        // link redo button event to Paste, which calls CopyPasteManager.PasteNotesFromClipboard
        pasteEvent.Event.AddListener(Paste);
        // link shortcuts
        foreach (MultikeyShortcutEventRelay shortcut in pasteShortcuts)
        {
            shortcut.Event.AddListener(Paste);
        }
    }

    public void Copy()
    {
        // only copy notes from the last selection group if it is currently selected
        if (UIManager.SelectedElement == UIManager.SelectionGroup && UIManager.SelectionGroup.notes.Count > 0)
        {
            CopyPasteManager.CopySelectedNotesToClipboard(UIManager.SelectionGroup.notes);
        }
        // otherwise, copy selected note, if anything is selected at all
        else if (UIManager.SelectedElement != null && UIManager.SelectedElement is Note note)
        {
            SortedSet<Note> copiedNote = new SortedSet<Note>{note};
            if (note is WheelSequence wheelSequence)
            {
                copiedNote.AddRange(wheelSequence.Notes);
            }
            if (note is LeverSequence leverSequence)
            {
                copiedNote.AddRange(leverSequence.Notes);
            }
            CopyPasteManager.CopySelectedNotesToClipboard(copiedNote);
        }
    }

    public void Paste()
    {
        int pasteTime = PlaybackManager.Instance.MillisecondsInChart;
        // use the timestamp at mouse position if it is within the track's boundaries
        if (UIManager.Track.PointerToTrackPosition(Input.mousePosition, allowOutOfBounds: false) is var (pos, time))
        {
            pasteTime = time;
        }
        // snap time to user's settings unless they are holding alt
        if (!Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt))
        {
            pasteTime = UIManager.SnapTime(pasteTime);
        }
        CopyPasteManager.PasteNotesFromClipboard(pasteTime);
    }

}
