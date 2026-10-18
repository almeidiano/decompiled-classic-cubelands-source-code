using System.Collections;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
	private IEnumerator Start()
	{
		yield return 0;
		Options.LoadOptions();
	}

	private void Update()
	{
	}

	private void OnPreRender()
	{
		WorldData.SP.RunGraphicChanges();
	}
}
