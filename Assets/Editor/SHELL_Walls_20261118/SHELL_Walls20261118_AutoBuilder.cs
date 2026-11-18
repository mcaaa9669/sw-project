#if UNITY_EDITOR
// SHELL: 3 safe, self-contained wall prefab styles.
// On installation, creates NEW prefab copies only. Never edits shared source prefabs or scenes.
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class SHELL_Walls20261118_AutoBuilder
{
    private const string ArtDir = "Assets/Art/Gimmicks/SHELL_Walls_20261118";
    private const string PrefabDir = "Assets/Prefabs/Gimmicks/SHELL_Walls_20261118";
    private const string BasePrefab = "Assets/Prefabs/Gimmicks/BushWall.prefab";
    private static readonly string[] Filenames = { "01_MossRock.png", "02_VineRock.png", "03_LeafyForest.png" };
    private static readonly string[] Names = { "SHELL_Wall_01_MossRock", "SHELL_Wall_02_VineRock", "SHELL_Wall_03_LeafyForest" };
    private static bool busy;

    static SHELL_Walls20261118_AutoBuilder()
    {
        EditorApplication.delayCall += TryAutoBuild;
    }

    private static void TryAutoBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            return;
        }
        if (!AssetDatabase.IsValidFolder(ArtDir)) return;
        bool missing = false;
        for (int i = 0; i < Names.Length; i++)
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/" + Names[i] + ".prefab") == null)
                missing = true;
        if (missing) BuildAll(false);
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.delayCall += TryAutoBuild;
    }

    [MenuItem("Tools/SHELL/Build 3 Ready Wall Prefabs (20261118)")]
    public static void BuildFromMenu() { BuildAll(true); }

    private static void BuildAll(bool userInvoked)
    {
        if (busy || EditorApplication.isPlayingOrWillChangePlaymode) return;
        busy = true;
        try
        {
            if (!AssetDatabase.IsValidFolder(ArtDir))
                throw new Exception("PNG folder not found: " + ArtDir);
            EnsureFolder("Assets/Prefabs/Gimmicks", "SHELL_Walls_20261118");
            int created = 0;
            for (int i = 0; i < Names.Length; i++)
            {
                string pngPath = ArtDir + "/" + Filenames[i];
                var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
                if (importer == null) throw new Exception("Missing PNG: " + pngPath);
                bool importChanged = importer.textureType != TextureImporterType.Sprite
                    || importer.spriteImportMode != SpriteImportMode.Single
                    || Math.Abs(importer.spritePixelsPerUnit - 64f) > 0.001f
                    || importer.filterMode != FilterMode.Point || importer.mipmapEnabled
                    || importer.textureCompression != TextureImporterCompression.Uncompressed
                    || !importer.alphaIsTransparency;
                if (importChanged)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 64f;
                    importer.filterMode = FilterMode.Point;
                    importer.mipmapEnabled = false;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
                if (sprite == null) throw new Exception("Could not import sprite: " + pngPath);
                string outPath = PrefabDir + "/" + Names[i] + ".prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(outPath) != null) continue;

                // Reuse the team's existing collider dimensions and child layout when available.
                // The original BushWall source itself is never changed.
                GameObject root = null;
                bool loadedPrefab = false;
                try
                {
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefab) != null)
                    {
                        root = PrefabUtility.LoadPrefabContents(BasePrefab);
                        loadedPrefab = true;
                    }
                    else
                        root = new GameObject(Names[i]);

                    root.name = Names[i];
                    var hitbox = root.GetComponent<BoxCollider2D>();
                    if (hitbox == null) hitbox = root.AddComponent<BoxCollider2D>();
                    hitbox.isTrigger = false;
                    hitbox.size = new Vector2(2.8f, 4f);
                    hitbox.offset = Vector2.zero;

                    // Disable old base and leaves sprites; apply the new sprite to Bush_Base.
                    SpriteRenderer chosen = null;
                    var renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
                    foreach (var r in renderers)
                    {
                        r.enabled = false;
                        if (r.gameObject.name == "Bush_Base") chosen = r;
                    }
                    if (chosen == null)
                    {
                        var visual = new GameObject("SHELL_NewWallVisual");
                        visual.transform.SetParent(root.transform, false);
                        chosen = visual.AddComponent<SpriteRenderer>();
                    }
                    chosen.enabled = true;
                    chosen.sprite = sprite;
                    chosen.sortingLayerName = "Default";
                    chosen.sortingOrder = 10;
                    chosen.color = Color.white;
                    chosen.transform.localPosition = Vector3.zero;
                    chosen.transform.localRotation = Quaternion.identity;
                    chosen.transform.localScale = new Vector3(
                        hitbox.size.x / Mathf.Max(0.001f, sprite.bounds.size.x),
                        hitbox.size.y / Mathf.Max(0.001f, sprite.bounds.size.y), 1f);
                    if (PrefabUtility.SaveAsPrefabAsset(root, outPath) == null)
                        throw new Exception("Could not write: " + outPath);
                    created++;
                }
                finally
                {
                    if (root != null)
                    {
                        if (loadedPrefab) PrefabUtility.UnloadPrefabContents(root);
                        else UnityEngine.Object.DestroyImmediate(root);
                    }
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[SHELL Walls] Three prefab styles ready in " + PrefabDir + " (new: " + created + "). Shared scenes unchanged.");
            if (userInvoked)
            {
                var folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(PrefabDir);
                Selection.activeObject = folder;
                EditorGUIUtility.PingObject(folder);
                EditorUtility.DisplayDialog("SHELL Wall Pack", "가벽 3종 프리팹 준비 완료!\nAssets/Prefabs/Gimmicks/SHELL_Walls_20261118\n\n팀원 원본 씬·프리팹은 수정하지 않았습니다.", "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[SHELL Walls] " + ex);
            if (userInvoked) EditorUtility.DisplayDialog("SHELL Walls", ex.Message, "OK");
        }
        finally { busy = false; }
    }

    static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
