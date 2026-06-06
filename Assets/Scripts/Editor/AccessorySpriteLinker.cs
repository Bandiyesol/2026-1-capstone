#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity 상단 메뉴 Tools → Link Accessory Sprites 로 실행.
/// Assets/Arts/Icons/Accessory/ 폴더의 PNG 파일 이름을 기준으로
/// Assets/Data/Accessory/ 폴더의 SO에 스프라이트를 자동 연결한다.
/// 규칙: ACC_C_001.png → ACC_C_001.asset
/// </summary>
public static class AccessorySpriteLinker
{
    const string SpritePath    = "Assets/Arts/Icons/Accessory";
    const string SOPath        = "Assets/Data/Accessory";

    [MenuItem("Tools/Link Accessory Sprites")]
    public static void LinkSprites()
    {
        // SO 전체 로드
        string[] soGuids = AssetDatabase.FindAssets("t:AccessoryData", new[] { SOPath });

        int linked   = 0;
        int notFound = 0;

        foreach (string guid in soGuids)
        {
            string soPath = AssetDatabase.GUIDToAssetPath(guid);
            AccessoryData so = AssetDatabase.LoadAssetAtPath<AccessoryData>(soPath);
            if (so == null) continue;

            // SO 파일명 추출 (예: ACC_C_001)
            string soName = System.IO.Path.GetFileNameWithoutExtension(soPath);

            // 같은 이름의 스프라이트 찾기
            string spritePath = $"{SpritePath}/{soName}.png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

            if (sprite != null)
            {
                so.icon = sprite;
                EditorUtility.SetDirty(so);
                linked++;
            }
            else
            {
                Debug.LogWarning($"[SpriteLinker] 스프라이트 없음: {spritePath}");
                notFound++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SpriteLinker] 완료 — 연결: {linked}개 / 미발견: {notFound}개");
        EditorUtility.DisplayDialog("완료",
            $"스프라이트 연결 완료!\n연결: {linked}개\n미발견: {notFound}개",
            "확인");
    }
}
#endif
