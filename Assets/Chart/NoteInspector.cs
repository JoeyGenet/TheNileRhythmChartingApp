using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public abstract class NoteInspector<NoteType> : MonoBehaviour
where NoteType : Note, IEditableNote<EditableChart, Note>
{
    public virtual NoteType Note { get; protected set; }

    protected virtual void Awake()
    {
        gameObject.SetActive(false);

        UIManager.OnNoteSelected += ShowHide;
        // Chart.OnNoteRemoved += 
    }

    protected virtual void OnDestroy()
    {
        UIManager.OnNoteSelected -= ShowHide;
    }

    // TODO: create UpdateOnNoteChanged that is called when the note changes or note's value changes
    protected virtual void Update()
    {
        if (Note != null && !UIManager.IsSelectingInputField && 
            (Input.GetKeyUp(KeyCode.Backspace) || Input.GetKeyUp(KeyCode.Delete)))
        {
            var note = Note;
            try
            {
                Note.Container.RemoveNote(Note);
                // SaveManager.Instance.ChartPackage.RemoveNote(Note);
                ShowHide(null);
            }
            catch (Exception e)
            {
                UIManager.Instance.ShowAlert("Exception While Deleting Note", $"Note {note} may not have been deleted.\n{e.Message}");
                Debug.LogException(e);
            }
        }
    }

    protected virtual void OnShow() {}

    protected virtual void ShowHide(Note note)
    {
        if (note is NoteType myNote)
        {
            if (Note == myNote) return; // Inspector is already showing this note. don't show again
            Note = myNote;
            try
            {
                OnShow();
                gameObject.SetActive(true);
            }
            catch (Exception e)
            {
                Note = null;
                UIManager.Instance.ShowAlert("Exception While Showing Inspector", $"Tried to show inspector for {myNote}\n{e.Message}");
                Debug.LogException(e);
            }
        }
        else
        {
            gameObject.SetActive(false);
            Note = null;
        }
    }
}


public abstract class NoteContainerInspector<ContainerType, ChildType> : NoteInspector<ContainerType>
where ChildType : Note
where ContainerType : Note, IEditableNote<EditableChart, Note>, IEditableNoteContainer<ChildType>
{
    public abstract class ChildInspector : MonoBehaviour
    {
        public ChildType Note { get; private set; }
        public NoteContainerInspector<ContainerType, ChildType> Container { get; private set; }

        [SerializeField] Button deleteButton;

        public virtual void Init(NoteContainerInspector<ContainerType, ChildType> container, ChildType note)
        {
            Container = container;
            Note = note;

        }

        protected virtual void Awake()
        {
            deleteButton.onClick.AddListener(() =>
            {
                var note = Note;
                try
                {
                    Container.Note.RemoveNote(Note);
                }
                catch (Exception e)
                {
                    UIManager.Instance.ShowAlert($"Exception While Deleting {typeof(ChildType)}", $"Note {note} may not have been deleted.\n{e.Message}");
                    Debug.LogException(e);
                }
            });
        }
    }

    [SerializeField] RectTransform childInspectorsParent;
    [SerializeField] ChildInspector childInspectorPrefab;
    [SerializeField] Button addNoteButton;

    readonly Dictionary<ChildType, ChildInspector> childInspectors = new();

    // protected virtual void OnNoteAdded(ChildType note) {}
    // protected virtual void OnNoteRemoved(ChildType note) {}

    protected override void Awake()
    {
        base.Awake();

        addNoteButton.onClick.AddListener(() =>
        {
            try
            {
                Note.AddNote(NewChildNote());
            }
            catch (Exception e)
            {
                UIManager.Instance.ShowAlert($"Exception While Adding {typeof(ChildType)}", $"Note may not have been added.\n{e.Message}");
                Debug.LogException(e);
            }
        });
    }

    protected override void Update()
    {
        base.Update();

        if (Note == null) return;

        // TODO: optimize somehow?
        foreach (var (leverNote, i) in Note.Notes.Select((note, i) => (note, i)))
        {
            childInspectors[leverNote].transform.SetSiblingIndex(i);
        }
    }

    protected abstract ChildType NewChildNote();

    void RemoveChildInspector(ChildType note)
    {
        var childInspector = childInspectors[note];
        if (childInspectors.Remove(note))
        {
            Destroy(childInspector.gameObject);
        }
    }

    void AddChildInspector(ChildType note)
    {
        var childInspector = Instantiate(childInspectorPrefab, childInspectorsParent);
        childInspector.Init(this, note);
        childInspectors[note] = childInspector;
    }

    protected override void ShowHide(Note note)
    {
        if (note is ContainerType myNote)
        {
            foreach (var (childNote, inspector) in childInspectors)
            {
                childInspectors.Remove(childNote);
                Destroy(inspector.gameObject);
            }
            foreach (ChildType childNote in Note.Notes)
            {
                AddChildInspector(childNote);
            }
            
            base.OnShow();

            if (Note != null)
            {
                myNote.OnNoteAdded += AddChildInspector;
                myNote.OnNoteRemoved += RemoveChildInspector;
            }
        }
        else
        {
            Note.OnNoteAdded -= AddChildInspector;
            Note.OnNoteRemoved -= RemoveChildInspector;

            base.OnShow();
        }
    }
}