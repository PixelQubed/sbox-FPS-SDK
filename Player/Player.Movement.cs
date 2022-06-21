using Sandbox;
using System;

namespace Amper.Source1;

partial class Source1Player
{
	public int m_StuckLast { get; set; }
	public float m_surfaceFriction { get; set; } = 1;
	public Surface m_pSurfaceData { get; set; }
	public Vector3 ViewOffset { get => EyeLocalPosition; set => EyeLocalPosition = value; }

	public Vector3 m_vecWaterJumpVel { get; set; }

	[Net, Predicted] public float m_flDucktime { get; set; }
	[Net, Predicted] public float m_flDuckJumpTime { get; set; }
	[Net, Predicted] public float m_flJumpTime { get; set; }
	[Net, Predicted] public float m_flSwimSoundTime { get; set; }
	[Net, Predicted] public float m_flWaterJumpTime { get; set; }
	[Net, Predicted] public float m_flStepSize { get; set; }

	[Net, Predicted] public PlayerFlags Flags { get; set; }
	
	public void AddFlags( PlayerFlags flag ) { Flags |= flag; }
	public void RemoveFlag( PlayerFlags flag ) { Flags &= ~flag; }



	[Net, Predicted] public float MaxSpeed { get; set; }
	[Net, Predicted] public WaterLevelType WaterLevelType { get; set; }

	[Net, Predicted] public float DuckAmount { get; set; }
	[Net, Predicted] public float DuckSpeed { get; set; }
}

[Flags]
public enum PlayerFlags
{
	Frozen = 0,
	OnTrain = 1,
	FL_WATERJUMP = 2
}
