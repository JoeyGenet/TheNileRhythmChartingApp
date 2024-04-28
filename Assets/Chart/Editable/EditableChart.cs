using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;

// TODO: instead of IEditableNoteContainer<Note>, use IEditableNoteContainer<IEditableNote>
public class EditableChart : Chart, IEditable, IEditableNoteContainer<Note>
{
	public delegate void EventAddedEvent(EditableEnvironmentChangeEvent note);
	public delegate void EventRemovedEvent(EditableEnvironmentChangeEvent note);

    public bool HasChanges { get; private set; }
    public void SetChanged() => HasChanges = true;

    public event NoteAddedEvent<Note> OnNoteAdded;
    public event NoteRemovedEvent<Note> OnNoteRemoved;
    public event NoteMovedEvent<Note> OnNoteMoved;

    public event EventAddedEvent OnEventAdded;
    public event EventRemovedEvent OnEventRemoved;

	public new EditableChartPackage Package => base.Package as EditableChartPackage;

    public new int DifficultyRating
    {
        get => base.DifficultyRating;
        set => this.RegisterChange(ref difficultyRating, Math.Clamp(value, 0, 16));
    }
	public new string DifficultyName
    {
        get => base.DifficultyName;
        set
		{
			// Add a number after the difficulty name so it doesn't conflict with other difficulties
            int id = 0;
            string name;
			var chartNames = Package.Charts.Select(c => c == this ? "" : c.DifficultyName).ToHashSet();
            while (chartNames.Contains(name = value.Trim() + (id == 0 ? "" : $" {id}"))) id++;

			this.RegisterChange(ref difficultyName, name);
		}
    }

	public new Environment Environment
    {
        get => base.Environment;
        set => this.RegisterChange(ref environment, value);
    }

    public EditableChart(EditableChartPackage package) : base(package) {}
    public EditableChart(EditableChartPackage package, Stream stream) : base(package, stream)
	{
		// Convert notes to editable
		var nonEditableNotes = new List<Note>(notes);
		notes.Clear();
		foreach (var note in nonEditableNotes)
		{
			Note editableNote = note switch
			{
				ButtonNote buttonNote => new EditableButtonNote(this, buttonNote),
				WheelFlick wheelFlick => new EditableWheelFlick(this, wheelFlick),
				WheelSequence wheelSequence => new EditableWheelSequence(this, wheelSequence),
				LeverSequence leverSequence => new EditableLeverSequence(this, leverSequence),
				_ => throw new Exception($"Cannot convert note {note} to editable"),
			};
			notes.Add(editableNote);
		}

		// Convert events to editable
		var nonEditableEvents = new List<EnvironmentChangeEvent>(events);
		events.Clear();
		foreach (var evt in nonEditableEvents)
		{
			events.Add(new EditableEnvironmentChangeEvent(this, evt));
		}
	}

    public bool AddNote(Note note, bool notify = true) => this.RegisterChange(
		() => notes.Add(note) && notify,
		() => OnNoteAdded?.Invoke(note)
	);
    public bool RemoveNote(Note note, bool notify = true) => this.RegisterChange(
		() => notes.Remove(note) && notify,
		() => OnNoteRemoved?.Invoke(note)
	);
	public bool ContainsNote(Note note) => notes.Contains(note);
	public void NotifyMoved(Note note) => OnNoteMoved?.Invoke(note);

	public bool AddEvent(EditableEnvironmentChangeEvent evt) => this.RegisterChange(
		() =>
		{
			if (events.Contains(evt)) return false;

			// Note will be inserted between these 2 notes:
            var prevEnvironmentChange = Events.Where(e => e.StartTime < evt.StartTime).OrderBy(e => e.StartTime).LastOrDefault();
            var nextEnvironmentChange = prevEnvironmentChange != null ? prevEnvironmentChange.NextEnvironmentChange
                                            : Events.OrderBy(e => e.StartTime).FirstOrDefault();

			// Update linked list
			if (prevEnvironmentChange != null) prevEnvironmentChange.NextEnvironmentChange = evt;
			if (nextEnvironmentChange != null) nextEnvironmentChange.PreviousEnvironmentChange = evt;
			evt.PreviousEnvironmentChange = prevEnvironmentChange;
			evt.NextEnvironmentChange = nextEnvironmentChange;

			Assert.IsTrue(events.Add(evt));
			return true;
		},
		() => OnEventAdded?.Invoke(evt)
	);
	public bool RemoveEvent(EditableEnvironmentChangeEvent evt) => this.RegisterChange(
		() =>
		{
			if (!events.Contains(evt)) return false;

			// Update linked list
            if (evt.PreviousEnvironmentChange != null) evt.PreviousEnvironmentChange.NextEnvironmentChange = evt.NextEnvironmentChange;
            if (evt.NextEnvironmentChange != null) evt.NextEnvironmentChange.PreviousEnvironmentChange = evt.PreviousEnvironmentChange;

			evt.PreviousEnvironmentChange = evt.NextEnvironmentChange = null;

			Assert.IsTrue(events.Remove(evt));
			return true;
		},
		() => OnEventRemoved?.Invoke(evt)
	);

	public void Save(Stream stream)
	{
        using var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true);

		writer.WriteLine("[Difficulty]");
		writer.WriteLine($"Rating: {DifficultyRating}");
		writer.WriteLine($"Name: {DifficultyName}");
		writer.WriteLine();

		writer.WriteLine("[Visuals]");
		if (environment != null) writer.WriteLine($"Environment: {Environment.Name}");
		writer.WriteLine();

		writer.WriteLine("[Notes]");
		static char lrToString(LeftRight lr) => lr switch
		{
			LeftRight.Left => 'L',
			LeftRight.Right => 'R',
			_ => throw new Exception($"Unable to serialize LeftRight {lr}"),
		};
		static char leverPosToString(LeverPos pos) => pos switch
		{
			LeverPos.Left => '<',
			LeverPos.Up => '^',
			LeverPos.Right => '>',
			LeverPos.Down => 'V',
			LeverPos.Middle => '.',
			_ => throw new Exception($"Unable to serialize LeverPos {pos}"),
		};
		foreach (Note note in notes)
		{
			string line = note switch
			{
				ButtonNote b => $"B {b.StartTime} {b.LeftPos}" + (b.RightPos == null ? "" : $" {b.RightPos}"),
				WheelFlick f => $"F {f.StartTime} {lrToString(f.Dir)}",
				WheelSequence w => $"W {w.StartTime} {w.Pos}" +
					w.Notes.Aggregate("", (s, t) => s + $" {t.DeltaAngle} {t.EndTime}"),
				LeverSequence l => $"L {lrToString(l.Dir)}" +
					l.Notes.Aggregate("", (s, n) => s + $" {n.StartTime} {leverPosToString(n.Pos)}"),
				_ => throw new Exception($"Unable to serialize note {note}"),
			};
			writer.WriteLine(line);
		}
		writer.WriteLine();


		writer.WriteLine("[Events]");
		foreach (var evt in events.OrderBy(evt => evt.StartTime))
		{
			writer.WriteLine($"E {evt.StartTime} {evt.EndTime} {evt.Environment}");
		}

		HasChanges = false;
	}

}
