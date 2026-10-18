using System;
using UnityEngine;

public class CamViewBobber : MonoBehaviour
{
	private float timer;

	public float bobbingSpeed = 0.18f;

	public float bobbingAmount = 0.2f;

	public float midpoint;

	public static float rotateForce;

	private Transform trans;

	public static void AddForce(bool neg)
	{
		if (neg)
		{
			rotateForce -= 2f;
		}
		else
		{
			rotateForce += 2f;
		}
		rotateForce = Mathf.Clamp(rotateForce, -7f, 7f);
	}

	private void Awake()
	{
		trans = base.transform.parent.FindChild("1pCamera/SpawnBlock");
	}

	private void Update()
	{
		if (rotateForce != 0f)
		{
			float num = Mathf.Abs(rotateForce);
			float num2 = rotateForce * Time.deltaTime;
			rotateForce -= num2;
			if (num < Mathf.Abs(rotateForce) || Mathf.Abs(rotateForce) < 0.5f)
			{
				rotateForce = 0f;
			}
			trans.RotateAround(Vector3.up, num2);
		}
		if (!Screen.lockCursor)
		{
			return;
		}
		float f = 0f;
		float f2 = 0f;
		if (GameManager.AllowInput())
		{
			f = Input.GetAxis("Horizontal");
			f2 = Input.GetAxis("Vertical");
		}
		float num3 = 0f;
		if (Mathf.Abs(f) == 0f && Mathf.Abs(f2) == 0f)
		{
			timer = 0f;
		}
		else
		{
			num3 = Mathf.Sin(timer);
			timer += bobbingSpeed * Time.deltaTime;
			if (timer > (float)Math.PI * 2f)
			{
				timer -= (float)Math.PI * 2f;
			}
		}
		if (num3 != 0f)
		{
			float num4 = num3 * bobbingAmount;
			num4 = Mathf.Clamp(Mathf.Abs(f) + Mathf.Abs(f2), 0f, 1f) * num4;
			base.transform.localPosition = new Vector3(base.transform.localPosition.x, midpoint + num4, base.transform.localPosition.z);
		}
		else
		{
			base.transform.localPosition = new Vector3(base.transform.localPosition.x, midpoint, base.transform.localPosition.z);
		}
	}
}
