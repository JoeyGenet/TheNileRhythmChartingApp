using UnityEditor;
using UnityEngine;
using System.IO;
using TMPro;
using System.Linq;

public class FontAssetReplacer : EditorWindow
{
    private string targetDirectory;
    private TMP_FontAsset oldFont;
    private TMP_FontAsset newFont;

    [MenuItem("Tools/Replace Font")]
    public static void ShowWindow()
    {
        EditorWindow window = GetWindow<FontAssetReplacer>();
        window.titleContent = new GUIContent("Replace Font");
    }

    public void OnGUI()
    {
        // set directory for search and replace

        GUILayout.BeginHorizontal();
        GUILayout.Label("Target Directory");
        targetDirectory = EditorGUILayout.TextField(targetDirectory);
        GUILayout.EndHorizontal();

        // select fonts

        GUILayout.BeginHorizontal();
        GUILayout.Label("Old Font");
        oldFont = (TMP_FontAsset)EditorGUILayout.ObjectField(oldFont, typeof(TMP_FontAsset), false);
        // string oldFontPath = AssetDatabase.GetAssetPath(oldFont);
        // string oldFontMetaPath = oldFontPath + ".meta";
        // string oldFontGuid = AssetDatabase.AssetPathToGUID(oldFontPath);
        string oldFontAssetGuid = "";
        long oldFontMaterialFileId = 0;
        long oldFontAtlasFileId = 0;
        if (oldFont != null)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(oldFont, out oldFontAssetGuid, out long _);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(oldFont.material, out string _, out oldFontMaterialFileId);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(oldFont.atlasTexture, out string _, out oldFontAtlasFileId);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("New Font");
        newFont = (TMP_FontAsset)EditorGUILayout.ObjectField(newFont, typeof(TMP_FontAsset), false);
        // string newFontPath = AssetDatabase.GetAssetPath(newFont);
        // string newFontMetaPath = newFontPath + ".meta";
        // string newFontGuid = AssetDatabase.AssetPathToGUID(newFontPath);
        string newFontAssetGuid = "";
        long newFontMaterialFileId = 0;
        long newFontAtlasFileId = 0;
        if (newFont != null)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(newFont, out newFontAssetGuid, out long _);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(newFont.material, out string _, out newFontMaterialFileId);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(newFont.atlasTexture, out string _, out newFontAtlasFileId);
        }
        GUILayout.EndHorizontal();

        // replace all font asset usages
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Replace Old Font with New Font"))
        {
            string[] targetAssetGuids = AssetDatabase.FindAssets("t:prefab t:scene t:material", new [] {targetDirectory});
            string[] targetAssetNames = targetAssetGuids.Select(guid => AssetDatabase.GUIDToAssetPath(guid)).ToArray();
            foreach (string targetAssetGuid in targetAssetGuids)
            {
                // AssetDatabase.FindAssets("t:material") will return the GUIDs of font files
                // because they contain submaterials. Do not replace text inside font files!
                string targetAssetPath = AssetDatabase.GUIDToAssetPath(targetAssetGuid);
                if (AssetDatabase.LoadAssetAtPath(targetAssetPath, typeof(Font)) != null)
                {
                    continue;
                }

                string newTargetAssetText = "";

                bool shouldRenameFile = false;
                bool didReplacementOccur = false;

                // replace GUID references to old asset with the new font GUID
                // also replace references to the font's atlas texture and material
                // font components will point to the new font and use the new font's base material if applicable, 
                // and font material presets for the old font will use the new font's atlas so they can apply to the new font
                using (StreamReader sr = new StreamReader(targetAssetPath))
                {
                    while (sr.Peek() > -1)
                    {
                        string line = sr.ReadLine();
                        if (line.Contains(oldFontAssetGuid))
                        {
                            // for material presets that now use the new font's atlas,
                            // we need to rename the font for it to appear as a selectable preset for the new font
                            if (line.Contains(oldFontAtlasFileId.ToString()) && 
                                AssetDatabase.LoadAssetAtPath(targetAssetPath, typeof(Material)) != null)
                            {
                                shouldRenameFile = true;
                            }
                            line = line.Replace(oldFontAssetGuid, newFontAssetGuid)
                                       .Replace(oldFontMaterialFileId.ToString(), newFontMaterialFileId.ToString())
                                       .Replace(oldFontAtlasFileId.ToString(), newFontAtlasFileId.ToString());
                            didReplacementOccur = true;
                        }
                        newTargetAssetText = newTargetAssetText + line + "\n";
                    }
                }

                if (didReplacementOccur)
                {
                    File.WriteAllText(targetAssetPath, newTargetAssetText);
                }

                // rename files after we write the altered text to the old path
                if (shouldRenameFile)
                {
                    // replace occurrences of the first contiguous string in the old font name with the new font name
                    string oldFontName = oldFont.name.Substring(0, oldFont.name.IndexOf(" "));
                    string newFontName = newFont.name.Substring(0, newFont.name.IndexOf(" "));
                    string assetName = AssetDatabase.LoadAssetAtPath(targetAssetPath, typeof(Material)).name;
                    AssetDatabase.RenameAsset(targetAssetPath, assetName.Replace(oldFontName, newFontName));
                }
            }
            AssetDatabase.Refresh();
        }
        GUILayout.EndHorizontal();
    }
}
