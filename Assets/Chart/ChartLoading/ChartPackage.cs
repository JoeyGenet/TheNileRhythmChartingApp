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
public class MissingElementException : ChartPackageLoadException
{
    public MissingElementException(string message) : base(message) {}
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
    public string title = "No Title";
    public string artist = "Unknown";
    public string charter = "Unknown";
    public Texture2D coverImage = null;

    public string Title => title.Trim();
    public string Artist => artist.Trim();
    public string Charter => charter.Trim();
    public Texture2D CoverImage => coverImage;

    // Song
    public AudioClip songClip = null;
    public uint bpm = 120;
    public int bpmOffset = 0;
    public float gain = 0f;
    public int songStart = 0; // Start offset relative to song start
    public int songEnd = 0; // End offset relative to song end
    public uint previewStart = 0;
    public uint previewDuration = 10000;

    public AudioClip SongClip => songClip;
    public uint BPM => Math.Max(bpm, 1);
    public int BPMOffset => bpmOffset;
    public float Gain => gain;
    public int SongStart => songStart;
    public int SongEnd => songEnd;
    public uint PreviewStart => Math.Clamp(previewStart, 0, SongLength - 1000);
    public uint PreviewDuration => Math.Clamp(previewDuration, 1000, Math.Min(30000, SongLength - PreviewStart));
    
    protected readonly SortedSet<Chart> charts = new();
    public IReadOnlySortedSet<Chart> Charts => new ReadOnlySortedSet<Chart>(charts);

    public uint SongLength => SongClip == null ? 10000 : (uint) (SongClip.length * 1000);

	public int BeatToTime(float beat) => (int) (beat * 60000f / BPM + BPMOffset);
	public float TimeToBeat(int time) => (time - BPMOffset) * BPM / 60000f;
    public (int minutes, int seconds, int milliseconds) TimeToReadable(int time) 
        => (time / 60000, time / 1000 % 60, time % 1000);
    public int ReadableToTime(int minutes, int seconds, int milliseconds)
        => minutes * 60000 + seconds * 1000 + milliseconds;

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

        ZipArchiveEntry tryGetEntry(string path, string fileDescription = "entry") 
            => archive.Entries.FirstOrDefault(entry => entry.FullName == path)
            ?? throw new ChartPackageLoadException($"Could not find {fileDescription} {path} in archive.");

        Exception firstException = null;
        void wrapException(Action a)
        {
            try { a(); }
            catch (Exception e) { firstException = e; }
        }
        async Task wrapExceptionAsync(Func<Task> a)
        {
            try { await a(); }
            catch (Exception e) { firstException = e; }
        }


        // Parse config
        await wrapExceptionAsync(async () =>
        {
            var configEntry = archive.Entries
                .FirstOrDefault(entry => FileManager.GetExtension(entry.Name).ToLower() == "nr")
                ?? throw new MissingElementException("Missing config file ending in '.nr'");

            string coverImagePath = null;
            string songPath = null;
            
            var parser = new FileParser();

            parser["Metadata"] = (line, args) => parser.KeyValue(new()
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
                ["PreviewStart"] = val => previewStart = uint.Parse(val),
                ["PreviewDuration"] = val => previewDuration = uint.Parse(val),
            });

            wrapException(() =>
            {
                using var configStream = configEntry.Open();
                parser.Run(configStream);
            });

            string rootPath = configEntry.FullName[..^configEntry.Name.Length];

            await wrapExceptionAsync(() =>
            {
                if (coverImagePath == null) throw new MissingElementException("Missing cover image");
                return LoadCoverImage(tryGetEntry(rootPath + coverImagePath, "cover image"));
            });
            await wrapExceptionAsync(() =>
            {
                if (songPath == null) throw new MissingElementException("Missing song");
                return LoadSong(tryGetEntry(rootPath + songPath, "song file"));
            });
        });

        // Load charts
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (FileManager.GetExtension(entry.Name) == "nrchart")
            {
                wrapException(() => LoadChart(entry));
            }
        }
        if (charts.Count == 0)
        {
            firstException ??= new MissingElementException("Missing chart files ending in '.nrchart'");
        }

        if (firstException != null) throw firstException;
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
