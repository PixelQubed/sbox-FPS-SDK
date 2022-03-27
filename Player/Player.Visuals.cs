using Sandbox;
using System;
using System.Linq;

namespace Source1;

partial class Source1Player
{
	protected Rotation WishViewPunchAngle { get; set; }
	public Rotation ViewPunchAngle { get; private set; }

	protected Vector3 WishViewPunchOffset { get; set; }
	public Vector3 ViewPunchOffset { get; private set; }

	[Obsolete("Use PunchViewAngles now")]
	public void PunchView( Vector3 dir )
	{
		PunchViewAngles( dir );
	}

	public void PunchViewAngles( Vector3 dir )
	{
		PunchViewAngles( dir.x, dir.y, dir.z );
	}

	public void PunchViewOffset( Vector3 dir )
	{
		PunchViewOffset( dir.x, dir.y, dir.z );
	}

	[ClientRpc]
	public virtual void PunchViewAngles( float pitch, float yaw, float roll )
	{
		WishViewPunchAngle *= Rotation.From( pitch, yaw, roll );
	}

	[ClientRpc]
	public virtual void PunchViewOffset( float x, float y, float z )
	{
		WishViewPunchOffset += new Vector3( x, y, z );
	}

	public virtual void SimulateVisuals()
	{
		if ( !IsClient )
			return;

		DecayViewPunchAngles();
	}

	public void DecayViewPunchAngles()
	{
		//
		// View Punch Angle
		//

		WishViewPunchAngle = Rotation.Lerp( WishViewPunchAngle, Rotation.Identity, Time.Delta * 5f );
		ViewPunchAngle = Rotation.Lerp( ViewPunchAngle, WishViewPunchAngle, Time.Delta * 10f );

		//
		// View Punch Offset
		//

		WishViewPunchOffset = Vector3.Lerp( WishViewPunchOffset, 0, Time.Delta * 5f );
		ViewPunchOffset = Vector3.Lerp( ViewPunchOffset, WishViewPunchOffset, Time.Delta * 10f );
	}
}
