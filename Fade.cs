using System;
using System.Collections;
using UnityEngine;

public class Fade : MonoBehaviour
{
	public static Fade use;

	private void Awake()
	{
		if ((bool)use)
		{
			Debug.Log("Only one instance of this script in a scene is allowed");
		}
		else
		{
			use = this;
		}
	}

	public IEnumerator Alpha(object obj, float start, float end, float timer)
	{
		yield return StartCoroutine(Alpha(obj, start, end, timer, EaseType.None));
	}

	public IEnumerator Alpha(object obj, float start, float end, float timer, EaseType easeType)
	{
		if (!CheckType(obj))
		{
			yield break;
		}
		float t = 0f;
		while (t < 1f)
		{
			t += Time.deltaTime * (1f / timer);
			if (obj is GUITexture)
			{
				GUITexture gUITexture = obj as GUITexture;
				gUITexture.color = new Color(gUITexture.color.r, gUITexture.color.b, gUITexture.color.g, Mathf.Lerp(start, end, Ease(t, easeType)) * 0.5f);
			}
			else
			{
				Material material = obj as Material;
				material.color = new Color(material.color.r, material.color.b, material.color.g, Mathf.Lerp(start, end, Ease(t, easeType)));
			}
			yield return 0;
		}
	}

	public IEnumerator Colors(object obj, Color start, Color end, float timer)
	{
		yield return StartCoroutine(Colors(obj, start, end, timer, EaseType.None));
	}

	public IEnumerator Colors(object obj, Color start, Color end, float timer, EaseType easeType)
	{
		if (!CheckType(obj))
		{
			yield break;
		}
		float t = 0f;
		while (t < 1f)
		{
			t += Time.deltaTime * (1f / timer);
			if (obj is GUITexture)
			{
				(obj as GUITexture).color = Color.Lerp(start, end, Ease(t, easeType)) * 0.5f;
			}
			else
			{
				(obj as Material).color = Color.Lerp(start, end, Ease(t, easeType));
			}
			yield return 0;
		}
	}

	public IEnumerator Colors(object obj, Color[] colorRange, float timer, bool repeat)
	{
		if (!CheckType(obj))
		{
			yield break;
		}
		if (colorRange.Length < 2)
		{
			Debug.Log("Error: color array must have at least 2 entries");
			yield break;
		}
		timer /= (float)colorRange.Length;
		int i = 0;
		do
		{
			float t = 0f;
			while (t < 1f)
			{
				t += Time.deltaTime * (1f / timer);
				if (obj is GUITexture)
				{
					(obj as GUITexture).color = Color.Lerp(colorRange[i], colorRange[(i + 1) % colorRange.Length], t) * 0.5f;
				}
				else
				{
					(obj as Material).color = Color.Lerp(colorRange[i], colorRange[(i + 1) % colorRange.Length], t);
				}
				yield return 0;
			}
			int num = i + 1;
			i = num % colorRange.Length;
		}
		while (repeat || i != 0);
	}

	private float Ease(float t, EaseType easeType)
	{
		return easeType switch
		{
			EaseType.None => t, 
			EaseType.In => Mathf.Lerp(0f, 1f, 1f - Mathf.Cos(t * (float)Math.PI * 0.5f)), 
			EaseType.Out => Mathf.Lerp(0f, 1f, Mathf.Sin(t * (float)Math.PI * 0.5f)), 
			_ => Mathf.SmoothStep(0f, 1f, t), 
		};
	}

	private bool CheckType(object obj)
	{
		if (obj is GUITexture || obj is Material)
		{
			return true;
		}
		Debug.Log(string.Concat("Error: object is a ", obj, ". It must be a GUITexture or a Material"));
		return false;
	}
}
