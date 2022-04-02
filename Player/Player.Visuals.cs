using Sandbox;
using System;
using System.Linq;

namespace Source1;

partial class Source1Player
{
	protected Vector3 WishViewPunchAngle { get; set; }
	public Vector3 ViewPunchAngle { get; private set; }

	protected Vector3 WishViewPunchOffset { get; set; }
	public Vector3 ViewPunchOffset { get; private set; }

	[Obsolete( "Use PunchViewAngles now" )]
	public void PunchView( Vector3 dir ) => PunchViewAngles( dir );

	public void PunchViewAngles( Vector3 dir ) => PunchViewAngles( dir.x, dir.y, dir.z );
	public void PunchViewOffset( Vector3 dir ) => PunchViewOffset( dir.x, dir.y, dir.z );

	public virtual void PunchViewAngles( float x, float y, float z )
	{
		WishViewPunchAngle += new Vector3( x, y, z );
	}

	public virtual void PunchViewOffset( float x, float y, float z )
	{
		WishViewPunchOffset += new Vector3( x, y, z );
	}

	public virtual void SimulateVisuals()
	{
		DecayViewPunchAngles();
	}

	public void DecayViewPunchAngles()
	{
		//
		// View Punch Angle
		//

		WishViewPunchAngle = Vector3.Lerp( WishViewPunchAngle, 0, Time.Delta * 5f );
		ViewPunchAngle = Vector3.Lerp( ViewPunchAngle, WishViewPunchAngle, Time.Delta * 10f );

		//
		// View Punch Offset
		//

		WishViewPunchOffset = Vector3.Lerp( WishViewPunchOffset, 0, Time.Delta * 5f );
		ViewPunchOffset = Vector3.Lerp( ViewPunchOffset, WishViewPunchOffset, Time.Delta * 10f );
	}
}
