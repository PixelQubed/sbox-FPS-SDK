using Sandbox;
using System;

namespace Amper.Source1;

partial class Source1Player
{
	[Net, Predicted] public float MaxSpeed { get; set; }

	public Vector3 ViewOffset { get => EyeLocalPosition; set => EyeLocalPosition = value; }
	public float SurfaceFriction { get; set; } = 1;
	public Surface SurfaceData { get; set; }

	//
	// Ducking
	//

	[Net, Predicted] public float DuckTime { get; set; }
	[Net, Predicted] public float DuckSpeed { get; set; }
	[Net, Predicted] public bool IsDucked { get; set; }
	[Net, Predicted] public int AirDuckCount { get; set; }
	[Net, Predicted] public float LastDuckTime { get; set; }
	public bool IsDucking => DuckTime > 0;
	public float DuckProgress => Math.Clamp( DuckTime / TimeToDuck, 0, 1 );
	public virtual float TimeToDuck => .2f;

	//
	// Water
	//

	[Net, Predicted] public float NextSwimSoundTime { get; set; }
	[Net, Predicted] public WaterLevelType WaterLevelType { get; set; }
	[Net, Predicted] public Vector3 WaterJumpVelocity { get; set; }
	[Net, Predicted] public float WaterJumpTime { get; set; }
	[Net, Predicted] public float WaterEntryTime { get; set; }
	public bool IsJumpingFromWater => WaterJumpTime != 0;

	//
	// Stuck
	//

	public int LastStuckOffsetIndex { get; set; }
	public float[] LastStuckCheckTime { get; set; } = new float[2];


	[Net, Predicted] public float m_nJumpTimeMsecs { get; set; }
	public float m_flStepSize => 18;

	[Net, Predicted] public PlayerFlags Flags { get; set; }
	
	public void AddFlags( PlayerFlags flag ) { Flags |= flag; }
	public void RemoveFlag( PlayerFlags flag ) { Flags &= ~flag; }

}

[Flags]
public enum PlayerFlags
{
	FL_FROZEN = 1 << 0,
	FL_ONTRAIN = 1 << 1,
	FL_WATERJUMP = 1 << 2
}
