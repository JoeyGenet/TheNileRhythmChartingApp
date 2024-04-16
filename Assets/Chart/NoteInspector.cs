using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public abstract class NoteInspector<NoteType> : MonoBehaviour
where NoteType : Note, IEditableNote<EditableChart, Note>
{
    public virtual NoteType Note { get; protected set; }

    public virtual void Show(NoteType note)
    {
        Note = note;
        gameObject.SetActive(Note != null);
    }
}


public abstract class NoteContainerInspector<ContainerType, ChildType> : NoteInspector<ContainerType>
where ChildType : Note, IEditableNote<ContainerType, ChildType>
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
                    Note.RemoveSelf();

                    // If last note in container, remove the container
                    if (Container.Note.Notes.Count == 0)
                    {
                        Container.Note.RemoveSelf();
                    }
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

    protected virtual void Awake()
    {
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
        UpdateChildInspectorPosition(note);
    }

    void UpdateChildInspectorPosition(ChildType note)
    {
        var found = Note.Notes.Select((note, i) => (note, i)).FirstOrDefault(v => v.note == note);
        if (found.note != null)
        {
            childInspectors[note].transform.SetSiblingIndex(found.i);
        }
    }

    public override void Show(ContainerType note)
    {
        if (Note != null)
        {
            Note.OnNoteAdded -= AddChildInspector;
            Note.OnNoteRemoved -= RemoveChildInspector;
            Note.OnNoteMoved -= UpdateChildInspectorPosition;
        }

        if (note != null)
        {
            foreach (var (childNote, inspector) in childInspectors)
            {
                childInspectors.Remove(childNote);
                Destroy(inspector.gameObject);
            }
            for (int i = childInspectorsParent.childCount-1; i >= 0; i--)
            {
                Destroy(childInspectorsParent.GetChild(i).gameObject);
            }

            // foreach (ChildType childNote in note.Notes)
            // {
            //     AddChildInspector(childNote);
            // }
            note.OnNoteAdded += AddChildInspector;
            note.OnNoteRemoved += RemoveChildInspector;
            note.OnNoteMoved += UpdateChildInspectorPosition;
        }

        base.Show(note);
    }
}