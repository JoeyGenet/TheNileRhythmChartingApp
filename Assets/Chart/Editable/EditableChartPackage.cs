using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Handles reading from a collection of charts of different difficulties and the same song
/// </summary>
public class EditableChartPackage : ChartPackage, IEditable
{
    public bool ConfigHasChanges { get; private set; }
    public bool HasChanges => ConfigHasChanges; // TODO: also be true if any chart has changes
    public void SetChanged() => ConfigHasChanges = true;

    // Metadata
    public new string Title
    {
        get => title;
        set => this.RegisterChange(ref title, value.Trim());
    }
    public new string Artist
    {
        get => artist;
        set => this.RegisterChange(ref artist, value.Trim());
    }
    public new string Charter // TODO: move charter to Chart
    {
        get => charter;
        set => this.RegisterChange(ref charter, value.Trim());
    }

    // Song
    public new uint BPM
    {
        get => bpm;
        set => this.RegisterChange(ref bpm, value);
    }
    public new int BPMOffset
    {
        get => bpmOffset;
        set => this.RegisterChange(ref bpmOffset, value);
    }
    public new float Gain
    {
        get => gain;
        set => this.RegisterChange(ref gain, value);
    }
    public new int SongStart
    {
        get => songStart;
        set => this.RegisterChange(ref songStart, value);
    }
    public new int SongEnd
    {
        get => songEnd;
        set => this.RegisterChange(ref songEnd, value);
    }

    // readonly List<EditableChart> charts = new();
    public new IReadOnlyList<EditableChart> Charts => charts as IReadOnlyList<EditableChart>;

    string imageCache = null;
    string songCache = null;
    readonly Dictionary<EditableChart, string> chartNames = new();

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
        SetChanged();
    }

    protected override void LoadChart(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        var chart = new EditableChart(this, stream); // TODO: change to editable chart
        charts.Add(chart);
        chartNames[chart] = entry.Name;
    }

    public void Save(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);

        var imageEntry = archive.CreateEntryFromFile(imageCache, Path.GetFileName(imageCache));
        var songEntry = archive.CreateEntryFromFile(songCache, Path.GetFileName(songCache));

        foreach (EditableChart chart in Charts)
        {
            var chartEntry = archive.CreateEntry(chartNames[chart]);
            using var chartStream = chartEntry.Open();
            chart.Save(stream);
        }

        var configEntry = archive.CreateEntry($"{Title}.nr");
        using (var writer = new StreamWriter(configEntry.Open()))
        {
            writer.WriteLine("[Metadata]");
            writer.WriteLine($"Title: {Title}");
            writer.WriteLine($"Artist: {Artist}");
            writer.WriteLine($"Charter: {Charter}");
            writer.WriteLine($"CoverImage: {imageEntry.Name}");
            writer.WriteLine();

            writer.WriteLine("[Song]");
            writer.WriteLine($"SongFile: {songEntry.Name}");
            writer.WriteLine($"BPM: {BPM}");
            writer.WriteLine($"BPMOffset: {BPMOffset}");
            writer.WriteLine($"Gain: {Gain}");
            writer.WriteLine($"Start: {SongStart}");
            writer.WriteLine($"End: {SongEnd}");
        }
    }
}
