using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Handles reading from a collection of charts of different difficulties and the same song
/// </summary>
public class EditableChartPackage : ChartPackage, IEditable
{
    public delegate void ChartAddedEvent(EditableChart chart);
    public delegate void ChartRemovedEvent(EditableChart chart);

    public event ChartAddedEvent OnChartAdded;
    public event ChartRemovedEvent OnChartRemoved;

    public bool ConfigHasChanges { get; private set; }
    public bool HasChanges => ConfigHasChanges || Charts.Any(c => c.HasChanges); // TODO: also be true if any chart has changes
    public void SetChanged() => ConfigHasChanges = true;

    // Metadata
    public new string Title
    {
        get => base.Title;
        set => this.RegisterChange(ref title, value);
    }
    public new string Artist
    {
        get => base.Artist;
        set => this.RegisterChange(ref artist, value);
    }
    public new string Charter // TODO: move charter to Chart
    {
        get => base.Charter;
        set => this.RegisterChange(ref charter, value);
    }
    public new string Description
    {
        get => base.Description;
        set => this.RegisterChange(ref description, value);
    }
    public new string CoverImagePath
    {
        get => base.CoverImagePath;
        set => this.RegisterChange(ref coverImagePath, value);
    }

    // Song
    public new string SongClipPath
    {
        get => base.SongClipPath;
        set => this.RegisterChange(ref songClipPath, value);
    }
    public new uint BPM
    {
        get => base.BPM;
        set => this.RegisterChange(ref bpm, value);
    }
    public new int BPMOffset
    {
        get => base.BPMOffset;
        set => this.RegisterChange(ref bpmOffset, value);
    }
    public new float Gain
    {
        get => base.Gain;
        set => this.RegisterChange(ref gain, value);
    }
    public new int SongStart
    {
        get => base.SongStart;
        set => this.RegisterChange(ref songStart, value);
    }
    public new int SongEnd
    {
        get => base.SongEnd;
        set => this.RegisterChange(ref songEnd, value);
    }
    public new uint PreviewStart
    {
        get => base.PreviewStart;
        set => this.RegisterChange(ref previewStart, value);
    }
    public new uint PreviewDuration
    {
        get => base.PreviewDuration;
        set => this.RegisterChange(ref previewDuration, value);
    }
    public uint UnclampedPreviewDuration
    {
        get => previewDuration;
        set => this.RegisterChange(ref previewDuration, value);
    }

    // readonly List<EditableChart> charts = new();
    public new IReadOnlySortedSet<EditableChart> Charts => (ReadOnlySortedSet<EditableChart, Chart>) charts;

    string imageCache = null;
    string songCache = null;

    public string ImageName => imageCache == null ? null : Path.GetFileName(imageCache);
    public string SongName => songCache == null ? null : Path.GetFileName(songCache);

    protected override async Task LoadCoverImage(ZipArchiveEntry entry)
    {
        imageCache = await entry.ExtractToTempPath("ChartPackageCache");
        using Stream stream = entry.Open();
        coverImage = await FileManager.OpenTexture(stream);
    }
    public async Task SetCoverImage(string path)
    {
        imageCache = FileManager.CopyToTempPath(path, "ChartPackageCache");
        using Stream stream = File.OpenRead(path);
        coverImage = await FileManager.OpenTexture(stream);
        ChartManager.ChartPackage.CoverImagePath = path;
        SetChanged();
    }
    public void ResetCoverImage()
    {
        coverImage = null;
        ChartManager.ChartPackage.CoverImagePath = "";
        SetChanged();
    }

    protected override async Task LoadSong(ZipArchiveEntry entry)
    {
        songCache = await entry.ExtractToTempPath("ChartPackageCache");
        songClip = await FileManager.OpenAudioClip(songCache);
    }
    public async Task SetSong(string path)
    {
        songCache = FileManager.CopyToTempPath(path, "ChartPackageCache");
        songClip = await FileManager.OpenAudioClip(path);
        ChartManager.ChartPackage.SongClipPath = path;
        SetChanged();
    }
    public void ResetSong()
    {
        songCache = null;
        songClip = null;
        ChartManager.ChartPackage.SongClipPath = "";
        SetChanged();
    }

    protected override void LoadChart(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        var chart = new EditableChart(this, stream); // TODO: change to editable chart
        charts.Add(chart);
    }

    public EditableChart AddChart(EditableChart chart = null, bool updateEditHistory = true)
    {
        // if adding a new chart, give it a new difficulty rating
        if (chart == null)
        {
            chart = new EditableChart(this);

            bool hasDifficulty(string name) => charts.Any(c => c.DifficultyName == name);

            if (!hasDifficulty("Master"))
            {
                if (hasDifficulty("Expert")) chart.DifficultyName = "Master";
                else if (hasDifficulty("Hard")) chart.DifficultyName = "Expert";
                else if (hasDifficulty("Medium")) chart.DifficultyName = "Hard";
                else if (hasDifficulty("Easy")) chart.DifficultyName = "Medium";
                else chart.DifficultyName = "Easy";
            }
            else
            {
                if (!hasDifficulty("Expert")) chart.DifficultyName = "Expert";
                else if (!hasDifficulty("Hard")) chart.DifficultyName = "Hard";
                else if (!hasDifficulty("Medium")) chart.DifficultyName = "Medium";
                else if (!hasDifficulty("Easy")) chart.DifficultyName = "Easy";
                else chart.DifficultyName = $"Unnamed {charts.Count-4}";
            }

            chart.DifficultyRating = chart.DifficultyName switch
            {
                "Easy" => 1,
                "Medium" => 5,
                "Hard" => 9,
                "Expert" => 12,
                "Master" => 14,
                _ => 1
            };

        }
        charts.Add(chart);
        SetChanged();
        OnChartAdded?.Invoke(chart);
        if (updateEditHistory)
        {
            AddChartAction addChartAction = new AddChartAction(chart);
            addChartAction.CommitAction();
        }
        return chart;
    }

    public bool RemoveChart(EditableChart chart, bool updateEditHistory = true)
    {
        if (updateEditHistory)
        {
            RemoveChartAction removeChartAction = new RemoveChartAction(chart);
            removeChartAction.CommitAction();
        }
        return this.RegisterChange(
            () => charts.Count > 1 && charts.Remove(chart),
            () => OnChartRemoved?.Invoke(chart)
        );
    }

    public override async Task Load(Stream stream)
    {
        try
        {
            await base.Load(stream);
        }
        catch (MissingElementException) {} // It's fine for chart editor to have a pkg without a chart
    }

    public void Save(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);

        var imageEntry = imageCache == null ? null : archive.CreateEntryFromFile(imageCache, ImageName);
        var songEntry = songCache == null ? null : archive.CreateEntryFromFile(songCache, SongName);

        var chartNames = new HashSet<string>();
        foreach (EditableChart chart in Charts)
        {
            int id = 0;
            string name;
            while (chartNames.Contains(name = chart.DifficultyName + (id == 0 ? "" : id) + ".nrchart")) id++;

            var chartEntry = archive.CreateEntry(name);
            using var chartStream = chartEntry.Open();
            chart.Save(chartStream);
        }

        var configEntry = archive.CreateEntry($"{Title}.nr");
        using (var writer = new StreamWriter(configEntry.Open()))
        {
            writer.WriteLine("[Metadata]");
            writer.WriteLine($"Title: {Title}");
            writer.WriteLine($"Artist: {Artist}");
            writer.WriteLine($"Charter: {Charter}");
            writer.WriteLine($"Description: {Description.Replace("\n", "\\n")}");
            if (imageEntry != null) writer.WriteLine($"CoverImage: {imageEntry.Name}");
            writer.WriteLine();

            writer.WriteLine("[Song]");
            if (songEntry != null) writer.WriteLine($"SongFile: {songEntry.Name}");
            writer.WriteLine($"BPM: {BPM}");
            writer.WriteLine($"BPMOffset: {BPMOffset}");
            writer.WriteLine($"Gain: {Gain}");
            writer.WriteLine($"PreviewStart: {PreviewStart}");
            writer.WriteLine($"PreviewDuration: {PreviewDuration}");
            writer.WriteLine($"Start: {SongStart}");
            writer.WriteLine($"End: {SongEnd}");
        }

        ConfigHasChanges = false;
    }
}
