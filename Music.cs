using System.Collections;
using UnityEngine;

public class Music : MonoBehaviour
{
	public static Music SP;

	public AudioClip normalAudio;

	private void Awake()
	{
		SP = this;
	}

	private void Start()
	{
		StartCoroutine("WaitFinishLoading");
	}

	public void SetVolume(float vol)
	{
		base.audio.volume = vol;
	}

	private IEnumerator WaitFinishLoading()
	{
		if (!DedicatedServer.isDedicated)
		{
			float volBefore = base.audio.volume;
			base.audio.volume = 1f;
			if (!GameManager.SP.GetFinishedLoading())
			{
				yield return 0;
			}
			float endTime = Time.time + 0.5f;
			while (Time.time < endTime)
			{
				float num = endTime - Time.time;
				base.audio.volume = num * 2f;
				yield return 0;
			}
			base.audio.Stop();
			base.audio.volume = volBefore;
			base.audio.clip = normalAudio;
			base.audio.Play();
		}
	}
}
