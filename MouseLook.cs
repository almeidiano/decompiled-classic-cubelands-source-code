using UnityEngine;

[AddComponentMenu("Camera-Control/Mouse Look")]
public class MouseLook : MonoBehaviour
{
	public enum RotationAxes
	{
		MouseXAndY,
		MouseX,
		MouseY
	}

	public RotationAxes axes;

	public static float sensitivityX = 15f;

	public static float sensitivityY = 15f;

	public float minimumX = -360f;

	public float maximumX = 360f;

	public float minimumY = -60f;

	public float maximumY = 60f;

	private float rotationX;

	private float rotationY;

	private Quaternion originalRotation;

	private bool isLocalPlayer;

	private float lastFloatShareTime;

	private float lastFloatShareValue;

	private NetworkView netView;

	private Transform headTransform;

	private float headEulerStart;

	private float correctXValue;

	private float correctYValue;

	private void Awake()
	{
		sensitivityX = (sensitivityY = Options.mouseSensitivity);
		if ((bool)base.rigidbody)
		{
			base.rigidbody.freezeRotation = true;
		}
		originalRotation = base.transform.localRotation;
		netView = base.transform.root.networkView;
		if (axes == RotationAxes.MouseY)
		{
			headTransform = base.transform.parent.Find("Graphics/Character/Head");
			if (headTransform != null)
			{
				headEulerStart = headTransform.localEulerAngles.x;
			}
		}
	}

	private void Update()
	{
		float num = Mathf.Min(Time.deltaTime, 0.05f);
		if (axes == RotationAxes.MouseXAndY)
		{
			return;
		}
		if (axes == RotationAxes.MouseX)
		{
			if (isLocalPlayer)
			{
				if (Screen.lockCursor)
				{
					rotationX += Mathf.Clamp(Input.GetAxis("Mouse X") * sensitivityX * num, (0f - num) * 1440f, num * 1440f);
				}
				if (lastFloatShareTime < Time.time - 0.05f && Mathf.Abs(lastFloatShareValue - rotationX) >= 2f)
				{
					lastFloatShareValue = rotationX;
					lastFloatShareTime = Time.time;
					netView.RPC("SetX", RPCMode.Others, lastFloatShareValue);
				}
			}
			else
			{
				rotationX = Mathf.Lerp(rotationX, correctXValue, 0.5f);
			}
			if (isLocalPlayer || rotationX != correctXValue)
			{
				rotationX = ClampAngle(rotationX, minimumX, maximumX);
				Quaternion quaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
				base.transform.localRotation = originalRotation * quaternion;
			}
			return;
		}
		if (isLocalPlayer)
		{
			if (Screen.lockCursor)
			{
				if (Options.invertMouseY)
				{
					rotationY -= Mathf.Clamp(Input.GetAxis("Mouse Y") * sensitivityY * num, (0f - num) * 1440f, num * 1440f);
				}
				else
				{
					rotationY += Mathf.Clamp(Input.GetAxis("Mouse Y") * sensitivityY * num, (0f - num) * 1440f, num * 1440f);
				}
			}
			if (lastFloatShareTime < Time.time - 0.05f && Mathf.Abs(lastFloatShareValue - rotationY) >= 2f)
			{
				lastFloatShareValue = rotationY;
				lastFloatShareTime = Time.time;
				netView.RPC("SetYView", RPCMode.Others, lastFloatShareValue);
			}
		}
		else
		{
			rotationY = Mathf.Lerp(rotationY, correctYValue, Time.deltaTime * 10f);
		}
		if (isLocalPlayer || rotationY != correctYValue)
		{
			rotationY = ClampAngle(rotationY, minimumY, maximumY);
			Quaternion localRotation = originalRotation * Quaternion.AngleAxis(rotationY, Vector3.left);
			base.transform.localRotation = localRotation;
			if (!isLocalPlayer && (bool)headTransform)
			{
				float num2 = Mathf.Clamp(rotationY, -25f, 30f);
				Vector3 localEulerAngles = new Vector3(headEulerStart - num2, 0f, 0f);
				headTransform.localEulerAngles = localEulerAngles;
			}
		}
	}

	public void SetLocal()
	{
		isLocalPlayer = true;
	}

	public float GetRotX()
	{
		return rotationX;
	}

	public float GetRotY()
	{
		return rotationY;
	}

	public static float ClampAngle(float angle, float min, float max)
	{
		if (angle < -360f)
		{
			angle += 360f;
		}
		if (angle > 360f)
		{
			angle -= 360f;
		}
		return Mathf.Clamp(angle, min, max);
	}

	[RPC]
	private void SetX(float rotX)
	{
		correctXValue = rotX;
	}

	public void SetRotX(float rotX)
	{
		rotationX = rotX;
	}

	public void SetY(float rotY)
	{
		correctYValue = rotY;
	}
}
