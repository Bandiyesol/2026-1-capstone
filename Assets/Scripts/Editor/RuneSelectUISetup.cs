#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>룬 선택 UI를 무기 선택 UI와 같은 3카드 + 로드아웃 패널로 맞춥니다.</summary>
public static class RuneSelectUISetup
{
    const string ScenePath = "Assets/Scenes/ProtoType_LTG.unity";

    [MenuItem("Tools/Rune/Diagnose Rune Select UI")]
    public static void DiagnoseFromMenu()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!Application.isPlaying && scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Debug.Log(BuildDiagnosisReport());
        EditorUtility.DisplayDialog("Rune Select UI 진단", BuildDiagnosisReport(), "확인");
    }

    [MenuItem("Tools/Rune/Setup Rune Select UI")]
    public static void SetupFromMenu()
    {
        Apply(saveScene: true, showDialog: true);
    }

    [MenuItem("Tools/Rune/Fix Rune Button Wiring")]
    public static void FixButtonWiringFromMenu()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!Application.isPlaying && scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        RuneSelectUI runeUi = ResolveCanonicalRuneSelectUi();
        if (runeUi == null)
        {
            EditorUtility.DisplayDialog("Rune Select UI", "RuneSelectUI를 찾지 못했습니다.", "확인");
            return;
        }

        int cleared = ClearWeaponPickEventsUnder(runeUi.transform);
        EditorUtility.SetDirty(runeUi);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        EditorUtility.DisplayDialog(
            "Rune Select UI",
            $"WeaponSelectUI.OnPickWeapon persistent 이벤트 {cleared}개를 제거했습니다.\n플레이 시 RuneSelectUI.OnPickRune으로 연결됩니다.",
            "확인");
    }

    public static void Apply(bool saveScene, bool showDialog)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!Application.isPlaying && scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        RuneSelectUI runeUi = ResolveCanonicalRuneSelectUi();
        if (runeUi == null)
        {
            EditorUtility.DisplayDialog("Rune Select UI", "씬에서 RuneSelectUI를 찾지 못했습니다.", "확인");
            return;
        }

        var weaponUi = Object.FindFirstObjectByType<WeaponSelectUI>(FindObjectsInactive.Include);
        if (weaponUi == null)
        {
            EditorUtility.DisplayDialog("Rune Select UI", "WeaponSelectUI 템플릿이 없습니다.", "확인");
            return;
        }

        Transform runeRoot = runeUi.transform;
        runeRoot.gameObject.name = "RuneSelectUI";
        EnsureFullScreenRoot(runeRoot);

        Transform legacy = runeRoot.Find("RuneListGroup");
        if (legacy != null)
            legacy.gameObject.SetActive(false);

        Transform clear = runeRoot.Find("ClearSlotButton");
        if (clear != null)
            clear.gameObject.SetActive(false);

        Transform choicePanel = EnsureChoicePanel(runeRoot, weaponUi.transform);
        Transform loadoutPanel = EnsureLoadoutPanel(runeRoot, weaponUi.transform);

        ChoiceSelectUILayout.Apply(runeRoot);
        EnsureFullScreenRoot(runeRoot);

        WireChoiceButtons(runeUi, choicePanel);
        WireLoadoutPanel(runeUi, loadoutPanel);
        WireGameManager(runeUi);

        SerializedObject runeSo = new SerializedObject(runeUi);
        runeSo.FindProperty("useAutoLayout").boolValue = false;
        runeSo.ApplyModifiedPropertiesWithoutUndo();

        runeRoot.gameObject.SetActive(false);
        EditorUtility.SetDirty(runeUi);

        if (saveScene && !Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        string report = "[RuneSelectUISetup] 설정 완료.\n\n" + BuildDiagnosisReport();
        Debug.Log(report);
        if (showDialog)
            EditorUtility.DisplayDialog("Rune Select UI", report, "확인");
    }

    static RuneSelectUI ResolveCanonicalRuneSelectUi()
    {
        var all = Object.FindObjectsByType<RuneSelectUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (all == null || all.Length == 0)
            return null;

        RuneSelectUI best = all[0];
        int bestScore = ScoreRuneSelectUi(best);

        for (int i = 1; i < all.Length; i++)
        {
            int score = ScoreRuneSelectUi(all[i]);
            if (score > bestScore)
            {
                best = all[i];
                bestScore = score;
            }
        }

        foreach (RuneSelectUI ui in all)
        {
            if (ui == best)
                continue;

            Debug.LogWarning($"[RuneSelectUISetup] 중복 RuneSelectUI 제거: {ui.gameObject.name}", ui);
            Object.DestroyImmediate(ui.gameObject);
        }

        return best;
    }

    static int ScoreRuneSelectUi(RuneSelectUI ui)
    {
        int score = 0;
        if (ui.transform.Find("ChoicePanel") != null)
            score += 10;
        if (ui.transform.Find("LoadoutPanel") != null)
            score += 5;
        if (ui.transform.Find("SlotGroup") != null)
            score += 5;

        SerializedObject so = new SerializedObject(ui);
        if (so.FindProperty("choiceButtons").arraySize >= 3)
            score += 3;
        if (so.FindProperty("slotButtons").arraySize >= 3)
            score += 3;

        return score;
    }

    static Transform EnsureChoicePanel(Transform runeRoot, Transform weaponRoot)
    {
        Transform choicePanel = runeRoot.Find("ChoicePanel");
        if (choicePanel != null)
            return choicePanel;

        Transform template = weaponRoot.Find("ContentPanel");
        if (template == null)
            return null;

        var clone = Object.Instantiate(template.gameObject, runeRoot);
        clone.name = "ChoicePanel";
        clone.transform.SetAsFirstSibling();
        return clone.transform;
    }

    static Transform EnsureLoadoutPanel(Transform runeRoot, Transform weaponRoot)
    {
        Transform loadout = runeRoot.Find("LoadoutPanel");
        if (loadout != null)
            return loadout;

        // 잘못 복제된 두 번째 ContentPanel이 있으면 LoadoutPanel로 이름 변경
        foreach (Transform child in runeRoot)
        {
            if (child.name == "ContentPanel" && child.Find("BoxPanel") != null)
            {
                child.name = "LoadoutPanel";
                return child;
            }
        }

        Transform template = weaponRoot.Find("ContentPanel");
        if (template == null)
            return null;

        var clone = Object.Instantiate(template.gameObject, runeRoot);
        clone.name = "LoadoutPanel";
        return clone.transform;
    }

    static void WireLoadoutPanel(RuneSelectUI runeUi, Transform loadoutPanel)
    {
        if (loadoutPanel == null)
            return;

        Transform boxPanel = FindBoxPanel(loadoutPanel);
        if (boxPanel == null)
            return;

        var slotButtons = new Button[3];
        var slotIcons = new Image[3];
        var slotNames = new TextMeshProUGUI[3];
        TextMeshProUGUI loadoutTitle = null;

        for (int i = 0; i < 3; i++)
        {
            Transform btn = boxPanel.Find($"Btn{i}");
            if (btn == null)
                continue;

            Button button = btn.GetComponent<Button>();
            ClearPersistentOnClick(button);
            slotButtons[i] = button;
            slotIcons[i] = btn.Find("Icon")?.GetComponent<Image>();
            slotNames[i] = btn.Find("Title")?.GetComponent<TextMeshProUGUI>();
        }

        foreach (Transform child in boxPanel)
        {
            if (child.name.StartsWith("Btn"))
                continue;

            loadoutTitle = child.GetComponent<TextMeshProUGUI>();
            if (loadoutTitle != null)
                break;
        }

        Transform warning = loadoutPanel.Find("WarningText");
        if (warning == null)
            warning = CreateWarningText(loadoutPanel).transform;

        Transform start = loadoutPanel.Find("StartButton");
        if (start == null)
            start = CreateStartButton(loadoutPanel).transform;

        ClearPersistentOnClick(warning?.GetComponent<Button>());
        ClearPersistentOnClick(start?.GetComponent<Button>());

        SerializedObject so = new SerializedObject(runeUi);
        so.FindProperty("loadoutPanelRoot").objectReferenceValue = loadoutPanel.gameObject;
        so.FindProperty("slotButtons").arraySize = 3;
        so.FindProperty("slotIcons").arraySize = 3;
        so.FindProperty("slotNames").arraySize = 3;

        for (int i = 0; i < 3; i++)
        {
            so.FindProperty("slotButtons").GetArrayElementAtIndex(i).objectReferenceValue = slotButtons[i];
            so.FindProperty("slotIcons").GetArrayElementAtIndex(i).objectReferenceValue = slotIcons[i];
            so.FindProperty("slotNames").GetArrayElementAtIndex(i).objectReferenceValue = slotNames[i];
        }

        if (loadoutTitle != null)
            so.FindProperty("loadoutTitleLabel").objectReferenceValue = loadoutTitle;

        so.FindProperty("warningText").objectReferenceValue = warning.GetComponent<TextMeshProUGUI>();
        so.FindProperty("startButton").objectReferenceValue = start.GetComponent<Button>();
        so.ApplyModifiedPropertiesWithoutUndo();

        loadoutPanel.gameObject.SetActive(false);
    }

    static Transform FindBoxPanel(Transform panelRoot)
    {
        Transform boxPanel = panelRoot.Find("BoxPanel");
        if (boxPanel != null)
            return boxPanel;

        foreach (Transform child in panelRoot)
        {
            if (child.Find("Btn0") != null)
                return child;
        }

        return null;
    }

    static GameObject CreateWarningText(Transform parent)
    {
        var go = new GameObject("WarningText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 150f);
        rect.sizeDelta = new Vector2(900f, 48f);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 22f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text = "슬롯 2개를 눌러 순서를 바꿀 수 있습니다.";
        return go;
    }

    static GameObject CreateStartButton(Transform parent)
    {
        var go = new GameObject("StartButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 80f);
        rect.sizeDelta = new Vector2(200f, 60f);

        var image = go.GetComponent<Image>();
        image.color = new Color(0.2f, 0.55f, 0.25f, 1f);

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, false);
        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label = labelGo.GetComponent<TextMeshProUGUI>();
        label.text = "Start";
        label.fontSize = 26f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;

        return go;
    }

    static void WireGameManager(RuneSelectUI runeUi)
    {
        var gameManager = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        if (gameManager == null)
            return;

        SerializedObject so = new SerializedObject(gameManager);
        so.FindProperty("uiRuneSelect").objectReferenceValue = runeUi;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(gameManager);
    }

    static void EnsureFullScreenRoot(Transform root)
    {
        if (root is not RectTransform rect)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void WireChoiceButtons(RuneSelectUI runeUi, Transform choicePanel)
    {
        Transform boxPanel = FindBoxPanel(choicePanel);
        if (boxPanel == null)
            return;

        var buttons = new Button[3];
        var titles = new TextMeshProUGUI[3];
        var details = new TextMeshProUGUI[3];
        var icons = new Image[3];
        TextMeshProUGUI panelTitle = null;

        for (int i = 0; i < 3; i++)
        {
            Transform btn = boxPanel.Find($"Btn{i}");
            if (btn == null)
                continue;

            Button button = btn.GetComponent<Button>();
            ClearPersistentOnClick(button);
            buttons[i] = button;
            titles[i] = btn.Find("Title")?.GetComponent<TextMeshProUGUI>();
            details[i] = btn.Find("Detail")?.GetComponent<TextMeshProUGUI>();
            icons[i] = btn.Find("Icon")?.GetComponent<Image>();
        }

        foreach (Transform child in boxPanel)
        {
            if (child.name.StartsWith("Btn"))
                continue;

            panelTitle = child.GetComponent<TextMeshProUGUI>();
            if (panelTitle != null)
                break;
        }

        SerializedObject so = new SerializedObject(runeUi);
        so.FindProperty("choicePanelRoot").objectReferenceValue = choicePanel.gameObject;
        so.FindProperty("choiceButtons").arraySize = 3;
        so.FindProperty("choiceTitleLabels").arraySize = 3;
        so.FindProperty("choiceDetailLabels").arraySize = 3;
        so.FindProperty("choiceIcons").arraySize = 3;

        for (int i = 0; i < 3; i++)
        {
            so.FindProperty("choiceButtons").GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
            so.FindProperty("choiceTitleLabels").GetArrayElementAtIndex(i).objectReferenceValue = titles[i];
            so.FindProperty("choiceDetailLabels").GetArrayElementAtIndex(i).objectReferenceValue = details[i];
            so.FindProperty("choiceIcons").GetArrayElementAtIndex(i).objectReferenceValue = icons[i];
        }

        if (panelTitle != null)
            so.FindProperty("titleLabel").objectReferenceValue = panelTitle;

        so.ApplyModifiedPropertiesWithoutUndo();
        choicePanel.gameObject.SetActive(false);
    }

    static string BuildDiagnosisReport()
    {
        var sb = new StringBuilder();
        var all = Object.FindObjectsByType<RuneSelectUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"RuneSelectUI 개수: {all.Length} (1개 권장)");

        foreach (RuneSelectUI ui in all)
        {
            sb.AppendLine($"\n■ {ui.gameObject.name} (active={ui.gameObject.activeSelf})");
            SerializedObject so = new SerializedObject(ui);
            AppendRef(sb, "ChoicePanel", so.FindProperty("choicePanelRoot"));
            AppendRef(sb, "LoadoutPanel", so.FindProperty("loadoutPanelRoot"));
            AppendArray(sb, "choiceButtons", so.FindProperty("choiceButtons"));
            AppendArray(sb, "slotButtons", so.FindProperty("slotButtons"));
            AppendRef(sb, "startButton", so.FindProperty("startButton"));
            AppendRef(sb, "warningText", so.FindProperty("warningText"));
        }

        var gm = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        if (gm == null)
        {
            sb.AppendLine("\nGameManager: 없음");
        }
        else
        {
            SerializedObject gmSo = new SerializedObject(gm);
            AppendRef(sb, "\nGameManager.uiRuneSelect", gmSo.FindProperty("uiRuneSelect"));
        }

        int weaponEvents = 0;
        foreach (RuneSelectUI ui in all)
            weaponEvents += CountWeaponPickEventsUnder(ui.transform);

        if (weaponEvents > 0)
            sb.AppendLine($"\n⚠ ChoicePanel 버튼에 WeaponSelectUI.OnPickWeapon {weaponEvents}개 남음 → Tools/Rune/Fix Rune Button Wiring");

        sb.AppendLine("\n→ Tools/Rune/Setup Rune Select UI 실행 권장");
        return sb.ToString();
    }

    static int ClearWeaponPickEventsUnder(Transform root)
    {
        int cleared = 0;
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            if (ClearPersistentOnClick(button))
                cleared++;
        }

        return cleared;
    }

    static int CountWeaponPickEventsUnder(Transform root)
    {
        int count = 0;
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentMethodName(i) == "OnPickWeapon")
                    count++;
            }
        }

        return count;
    }

    static bool ClearPersistentOnClick(Button button)
    {
        if (button == null)
            return false;

        SerializedObject so = new SerializedObject(button);
        SerializedProperty calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
        if (calls == null || !calls.isArray || calls.arraySize == 0)
            return false;

        calls.ClearArray();
        so.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    static void AppendRef(StringBuilder sb, string label, SerializedProperty prop)
    {
        sb.AppendLine($"  {label}: {(prop.objectReferenceValue != null ? "OK" : "비어 있음")}");
    }

    static void AppendArray(StringBuilder sb, string label, SerializedProperty prop)
    {
        int filled = 0;
        for (int i = 0; i < prop.arraySize; i++)
        {
            if (prop.GetArrayElementAtIndex(i).objectReferenceValue != null)
                filled++;
        }

        sb.AppendLine($"  {label}: {filled}/{prop.arraySize}");
    }
}
#endif
