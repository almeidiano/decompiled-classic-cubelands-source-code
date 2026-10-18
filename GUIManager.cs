using System.Collections;
using UnityEngine;

public class GUIManager : MonoBehaviour
{
	public static GUIManager thisSP;

	public ArrayList openWindows;

	public void Awake()
	{
		thisSP = this;
		openWindows = new ArrayList();
	}

	public IEnumerator RegisterWindow(Rect newWindowRect)
	{
		openWindows.Add(newWindowRect);
		yield return new WaitForSeconds(0.1f);
		RemoveWindow(newWindowRect);
	}

	public void RemoveWindow(Rect newWindowRect)
	{
		for (int i = 0; i < openWindows.Count; i++)
		{
			if ((Rect)openWindows[i] == newWindowRect)
			{
				openWindows.Remove(openWindows[i]);
				break;
			}
		}
	}

	public bool ClickingOnWindow(Vector3 mousePos)
	{
		return ClickingOnWindow(mousePos.x, mousePos.y);
	}

	public bool ClickingOnWindow(float x, float y)
	{
		for (int i = 0; i < openWindows.Count; i++)
		{
			if (Utils.PositionWithinRect(x, y, (Rect)openWindows[i]))
			{
				return true;
			}
		}
		return false;
	}

	public static bool TypingInWindow()
	{
		return Chat.usingChat;
	}
}
