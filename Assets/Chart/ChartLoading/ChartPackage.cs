using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class ChartPackageLoadException : Exception
{
    public ChartPackageLoadException(string message) : base(message) {}
}

/// <summary>
/// Handles reading from a collection of charts of different difficulties and the same song
/// </summary>
public class ChartPackage
{
    enum FileType
    {
        Config,
        Chart,
        Image,
        Audio,
    }

    // Metadata
    protected string title = "No Title";
    protected string artist = "Unknown";
    protected string charter = "Unknown";
    protected Texture2D coverImage = null;

    public string Title => title;
    public string Artist => artist;
    public string Charter => charter;
    public Texture2D CoverImage => coverImage;

    // Song
    protected AudioClip songClip = null;
    protected uint bpm = 120;
    protected int bpmOffset = 0;
    protected float gain = 0f;
    protected int songStart = 0; // Start offset relative to song start
    protected int songEnd = 0; // End offset relative to song end

    public AudioClip SongClip => songClip;
    public uint BPM => bpm;
    public int BPMOffset => bpmOffset;
    public float Gain => gain;
    public int SongStart => songStart;
    public int SongEnd => songEnd;
    
    protected readonly List<Chart> charts = new();
    public IReadOnlyList<Chart> Charts => charts;

	public int BeatToTime(float beat) => (int) (beat * 60000f / BPM + BPMOffset);
	public float TimeToBeat(int time) => (time - BPMOffset) * BPM / 60000f;

    protected virtual async Task LoadCoverImage(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        coverImage = await FileManager.OpenTexture(stream);
    }

    protected virtual async Task LoadSong(ZipArchiveEntry entry)
    {
        string tempFilePath = await entry.ExtractToTempPath("AudioClips");
        songClip = await FileManager.OpenAudioClip(tempFilePath);
        File.Delete(tempFilePath);
    }

    protected virtual void LoadChart(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        charts.Add(new Chart(this, stream));
    }

    public virtual async Task Load(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);

        // Parse config
        ZipArchiveEntry configEntry = archive.Entries
            .FirstOrDefault(entry => FileManager.GetExtension(entry.Name).ToLower() == "nr")
            ?? throw new ChartPackageLoadException("Could not find config file that ends in '.nr'");

        var parser = new FileParser();

        string coverImagePath = null;
        string songPath = null;

        parser["Metadata"] += (line, args) => parser.KeyValue(new()
        {
            ["Title"] = val => title = val,
            ["Artist"] = val => artist = val,
            ["Charter"] = val => charter = val,
            ["CoverImage"] = filepath => coverImagePath = filepath,
        });

        parser["Song"] = (line, args) => parser.KeyValue(new()
        {
            ["SongFile"] = filepath => songPath = filepath,
            ["BPM"] = val => bpm = uint.Parse(val),
            ["BPMOffset"] = val => bpmOffset = int.Parse(val),
            ["Gain"] = val => gain = float.Parse(val),
            ["Start"] = val => songStart = int.Parse(val),
            ["End"] = val => songEnd = int.Parse(val),
        });

        using (var configStream = configEntry.Open())
        {
            await Task.Run(() => parser.Run(configStream));
        }

        string rootPath = configEntry.FullName[..^configEntry.Name.Length];
        await LoadCoverImage(archive.TryGetEntry(rootPath + coverImagePath, "cover image"));
        await LoadSong(archive.TryGetEntry(rootPath + songPath, "song file"));


        // Load charts
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (FileManager.GetExtension(entry.Name) == "nrchart")
            {
                await Task.Run(() => LoadChart(entry));
            }
        }
        if (charts.Count == 0)
        {
            throw new ChartPackageLoadException("Chart package has no charts/difficulties");
        }
        else
        {
            charts.Sort((a, b) => a.DifficultyRating.CompareTo(b.DifficultyRating));
        }
    }

    // public static async Task<ChartPackage> Load(string path)
    // {
    //     using var stream = await FileManager.OpenRead(path);
    //     return new ChartPackage(stream);
    // }

    public override string ToString() => $@"ChartPackage
Title: {Title}
Artist: {Artist}
Charter: {Charter}
CoverImage: {(CoverImage != null ? CoverImage.width : null)}
SongClip: {(SongClip != null ? SongClip.length : null)}
BPM: {BPM}
BPMOffset: {BPMOffset}
Gain: {Gain}
SongStart: {SongStart}
SongEnd: {SongEnd}
Charts: {string.Join(", ", charts.Select(c => c.Notes.Count))}";
}
