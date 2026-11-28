using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// Stage02 배경(BGTilemap)을 "Dungeon Wall Edited.png"로 교체하는 에디터 도구
//
// 왜 타일맵이 아니라 Tiled 스프라이트로 바꾸나:
//   새 이미지는 1254px인데, 기존처럼 8x8칸으로 자르면 한 칸이 156.75px(정수 아님)이 되어
//   타일 사이에 줄(이음새)이 생김. 이미지 자체는 상하좌우가 이어지는(seamless) 그림이라
//   SpriteRenderer의 Tiled 모드로 같은 영역에 반복해서 깔면 깨지지 않음.
//
// 메뉴: Tools > Stage02 > 배경 교체 (Dungeon Wall Edited)
//   1. 지금 BGTilemap을 프리팹으로 백업 (Assets/Art/Background/Backup/BGTilemap_Backup.prefab)
//   2. 씬 안의 BGTilemap은 지우지 않고 "BGTilemap_Backup"으로 이름 바꾸고 비활성화
//   3. 같은 위치·크기·정렬 순서·색으로 새 배경 스프라이트(BGSprite_DungeonWallEdited) 생성
//   Ctrl+Z로 되돌릴 수 있고, "배경 되돌리기" 메뉴로 원래대로 복구 가능
public static class Stage02BackgroundSwapper
{
    const string TexturePath = "Assets/Art/Background/Dungeon Wall Edited.png";
    const string BackupFolder = "Assets/Art/Background/Backup";
    const string BackupPrefabPath = BackupFolder + "/BGTilemap_Backup.prefab";

    const string TilemapName = "BGTilemap";
    const string BackupName = "BGTilemap_Backup";
    const string NewName = "BGSprite_DungeonWallEdited";

    // 기존 배경(Cainos 256px, PPU 32)은 8x8 유닛마다 무늬가 반복됨 → 새 이미지도 8유닛 크기로 맞춤
    const float RepeatWorldSize = 8f;

    [MenuItem("Tools/Stage02/배경 교체 (Dungeon Wall Edited)")]
    static void Swap()
    {
        GameObject tilemapGo = FindInScene(TilemapName);
        if (tilemapGo == null || !tilemapGo.TryGetComponent(out Tilemap tilemap))
        {
            EditorUtility.DisplayDialog("배경 교체", $"열린 씬에서 '{TilemapName}' 타일맵을 찾지 못했습니다.", "확인");
            return;
        }

        // 1. 백업 프리팹 저장 (타일 배치가 그대로 들어 있음)
        if (!AssetDatabase.IsValidFolder(BackupFolder))
            AssetDatabase.CreateFolder("Assets/Art/Background", "Backup");
        PrefabUtility.SaveAsPrefabAsset(tilemapGo, BackupPrefabPath, out bool saved);
        if (!saved)
        {
            EditorUtility.DisplayDialog("배경 교체", "백업 프리팹 저장에 실패해서 교체를 중단했습니다.", "확인");
            return;
        }

        // 2. 새 이미지 임포트 설정
        Sprite sprite = PrepareSprite();
        if (sprite == null)
        {
            EditorUtility.DisplayDialog("배경 교체", $"'{TexturePath}' 스프라이트를 불러오지 못했습니다.", "확인");
            return;
        }

        // 3. 기존 타일맵이 실제로 칠해진 영역 계산 (월드 좌표, 타일맵은 건드리지 않음)
        if (!TryGetPaintedCells(tilemap, out Vector3Int cellMin, out Vector3Int cellMax))
        {
            EditorUtility.DisplayDialog("배경 교체", $"'{TilemapName}'에 칠해진 타일이 없습니다.", "확인");
            return;
        }
        Vector3 worldMin = tilemap.CellToWorld(cellMin);
        Vector3 worldMax = tilemap.CellToWorld(cellMax);
        Vector3 center = (worldMin + worldMax) * 0.5f;
        Vector2 size = new Vector2(Mathf.Abs(worldMax.x - worldMin.x), Mathf.Abs(worldMax.y - worldMin.y));

        TilemapRenderer oldRenderer = tilemapGo.GetComponent<TilemapRenderer>();

        // 4. 새 배경 스프라이트 생성
        var newGo = new GameObject(NewName);
        Undo.RegisterCreatedObjectUndo(newGo, "배경 교체");
        newGo.layer = tilemapGo.layer;
        newGo.transform.SetParent(tilemapGo.transform.parent, false);
        newGo.transform.SetSiblingIndex(tilemapGo.transform.GetSiblingIndex());
        newGo.transform.position = new Vector3(center.x, center.y, tilemapGo.transform.position.z);

        var sr = newGo.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = size;
        sr.color = tilemap.color;
        if (oldRenderer != null)
        {
            sr.sharedMaterial = oldRenderer.sharedMaterial;
            sr.sortingLayerID = oldRenderer.sortingLayerID;
            sr.sortingOrder = oldRenderer.sortingOrder;
            sr.maskInteraction = oldRenderer.maskInteraction;
        }

        // 5. 기존 타일맵은 지우지 않고 비활성화
        Undo.RecordObject(tilemapGo, "배경 교체");
        tilemapGo.name = BackupName;
        tilemapGo.SetActive(false);

        EditorSceneManager.MarkSceneDirty(tilemapGo.scene);
        Selection.activeGameObject = newGo;

        Debug.Log($"[배경 교체] 완료. 영역 {size.x}x{size.y} (중심 {center}), 백업: {BackupPrefabPath}");
        EditorUtility.DisplayDialog("배경 교체",
            "배경을 교체했습니다.\n\n" +
            $"- 백업 프리팹: {BackupPrefabPath}\n" +
            $"- 씬 안의 기존 타일맵: '{BackupName}' (비활성화)\n\n" +
            "확인 후 씬을 저장하세요. 되돌리려면 Ctrl+Z 또는 Tools > Stage02 > 배경 되돌리기", "확인");
    }

    [MenuItem("Tools/Stage02/배경 되돌리기")]
    static void Revert()
    {
        GameObject backup = FindInScene(BackupName);
        if (backup == null)
        {
            EditorUtility.DisplayDialog("배경 되돌리기", $"씬에서 '{BackupName}'를 찾지 못했습니다.\n백업 프리팹({BackupPrefabPath})을 씬에 직접 끌어다 놓아도 됩니다.", "확인");
            return;
        }

        GameObject newGo = FindInScene(NewName);
        if (newGo != null)
            Undo.DestroyObjectImmediate(newGo);

        Undo.RecordObject(backup, "배경 되돌리기");
        backup.name = TilemapName;
        backup.SetActive(true);
        EditorSceneManager.MarkSceneDirty(backup.scene);
        Debug.Log("[배경 되돌리기] 기존 BGTilemap을 복구했습니다.");
    }

    // 새 이미지를 Tiled 스프라이트로 쓸 수 있게 임포트 설정
    static Sprite PrepareSprite()
    {
        var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer == null) return null;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Repeat;           // 반복 경계에서 반대쪽 픽셀과 섞이도록
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed; // 1254px은 4의 배수가 아니라 압축 시 품질 저하
        importer.maxTextureSize = 2048;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;    // Tiled 모드는 Full Rect 필요
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        importer.SetTextureSettings(settings);

        // 이미지 한 장 = 8유닛 (기존 벽돌 크기와 동일)
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        int width = tex != null ? tex.width : 1254;
        importer.spritePixelsPerUnit = width / RepeatWorldSize;

        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(TexturePath);
    }

    // 타일이 있는 칸의 최소 칸 ~ (최대 칸 + 1) 범위
    static bool TryGetPaintedCells(Tilemap tilemap, out Vector3Int min, out Vector3Int max)
    {
        min = new Vector3Int(int.MaxValue, int.MaxValue, 0);
        max = new Vector3Int(int.MinValue, int.MinValue, 0);
        bool any = false;

        foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
        {
            if (!tilemap.HasTile(pos)) continue;
            any = true;
            min = Vector3Int.Min(min, new Vector3Int(pos.x, pos.y, 0));
            max = Vector3Int.Max(max, new Vector3Int(pos.x + 1, pos.y + 1, 0));
        }
        return any;
    }

    // 비활성 오브젝트까지 포함해서 열린 씬에서 이름으로 찾기
    static GameObject FindInScene(string objectName)
    {
        var scene = EditorSceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == objectName)
                    return t.gameObject;
            }
        }
        return null;
    }
}
