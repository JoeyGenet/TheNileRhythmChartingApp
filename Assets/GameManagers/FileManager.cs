using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Networking;

public static class FileManager
{
    public static string BuiltinDir => "StreamingAssets";
    public static string UserDir
        => Path.Join(Regex.Replace(Application.persistentDataPath, "Android.*", ""), "TheNileRhythm");

    public static string JoinPath(string root, params string[] path) 
        => path.Aggregate(root, (acc, p) => p == null ? acc : Path.Join(acc, p));

    public static string GetExtension(string path) => path[(path.LastIndexOf('.') + 1)..];

    public static string[] GetFiles(string path, string searchPattern = null, SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        return Directory.GetFiles(path, searchPattern, searchOption);
    }

    static async Task SendUnityWebRequest(UnityWebRequest uwr)
    {
        uwr.SendWebRequest();

        while (!uwr.isDone) await Task.Delay(5);

        if (uwr.result != UnityWebRequest.Result.Success)
        {
            throw new Exception($"Exception while requesting file {uwr.uri}: " + uwr.error);
        }
    }


    public static Task<Stream> OpenRead(string path)
    {
        return Task.FromResult<Stream>(File.OpenRead(path));
    }


    public static bool IsTexture(string extension) => extension.ToLower() switch
    {
        "png" or "jpg" or "jpeg" or "bmp" or "tif" or "tiff" => true,
        _ => false,
    };

    // public static async Task<Texture2D> OpenTexture(string path)
    // {
    //     using var uwr = UnityWebRequestTexture.GetTexture(new Uri(path), nonReadable: true); // Set non-readable to conserve memory
    //     await SendUnityWebRequest(uwr);
    //     return DownloadHandlerTexture.GetContent(uwr);
    // }

    public static Texture2D OpenTexture(Stream stream)
    {
        var texture = new Texture2D(1, 1);
        using var memStream = new MemoryStream();
        stream.CopyTo(memStream);
        texture.LoadImage(memStream.ToArray());
        return texture;
    }

    public static AudioType? GetAudioTypeFromExtension(string extension) => extension.ToLower() switch
    {
        "wav" => AudioType.WAV,
        "mp3" or "mp2" => AudioType.MPEG,
        "ogg" => AudioType.OGGVORBIS,
        "aiff" => AudioType.AIFF,
        _ => null,
    };

    public static async Task<AudioClip> OpenAudioClip(string path)
    {
        var audioType = GetAudioTypeFromExtension(GetExtension(path))
            ?? throw new Exception($"The audio file format of {path} is not supported");

        using var uwr = UnityWebRequestMultimedia.GetAudioClip(new Uri(path), audioType);
        await SendUnityWebRequest(uwr);
        return DownloadHandlerAudioClip.GetContent(uwr);
    }

    static string GetTempPath(string path, string filename)
    {
        Directory.CreateDirectory(JoinPath(Application.temporaryCachePath, path));
        return JoinPath(Application.temporaryCachePath, path, filename);
    }

    public static string CopyToTempPath(string source, string path = null)
    {
        string tempFilePath = GetTempPath(path, Path.GetFileName(path));
        File.Copy(source, tempFilePath, overwrite: true);
        return tempFilePath;
    }

    public static string SaveToTempPath(this Stream stream, string path = null)
    {
        string tempFilePath = GetTempPath(path, null);
        using var fileStream = File.Open(tempFilePath, FileMode.Create);
        stream.CopyTo(fileStream);
        return tempFilePath;
    }

    public static string ExtractToTempPath(this ZipArchiveEntry entry, string path = null)
    {
        string tempFilePath = GetTempPath(path, entry.Name);
        entry.ExtractToFile(tempFilePath, overwrite: true);
        return tempFilePath;
    }
    

    // public static async Task<AudioClip> OpenAudioClip(this ZipArchiveEntry entry)
    // {
    //     string tempFilePath = entry.ExtractToTempPath("AudioClips");
    //     AudioClip clip = await OpenAudioClip(tempFilePath);
    //     File.Delete(tempFilePath);
    //     return clip;
    // }


    public static ZipArchiveEntry TryGetEntry(this ZipArchive archive, string path, string fileDescription = "entry") 
        => archive.Entries.FirstOrDefault(entry => entry.FullName == path)
        ?? throw new ChartPackageLoadException($"Could not find {fileDescription} {path} in archive.");
}
