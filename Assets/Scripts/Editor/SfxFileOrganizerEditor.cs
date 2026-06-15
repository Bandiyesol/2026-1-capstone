#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>루트에 풀어둔 악세사리·UI SFX를 하위 폴더로 정리합니다.</summary>
public static class SfxFileOrganizerEditor
{
	const string SfxRoot = "Assets/Arts/Audio/SFX";
	const string AccessoryFolder = "Assets/Arts/Audio/SFX/Accessory";
	const string UiFolder = "Assets/Arts/Audio/SFX/UI";

	static readonly string[] UiFileNames =
	{
		"입력창에 입력 소리.mp3",
		"마법진 이동 소리.mp3",
	};

	static readonly string[] AccessoryFileNames =
	{
		"제우스의 심판.mp3",
		"심연의 군주.mp3",
		"번개 맞은 검.mp3",
		"에키드나의 목걸이.mp3",
		"마법의 구.wav",
		"그림자 가면.mp3",
		"투명 망토.mp3",
		"폭탄광.mp3",
		"불사조의 망토 폭발 소리.mp3",
		"불사조의 망토 버프 소리.wav",
		"미네르바의 지혜.mp3",
		"미다스의 장갑.mp3",
		"시간술사의 모래시계 가방.wav",
		"신의 방패.mp3",
		"무한의 마력.wav",
		"재앙의 씨앗 씨앗 심는 소리.mp3",
		"재앙의 씨앗 폭발 소리.wav",
		"영혼의 랜턴 공전 소리.mp3",
		"영혼의 랜턴 총알 소리.mp3",
		"용의 심장 심장 뛰는 소리.mp3",
		"용의 심장 울음소리.mp3",
		"차원 여행자의 게이트 .mp3",
		"금지된 마법서.mp3",
		"번개 깃든 악령.mp3",
	};

	[MenuItem("Tools/Game/Organize Accessory SFX Files")]
	public static void OrganizeFromMenu()
	{
		int moved = OrganizeInternal();
		AssetDatabase.Refresh();
		SfxCatalogBuilder.RebuildCatalog(applyBalance: true);
		AssetDatabase.SaveAssets();
		Debug.Log($"[SfxFileOrganizer] {moved}개 파일 정리 및 SfxCatalog 갱신 완료.");
	}

	public static void OrganizeFromCommandLine()
	{
		OrganizeInternal();
		AssetDatabase.Refresh();
		SfxCatalogBuilder.RebuildCatalog(applyBalance: true);
		AssetDatabase.SaveAssets();
		EditorApplication.Exit(0);
	}

	static int OrganizeInternal()
	{
		EnsureFolder(AccessoryFolder);
		EnsureFolder(UiFolder);

		int moved = 0;
		foreach (string fileName in UiFileNames)
			moved += TryMove(fileName, UiFolder);

		foreach (string fileName in AccessoryFileNames)
			moved += TryMove(fileName, AccessoryFolder);

		TryRenameGateFile();
		return moved;
	}

	static void TryRenameGateFile()
	{
		string oldPath = $"{AccessoryFolder}/차원 여행자의 게이트 .mp3";
		string newPath = $"{AccessoryFolder}/차원 여행자의 게이트.mp3";
		if (!File.Exists(oldPath) || File.Exists(newPath))
			return;

		string error = AssetDatabase.MoveAsset(oldPath, newPath);
		if (!string.IsNullOrEmpty(error))
			Debug.LogWarning($"[SfxFileOrganizer] 게이트 파일명 변경 실패: {error}");
	}

	static int TryMove(string fileName, string destFolder)
	{
		string srcPath = $"{SfxRoot}/{fileName}";
		if (!File.Exists(srcPath))
			return 0;

		string destPath = $"{destFolder}/{fileName}";
		string error = AssetDatabase.MoveAsset(srcPath, destPath);
		if (!string.IsNullOrEmpty(error))
		{
			Debug.LogWarning($"[SfxFileOrganizer] 이동 실패 ({fileName}): {error}");
			return 0;
		}

		return 1;
	}

	static void EnsureFolder(string assetPath)
	{
		if (AssetDatabase.IsValidFolder(assetPath))
			return;

		string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
		string folderName = Path.GetFileName(assetPath);
		if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
			EnsureFolder(parent);

		AssetDatabase.CreateFolder(parent, folderName);
	}
}
#endif
