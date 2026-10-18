using UnityEngine;

public class IngameMenuUpjers : MonoBehaviour
{
	public GUISkin CubeLandsSkin;

	private void OnGUI()
	{
		GUILayout.BeginHorizontal("box");
		GUI.skin = CubeLandsSkin;
		GUILayout.Button("CubeLands Button\nund det is\nsogar dynamisch", GUILayout.Height(100f));
		GUILayout.Button("Standard Button", GUILayout.Height(100f));
		GUILayout.EndHorizontal();
		GUI.skin = null;
		GUI.Box(new Rect(20f, Screen.height - 140, 250f, 120f), string.Empty);
		GUILayout.BeginArea(new Rect(20f, Screen.height - 140, 250f, 120f));
		GUILayout.BeginHorizontal();
		GUI.skin = CubeLandsSkin;
		GUILayout.Button("CubeLands Button\nund det is\nsogar dynamisch", GUILayout.Height(100f));
		GUILayout.Button("Standard Button", GUILayout.Height(100f));
		GUILayout.EndHorizontal();
		GUILayout.EndArea();
	}
}
