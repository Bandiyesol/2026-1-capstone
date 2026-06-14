#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 악세사리 테스트용 치트 에디터 창
/// Tools → Accessory Cheat Window 로 열기
/// </summary>
public class AccessoryCheatWindow : EditorWindow
{
    Vector2 scrollPos;
    string searchFilter = "";
    List<AccessoryData> allAccessories = new List<AccessoryData>();
    bool loaded = false;

    [MenuItem("Tools/Accessory Cheat Window")]
    static void Open() => GetWindow<AccessoryCheatWindow>("악세사리 치트");

    void OnEnable()
    {
        LoadAccessories();
    }

    void LoadAccessories()
    {
        allAccessories.Clear();
        string[] guids = AssetDatabase.FindAssets("t:AccessoryData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AccessoryData data = AssetDatabase.LoadAssetAtPath<AccessoryData>(path);
            if (data != null) allAccessories.Add(data);
        }
        // 이름순 정렬
        allAccessories.Sort((a, b) => string.Compare(a.name, b.name));
        loaded = true;
    }

    void OnGUI()
    {
        if (!loaded || allAccessories.Count == 0)
        {
            if (GUILayout.Button("악세사리 목록 로드")) LoadAccessories();
            return;
        }

        EditorGUILayout.LabelField($"총 {allAccessories.Count}종", EditorStyles.boldLabel);

        // 검색
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("검색:", GUILayout.Width(40));
        searchFilter = EditorGUILayout.TextField(searchFilter);
        if (GUILayout.Button("새로고침", GUILayout.Width(70))) LoadAccessories();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 플레이 중인지 체크
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("플레이 모드에서만 악세사리를 추가할 수 있어요!", MessageType.Warning);
            return;
        }

        if (AccessoryManager.instance == null)
        {
            EditorGUILayout.HelpBox("AccessoryManager를 찾을 수 없어요!", MessageType.Error);
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        foreach (AccessoryData data in allAccessories)
        {
            if (!string.IsNullOrEmpty(searchFilter) &&
                !data.displayName.Contains(searchFilter) &&
                !data.name.Contains(searchFilter))
                continue;

            EditorGUILayout.BeginHorizontal();

            // 등급 색상
            Color prev = GUI.color;
            GUI.color = GetGradeColor(data.grade);
            EditorGUILayout.LabelField($"[{data.grade}]", GUILayout.Width(70));
            GUI.color = prev;

            EditorGUILayout.LabelField(data.displayName, GUILayout.Width(180));
            EditorGUILayout.LabelField(data.effectType.ToString(), GUILayout.Width(150));

            if (GUILayout.Button("추가", GUILayout.Width(50)))
            {
                AccessoryManager.instance.Add(data);
                Debug.Log($"[치트] {data.displayName} 추가됨!");
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    Color GetGradeColor(AccessoryGrade grade)
    {
        return grade switch
        {
            AccessoryGrade.Common    => Color.white,
            AccessoryGrade.Rare      => new Color(0.4f, 0.6f, 1f),
            AccessoryGrade.Unique    => new Color(0.8f, 0.4f, 1f),
            AccessoryGrade.Legendary => new Color(1f, 0.7f, 0.2f),
            _                        => Color.white
        };
    }
}
#endif
