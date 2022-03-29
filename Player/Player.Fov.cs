using Sandbox;
using System;

namespace Source1;

partial class Source1Player
{

	public float TargetFOV { get; set; }
	public float FOVSpeed { get; set; } = 1;

	public void SetFOV( float fov, float speed )
	{
		TargetFOV = fov;
		FOVSpeed = speed;
	}

}
