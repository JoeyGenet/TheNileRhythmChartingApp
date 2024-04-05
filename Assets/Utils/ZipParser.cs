using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZipParser : FileParser
{
    ZipArchiveEntry TryGetEntry(string path, string fileDescription) 
        => archive.Entries.FirstOrDefault(entry => entry.FullName == rootPath + path)
        ?? throw new ChartPackageLoadException($"Could not find {fileDescription} {rootPath + path} in archive.");
}
