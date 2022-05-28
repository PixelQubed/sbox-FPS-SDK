using Sandbox;

namespace Amper.Source1;

partial class Source1Player
{
	protected Vector3 WishViewPunchAngle { get; set; }
	public Vector3 ViewPunchAngle { get; private set; }

	protected Vector3 WishViewPunchOffset { get; set; }
	public Vector3 ViewPunchOffset { get; private set; }

	public void PunchViewAngles( Vector3 dir ) => PunchViewAngles( dir.x, dir.y, dir.z );
	public void PunchViewOffset( Vector3 dir ) => PunchViewOffset( dir.x, dir.y, dir.z );

	[ClientRpc]
	public virtual void PunchViewAngles( float x, float y, float z )
	{
		WishViewPunchAngle += new Vector3( x, y, z );
	}


	[ClientRpc]
	public virtual void PunchViewOffset( float x, float y, float z )
	{
		WishViewPunchOffset += new Vector3( x, y, z );
	}

	[ClientRpc]
	public void PunchViewOffsetClient( float x, float y, float z ) 
	{
		PunchViewOffset( x, y, z ); 
	}

	public virtual void SimulateVisuals()
	{
		if ( IsClient ) 
			DecayViewPunch();
	}

	public void DecayViewPunch()
	{
		//
		// View Punch Angle
		//

		WishViewPunchAngle = WishViewPunchAngle.LerpTo( 0, Time.Delta * 5f );
		ViewPunchAngle = ViewPunchAngle.LerpTo( WishViewPunchAngle, Time.Delta * 10f );

		//
		// View Punch Offset
		//

		WishViewPunchOffset = WishViewPunchOffset.LerpTo( 0, Time.Delta * 5f );
		ViewPunchOffset = ViewPunchOffset.LerpTo( WishViewPunchOffset, Time.Delta * 10f );
	}
}
