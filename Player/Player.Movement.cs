using Sandbox;
using System;

namespace Amper.Source1;

partial class Source1Player
{
	public int m_StuckLast { get; set; }
	public float[] m_StuckCheckTime { get; set; } = new float[2];

	public float m_surfaceFriction { get; set; } = 1;
	public Surface m_pSurfaceData { get; set; }
	public Vector3 ViewOffset { get => EyeLocalPosition; set => EyeLocalPosition = value; }


	//
	// Water
	//
	[Net, Predicted] public float m_flSwimSoundTime { get; set; }
	[Net, Predicted] public WaterLevelType WaterLevelType { get; set; }
	[Net, Predicted] public Vector3 m_vecWaterJumpVel { get; set; }
	[Net, Predicted] public float m_flWaterJumpTime { get; set; }
	public bool IsJumpingFromWater => m_flWaterJumpTime != 0;
	[Net, Predicted] public float m_flWaterEntryTime { get; set; }

	[Net, Predicted] public float m_nJumpTimeMsecs { get; set; }
	public float m_flStepSize => 18;
	[Net, Predicted] public float m_flFallVelocity { get; set; }

	//
	// Ducking
	//
	[Net, Predicted] public bool m_bDucking { get; set; }
	[Net, Predicted] public bool m_bDucked { get; set; }
	[Net, Predicted] public float m_flDuckTime { get; set; }

	[Net, Predicted] public PlayerFlags Flags { get; set; }
	
	public void AddFlags( PlayerFlags flag ) { Flags |= flag; }
	public void RemoveFlag( PlayerFlags flag ) { Flags &= ~flag; }



	[Net, Predicted] public float MaxSpeed { get; set; }

	[Net, Predicted] public float DuckAmount { get; set; }
	[Net, Predicted] public float DuckSpeed { get; set; }
}

[Flags]
public enum PlayerFlags
{
	FL_FROZEN = 1 << 0,
	FL_ONTRAIN = 1 << 1,
	FL_WATERJUMP = 1 << 2,
	FL_DUCKING = 1 << 3
}
