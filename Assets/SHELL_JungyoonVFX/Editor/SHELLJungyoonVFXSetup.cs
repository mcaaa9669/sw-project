using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class SHELLJungyoonVFXSetup
{
    private const string Root = "Assets/SHELL_JungyoonVFX";
    private const string ResourcesFolder = Root + "/Resources/SHELL_JungyoonVFX";
    private const string LeafPath = "Assets/SHELL_AdvancedGimmicks_20261118/Art/V2/LeafParticle_32.png";
    private const string SparkPath = "Assets/Cainos/Interactive Pixel Water/Texture/TX FX Particle Lava Spark.png";

    [MenuItem("SHELL/Jungyoon VFX/Build and Validate Assets")]
    public static void BuildAndValidateAssets()
    {
        Sprite[] corpseFrames = ImportFrames("FX_CorpseSpawn");
        Sprite[] soulFrames = ImportFrames("FX_SoulShift");
        BuildPrefab("FX_CorpseSpawn", corpseFrames, 0.36f);
        BuildPrefab("FX_SoulShift", soulFrames, 0.40f);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("SHELL Jungyoon VFX: both 8-frame spritesheets and runtime prefabs validated.");
    }

    [MenuItem("SHELL/Jungyoon VFX/Build Stage Effects")]
    public static void BuildStageEffects()
    {
        Sprite leaf = AssetDatabase.LoadAssetAtPath<Sprite>(LeafPath);
        if (leaf == null)
            throw new InvalidOperationException($"Leaf sprite not found: {LeafPath}");

        Sprite[] dustFrames = ImportFrames("FX_CorpseSpawn");
        Sprite[] pulseFrames = ImportFrames("FX_CheckpointPulse");
        BuildForestLeaves(leaf);
        BuildLandingDust(leaf, dustFrames);
        BuildPrefab("FX_CheckpointPulse", pulseFrames, 0.32f);
        AssetDatabase.SaveAssets();
        Debug.Log("SHELL Jungyoon VFX: stage effect prefabs ready.");
    }

    [MenuItem("SHELL/Jungyoon VFX/Build Button and Platform Effects")]
    public static void BuildGimmickEffects()
    {
        Sprite spark = AssetDatabase.LoadAssetAtPath<Sprite>(SparkPath);
        if (spark == null)
            throw new InvalidOperationException($"Pixel spark sprite not found: {SparkPath}");

        Sprite[] pulseFrames = ImportFrames("FX_CheckpointPulse");
        CreatePrefabIfMissing("FX_ButtonPress", root =>
        {
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = pulseFrames[0];
            renderer.color = new Color(0.92f, 0.97f, 0.86f, 0.62f);
            renderer.sortingOrder = 2;
            root.AddComponent<SHELLVFXFramePlayer>().Configure(pulseFrames, 0.24f);
        });

        CreatePrefabIfMissing("FX_PlatformReveal", root =>
        {
            SHELLPlatformEdgeSparkle effect = root.AddComponent<SHELLPlatformEdgeSparkle>();
            Vector2[] previewPositions =
            {
                new(-1.8f, 0.07f), new(-1.44f, 0.35f), new(-0.61f, 0.35f),
                new(0.25f, 0.35f), new(1.3f, 0.35f), new(1.8f, 0.16f)
            };
            SpriteRenderer[] glints = new SpriteRenderer[previewPositions.Length];
            for (int i = 0; i < glints.Length; i++)
            {
                SpriteRenderer renderer = AddSprite(root.transform, $"Edge Spark {i + 1}", spark, 2);
                renderer.transform.localPosition = previewPositions[i];
                renderer.color = new Color(0.88f, 0.94f, 0.64f, 0.65f);
                glints[i] = renderer;
            }
            AssignRenderers(effect, "glints", glints);
        });

        AssetDatabase.SaveAssets();
        Debug.Log("SHELL Jungyoon VFX: button and platform effect prefabs ready.");
    }

    private static void BuildForestLeaves(Sprite leaf)
    {
        const string name = "FX_ForestLeaves";
        CreatePrefabIfMissing(name, root =>
        {
            SHELLForestLeaves effect = root.AddComponent<SHELLForestLeaves>();
            SpriteRenderer[] renderers = new SpriteRenderer[6];
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = AddSprite(root.transform, $"Leaf {i + 1}", leaf, -5);
                renderer.transform.localPosition = new Vector3(((i * 7) % 11 - 5) * 0.65f, 1.7f - i * 0.65f, 0f);
                renderer.transform.localScale = new Vector3(0.34f, 0.44f, 1f);
                renderer.color = new Color(0.83f, 0.9f, 0.68f, i == 5 ? 0f : 0.48f);
                renderers[i] = renderer;
            }
            AssignRenderers(effect, "leaves", renderers);
        });
    }

    private static void BuildLandingDust(Sprite leaf, Sprite[] dustFrames)
    {
        const string name = "FX_LandingDust";
        CreatePrefabIfMissing(name, root =>
        {
            SpriteRenderer dust = AddSprite(root.transform, "Dust", dustFrames[0], 6);
            dust.transform.localPosition = Vector3.up * 0.22f;
            dust.transform.localScale = new Vector3(0.52f, 0.52f, 1f);
            SHELLVFXFramePlayer frames = root.AddComponent<SHELLVFXFramePlayer>();
            frames.Configure(dustFrames, 0.18f, dust);

            SHELLLandingGrass grass = root.AddComponent<SHELLLandingGrass>();
            SpriteRenderer[] blades = new SpriteRenderer[3];
            for (int i = 0; i < blades.Length; i++)
            {
                SpriteRenderer blade = AddSprite(root.transform, $"Grass {i + 1}", leaf, 7);
                float x = i == 1 ? -0.07f : (i - 1) * 0.18f;
                blade.transform.localPosition = new Vector3(x, 0.04f, 0f);
                blade.transform.localScale = new Vector3(0.08f, 0.21f + (i % 2) * 0.06f, 1f);
                blade.color = new Color(0.76f, 0.97f, 0.46f, 0.78f);
                blades[i] = blade;
            }
            AssignRenderers(grass, "blades", blades);
        });
    }

    private static SpriteRenderer AddSprite(Transform root, string name, Sprite sprite, int sortingOrder)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(root, false);
        SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private static void AssignRenderers(MonoBehaviour component, string propertyName, SpriteRenderer[] renderers)
    {
        SerializedObject settings = new SerializedObject(component);
        SerializedProperty property = settings.FindProperty(propertyName);
        property.arraySize = renderers.Length;
        for (int i = 0; i < renderers.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
        settings.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreatePrefabIfMissing(string name, Action<GameObject> configure)
    {
        string path = $"{ResourcesFolder}/{name}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            return;

        GameObject root = new GameObject(name);
        try
        {
            configure(root);
            if (PrefabUtility.SaveAsPrefabAsset(root, path) == null)
                throw new InvalidOperationException($"Could not save VFX prefab: {path}");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static Sprite[] ImportFrames(string assetName)
    {
        string path = $"{Root}/Sprites/{assetName}.png";
        ValidatePng(path);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException($"Texture importer not found: {path}");

        Sprite[] frames = LoadFrames(path);
        if (HasExpectedFrames(frames, assetName) &&
            importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Multiple &&
            importer.spritePixelsPerUnit == 25f && importer.filterMode == FilterMode.Point &&
            importer.textureCompression == TextureImporterCompression.Uncompressed && !importer.mipmapEnabled &&
            !importer.isReadable && importer.alphaIsTransparency && importer.wrapMode == TextureWrapMode.Clamp)
            return frames;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 25f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.isReadable = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();
        SpriteRect[] previousSlices = dataProvider.GetSpriteRects();
        SpriteRect[] slices = new SpriteRect[8];
        for (int i = 0; i < slices.Length; i++)
        {
            string name = $"{assetName}_{i:00}";
            SpriteRect previousSlice = Array.Find(previousSlices, slice => slice.name == name);
            slices[i] = new SpriteRect
            {
                name = name,
                spriteID = previousSlice?.spriteID ?? GUID.Generate(),
                rect = new Rect(i * 64, 0, 64, 64),
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                border = Vector4.zero
            };
        }

        dataProvider.SetSpriteRects(slices);
        dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(
            slices.Select(slice => new SpriteNameFileIdPair(slice.name, slice.spriteID)));
        dataProvider.Apply();
        importer.SaveAndReimport();

        frames = LoadFrames(path);
        if (!HasExpectedFrames(frames, assetName))
            throw new InvalidOperationException($"{path} must import as exactly eight 64x64 sprites.");

        return frames;
    }

    private static Sprite[] LoadFrames(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool HasExpectedFrames(Sprite[] frames, string assetName)
    {
        return frames.Length == 8 && frames.Select((sprite, index) =>
            sprite.name == $"{assetName}_{index:00}" && sprite.rect == new Rect(index * 64, 0, 64, 64) &&
            sprite.pivot == new Vector2(32, 32)).All(matches => matches);
    }

    private static void ValidatePng(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Required VFX spritesheet is missing.", path);

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
        try
        {
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false) ||
                texture.width != 512 || texture.height != 64)
                throw new InvalidOperationException($"{path} must be a readable 512x64 PNG.");

            Color32[] pixels = texture.GetPixels32();
            for (int frame = 0; frame < 8; frame++)
            {
                bool hasTransparent = false;
                bool hasVisible = false;
                int minX = 64, minY = 64, maxX = -1, maxY = -1;
                for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    Color32 pixel = pixels[y * 512 + frame * 64 + x];
                    hasTransparent |= pixel.a == 0;
                    if (pixel.a == 0)
                        continue;

                    hasVisible = true;
                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }

                if (!hasTransparent || !hasVisible || minX == 0 || minY == 0 || maxX == 63 || maxY == 63)
                    throw new InvalidOperationException($"{path} frame {frame} must have visible pixels and transparent margins.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static void BuildPrefab(string assetName, Sprite[] frames, float duration)
    {
        string path = $"{ResourcesFolder}/{assetName}.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null && existing.TryGetComponent(out SpriteRenderer existingRenderer) &&
            existingRenderer.sprite == frames[0] && existingRenderer.sortingOrder == 20 &&
            existing.TryGetComponent(out SHELLVFXFramePlayer existingPlayer))
        {
            SerializedObject settings = new SerializedObject(existingPlayer);
            SerializedProperty existingFrames = settings.FindProperty("frames");
            if (settings.FindProperty("duration").floatValue == duration && settings.FindProperty("destroyAtEnd").boolValue &&
                existingFrames.arraySize == frames.Length && frames.Select((sprite, index) =>
                    existingFrames.GetArrayElementAtIndex(index).objectReferenceValue == sprite).All(matches => matches))
                return;
        }

        GameObject effect = new GameObject(assetName);
        try
        {
            SpriteRenderer renderer = effect.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.sortingOrder = 20;

            SHELLVFXFramePlayer player = effect.AddComponent<SHELLVFXFramePlayer>();
            player.Configure(frames, duration);

            if (PrefabUtility.SaveAsPrefabAsset(effect, path) == null)
                throw new InvalidOperationException($"Could not save VFX prefab: {path}");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(effect);
        }
    }
}
