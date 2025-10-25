using UnityEngine;

public class UndoRedoUIController : MonoBehaviour
{   
    [SerializeField] EventRelay undoEvent;
    [SerializeField] EventRelay redoEvent;

    void Awake()
    {
        undoEvent.Event.AddListener(IEditableExtensions.Undo);
        redoEvent.Event.AddListener(IEditableExtensions.Redo);
    }

}
