using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public Chart Chart { get; private set; } = new Chart();

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SaveChart()
    {
        // Set a variable to the Documents path.
        string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string path = Path.Combine(docPath, "Test.txt");

        // NativeFilePicker.ExportFile(path, success =>
        // {
        //     if (success) Debug.Log("File successfully exported");
        //     else Debug.Log("File export failed");
        // });

        // // Write the string array to a new file named "WriteLines.txt".
        // using (StreamWriter outputFile = new(path))
        // {
        //     outputFile.WriteLine("Hello world");
        // }
        // Debug.Log($"Saved to {path}");
    }
}
