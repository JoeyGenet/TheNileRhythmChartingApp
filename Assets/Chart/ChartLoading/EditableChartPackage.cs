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
        set => this.RegisterChange(ref title, value);
    }
    public new string Artist
    {
        get => artist;
        set => this.RegisterChange(ref artist, value);
    }
    public new string Charter
    {
        get => charter;
        set => this.RegisterChange(ref charter, value);
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

    string imageCache = null;
    string songCache = null;
    readonly Dictionary<Chart, string> chartNames = new();

    protected override async Task LoadSong(ZipArchiveEntry entry)
    {
        songCache = entry.ExtractToTempPath("ChartPackageCache");
        songClip = await FileManager.OpenAudioClip(songCache);
    }
    public async Task SetSong(string path)
    {
        songCache = FileManager.CopyToTempPath(path, "ChartPackageCache");
        songClip = await FileManager.OpenAudioClip(path);
        SetChanged();
    }

    protected override void LoadCoverImage(ZipArchiveEntry entry)
    {
        imageCache = entry.ExtractToTempPath("ChartPackageCache");
        using Stream stream = entry.Open();
        coverImage = FileManager.OpenTexture(stream);
    }
    public void SetCoverImage(string path)
    {
        imageCache = FileManager.CopyToTempPath(path, "ChartPackageCache");
        using Stream stream = File.OpenRead(path);
        coverImage = FileManager.OpenTexture(stream);
        SetChanged();
    }

    protected override void LoadChart(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        var chart = Chart.Load(this, stream); // TODO: change to editable chart
        charts.Add(chart);
        chartNames[chart] = entry.Name;
    }

    public EditableChartPackage(Stream stream) : base(stream) {}

    public void Save(string path)
    {
        using var archive = new ZipArchive(File.OpenWrite(path));

        var imageEntry = archive.CreateEntryFromFile(imageCache, Path.GetFileName(imageCache));
        var songEntry = archive.CreateEntryFromFile(songCache, Path.GetFileName(songCache));


    }
}
