using UnityEngine;
using UnityEngine.InputSystem;

public class KeyBindManager : MonoBehaviour
{
	public static KeyBindManager Instance { get; private set; }
	public PlayerControls Controls { get; private set; }

	private string _savePath;


	private void Awake()
	{
		Instance = this;
		Controls = new PlayerControls();
		_savePath = Application.persistentDataPath + "/setting.json";
	}

	public void SaveSetting()
	{
		InputSaveData data = new InputSaveData();
		data.bindingOverrides = Controls.SaveBindingOverridesAsJson();

		string json = JsonUtility.ToJson(data, true);
		System.IO.File.WriteAllText(_savePath, json);

		Debug.Log($"Save Successful: {_savePath}");
	}

	public void LoadSetting()
	{
		if (System.IO.File.Exists(_savePath))
		{
			string json = System.IO.File.ReadAllText(_savePath);
			InputSaveData data = JsonUtility.FromJson<InputSaveData>(json);

			if (string.IsNullOrEmpty(data.bindingOverrides)) return;
			
			Controls.LoadBindingOverridesFromJson(data.bindingOverrides);
		}
	}
}