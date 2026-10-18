using UnityEngine;

public struct VertexPositionTexture
{
	public Vector3 position;

	public float u;

	public float v;

	public VertexPositionTexture(float x, float y, float z, float u, float v)
	{
		position = new Vector3(x, y, z);
		this.u = u;
		this.v = v;
	}
}
