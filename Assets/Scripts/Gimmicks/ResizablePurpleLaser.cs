using UnityEngine;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

/// <summary>
/// Reusable 2D laser: stationary-size end emitters, tileable center beam,
/// animated jagged purple electricity, and synchronized trigger collider.
/// Generated from Tools > Generate Purple Laser.
/// </summary>
[ExecuteAlways]
public class ResizablePurpleLaser : MonoBehaviour
{
    [Header("Resize here (emitter proportions stay fixed)")]
    [Min(0.5f)] public float beamLength = 5f;

    [Header("Electric effect")]
    [Range(0.02f, 0.30f)] public float zigzagHeight = 0.13f;
    [Range(5f, 40f)] public float animationFPS = 20f;
    public bool animate = true;

    [SerializeField, HideInInspector] private SpriteRenderer leftEmitter;
    [SerializeField, HideInInspector] private SpriteRenderer rightEmitter;
    [SerializeField, HideInInspector] private SpriteRenderer centerBeam;
    [SerializeField, HideInInspector] private LineRenderer purpleGlow;
    [SerializeField, HideInInspector] private LineRenderer purpleArc;
    [SerializeField, HideInInspector] private LineRenderer brightArc;
    [SerializeField, HideInInspector] private BoxCollider2D laserTrigger;

    private float nextAnimationTime;
    private const int Segments = 20;

    // Called once by the editor generator.
    public void Configure(SpriteRenderer left, SpriteRenderer center, SpriteRenderer right,
        LineRenderer glow, LineRenderer purple, LineRenderer bright, BoxCollider2D trigger)
    {
        leftEmitter = left;
        centerBeam = center;
        rightEmitter = right;
        purpleGlow = glow;
        purpleArc = purple;
        brightArc = bright;
        laserTrigger = trigger;
        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void OnValidate()
    {
        beamLength = Mathf.Max(0.5f, beamLength);
#if UNITY_EDITOR
        // Inspector edits happen in the editor; defer changes to child components
        // until Unity has finished validating serialized properties.
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall += RefreshAfterValidation;
            return;
        }
#endif
        Refresh();
    }

#if UNITY_EDITOR
    private void RefreshAfterValidation()
    {
        if (this != null) Refresh();
    }
#endif

    private void Refresh()
    {
        float half = beamLength * 0.5f;
        const float emitterInset = 0.31f;

        if (leftEmitter != null)
            leftEmitter.transform.localPosition = new Vector3(-half - emitterInset, 0f, 0f);
        if (rightEmitter != null)
            rightEmitter.transform.localPosition = new Vector3(half + emitterInset, 0f, 0f);

        if (centerBeam != null)
        {
            centerBeam.transform.localPosition = Vector3.zero;
            centerBeam.drawMode = SpriteDrawMode.Tiled;
            centerBeam.size = new Vector2(beamLength, 0.25f);
        }

        if (laserTrigger != null)
        {
            laserTrigger.isTrigger = true;
            laserTrigger.offset = Vector2.zero;
            laserTrigger.size = new Vector2(beamLength, 0.34f);
        }

        DrawElectricity(0f);
    }

    private void Update()
    {
        if (!Application.isPlaying || !animate || Time.time < nextAnimationTime)
            return;
        nextAnimationTime = Time.time + 1f / animationFPS;
        DrawElectricity(Mathf.Floor(Time.time * animationFPS) * 0.23f);
    }

    private void DrawElectricity(float phase)
    {
        float half = beamLength * 0.5f;

        if (purpleGlow != null) purpleGlow.positionCount = Segments + 1;
        if (purpleArc != null) purpleArc.positionCount = Segments + 1;
        if (brightArc != null) brightArc.positionCount = Segments + 1;

        for (int i = 0; i <= Segments; ++i)
        {
            float t = i / (float)Segments;
            float x = Mathf.Lerp(-half, half, t);
            float envelope = Mathf.Sin(t * Mathf.PI); // Endpoints stay attached to emitters.
            float zig = Mathf.Sin(i * 3.7f + phase * 4.1f)
                      + 0.5f * Mathf.Sin(i * 6.9f - phase * 2.7f);
            float y = Mathf.Round(zig * zigzagHeight * envelope / 0.025f) * 0.025f;
            Vector3 p = new Vector3(x, y, -0.01f);

            if (purpleGlow != null) purpleGlow.SetPosition(i, p);
            if (purpleArc != null) purpleArc.SetPosition(i, p);
            if (brightArc != null)
            {
                float fineZig = Mathf.Sin(i * 5.1f - phase * 5.7f) * 0.025f * envelope;
                brightArc.SetPosition(i, new Vector3(x, y + fineZig, -0.02f));
            }
        }
    }
}

#if UNITY_EDITOR
/// <summary>One-click prefab and pixel-art generation. Safe: never overwrites Laser.prefab.</summary>
public static class PurpleLaserPrefabGenerator
{
    private const string ArtDir = "Assets/Art/Gimmicks/PurpleLaserGenerated";
    private const string PrefabDir = "Assets/Prefabs/Gimmicks";
    private const string PrefabBase = "Assets/Prefabs/Gimmicks/Laser_Resizable.prefab";

    [MenuItem("Tools/Generate Purple Laser")]
    public static void Generate()
    {
        Directory.CreateDirectory(ArtDir);
        Directory.CreateDirectory(PrefabDir);
        AssetDatabase.Refresh();

        string emitterPath = ArtDir + "/Emitter_Black.png";
        string beamPath = ArtDir + "/Beam_Purple_7100FF.png";
        string materialPath = ArtDir + "/PurpleLaser_Sprite.mat";

        CreateSpriteFile(emitterPath, 24, 36, CreateEmitterPixels());
        CreateSpriteFile(beamPath, 16, 8, CreateBeamPixels());

        Sprite emitterSprite = AssetDatabase.LoadAssetAtPath<Sprite>(emitterPath);
        Sprite beamSprite = AssetDatabase.LoadAssetAtPath<Sprite>(beamPath);
        if (emitterSprite == null || beamSprite == null)
        {
            Debug.LogError("Laser generator: could not import sprite textures.");
            return;
        }

        Material electricMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (electricMaterial == null)
        {
            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader == null)
            {
                Debug.LogError("Laser generator: Sprites/Default shader not found.");
                return;
            }
            electricMaterial = new Material(spriteShader);
            AssetDatabase.CreateAsset(electricMaterial, materialPath);
        }

        GameObject root = new GameObject("Laser_Resizable");
        ResizablePurpleLaser controller = root.AddComponent<ResizablePurpleLaser>();

        SpriteRenderer beam = NewSprite(root, "CenterBeam_Tiled", beamSprite, 1);
        beam.drawMode = SpriteDrawMode.Tiled;
        beam.tileMode = SpriteTileMode.Continuous;
        SpriteRenderer left = NewSprite(root, "LeftEmitter_Black", emitterSprite, 10);
        SpriteRenderer right = NewSprite(root, "RightEmitter_Black", emitterSprite, 10);
        right.flipX = true;

        LineRenderer glow = NewLine(root, "ElectricGlow", electricMaterial,
            new Color(113f / 255f, 0f, 1f, 0.25f), 0.18f, 3);
        LineRenderer purple = NewLine(root, "ElectricZigzag_7100FF", electricMaterial,
            new Color(113f / 255f, 0f, 1f, 1f), 0.075f, 4);
        LineRenderer bright = NewLine(root, "ElectricBrightCore", electricMaterial,
            new Color(0.88f, 0.77f, 1f, 0.92f), 0.026f, 5);

        BoxCollider2D trigger = root.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        controller.Configure(left, beam, right, glow, purple, bright, trigger);

        string uniquePrefabPath = AssetDatabase.GenerateUniqueAssetPath(PrefabBase);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, uniquePrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();

        if (prefab == null)
        {
            Debug.LogError("Laser generator: failed to save prefab.");
            return;
        }

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log("Purple laser ready! Drag " + uniquePrefabPath +
                  " into the Scene. Change 'Beam Length' on the root component. " +
                  "The BoxCollider2D is a trigger; connect your project's existing damage logic separately.");
        EditorUtility.DisplayDialog("Purple Laser ready",
            "Created: " + uniquePrefabPath + "\n\n" +
            "Color: #7100FF; black emitters; tiled beam; animated zigzag.\n" +
            "Drag prefab into Scene, press Play, and adjust Beam Length.\n\n" +
            "IMPORTANT: This prefab includes a trigger collider, but not a damage/death script.",
            "OK");
    }

    private static SpriteRenderer NewSprite(GameObject parent, string name, Sprite sprite, int order)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        SpriteRenderer sr = child.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }

    private static LineRenderer NewLine(GameObject parent, string name, Material material,
        Color color, float width, int order)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        LineRenderer lr = child.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.alignment = LineAlignment.View;
        lr.sharedMaterial = material;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.startColor = color;
        lr.endColor = color;
        lr.numCapVertices = 0;
        lr.numCornerVertices = 0;
        lr.sortingOrder = order;
        lr.positionCount = 21;
        return lr;
    }

    private static void CreateSpriteFile(string path, int width, int height, Color32[] pixels)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    // Hand-drawn 24x36 black/gunmetal pixel-art emitter with a purple port.
    private static Color32[] CreateEmitterPixels()
    {
        Color32[] pixels = new Color32[24 * 36];
        for (int y = 0; y < 36; ++y)
        for (int x = 0; x < 24; ++x)
        {
            if (x < 2 || x > 21 || y < 3 || y > 32) continue;
            bool corner = (x < 5 || x > 18) && (y < 6 || y > 29);
            if (corner) continue;

            Color32 c = new Color32(29, 30, 38, 255);
            if (x == 2 || x == 21 || y == 3 || y == 32) c = new Color32(8, 9, 14, 255);
            else if (x < 5 || y < 6 || y > 29) c = new Color32(83, 88, 97, 255);
            else if (x < 9) c = new Color32(39, 43, 52, 255);
            else if (x >= 9 && x < 12) c = new Color32(57, 60, 70, 255);
            if ((y == 9 || y == 26) && x >= 5 && x <= 19)
                c = new Color32(108, 111, 122, 255);
            if (x >= 16 && y >= 13 && y <= 22)
                c = new Color32(17, 12, 31, 255);
            if (x >= 18 && y >= 15 && y <= 20)
                c = new Color32(113, 0, 255, 255); // #7100FF
            if (x >= 20 && y >= 16 && y <= 19)
                c = new Color32(207, 170, 255, 255);
            pixels[y * 24 + x] = c;
        }
        return pixels;
    }

    // Repeats horizontally: exact #7100FF with a pale electric core.
    private static Color32[] CreateBeamPixels()
    {
        Color32[] pixels = new Color32[16 * 8];
        for (int y = 0; y < 8; ++y)
        for (int x = 0; x < 16; ++x)
        {
            Color32 c = new Color32(0, 0, 0, 0);
            if (y == 0 || y == 7) c = new Color32(113, 0, 255, 45);
            if (y == 1 || y == 6) c = new Color32(113, 0, 255, 110);
            if (y == 2 || y == 5) c = new Color32(113, 0, 255, 235);
            if (y == 3 || y == 4) c = new Color32(189, 119, 255, 255);
            pixels[y * 16 + x] = c;
        }
        return pixels;
    }
}
#endif
