using UnityEngine;

public class UndoRedoUIController : MonoBehaviour
{   
    [SerializeField] EventRelay undoEvent;
    [SerializeField] EventRelay redoEvent;

    void Awake()
    {
        // undoEvent.Event.AddListener(EditHistoryManager.Undo);
        // redoEvent.Event.AddListener(EditHistoryManager.Redo);
    }

}
