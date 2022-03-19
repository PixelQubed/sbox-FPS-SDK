using Sandbox;
using System;

namespace Source1;

partial class Source1GameMovement
{
	public virtual Vector3 GetPlayerMins( bool ducked ) { return Player.GetPlayerMinsScaled( ducked ); }
	public virtual Vector3 GetPlayerMaxs( bool ducked ) { return Player.GetPlayerMaxsScaled( ducked ); }
	public virtual Vector3 GetPlayerViewOffset( bool ducked ) { return Player.GetPlayerViewOffsetScaled( ducked ); }
	public virtual Vector3 GetPlayerExtents( bool ducked ) { return Player.GetPlayerExtentsScaled( ducked ); }

	public virtual Vector3 GetPlayerMins() { return GetPlayerMins( IsDucked ); }
	public virtual Vector3 GetPlayerMaxs() { return GetPlayerMaxs( IsDucked ); }
	public virtual Vector3 GetPlayerViewOffset() { return GetPlayerViewOffset( IsDucked ); }
	public virtual Vector3 GetPlayerExtents() { return GetPlayerExtents( IsDucked ); }

	public virtual void SetDuckedEyeOffset( float duckFraction )
	{
		Vector3 vDuckHullMin = GetPlayerMins( true );
		Vector3 vStandHullMin = GetPlayerMins( false );

		float fMore = vDuckHullMin.z - vStandHullMin.z;

		Vector3 vecDuckViewOffset = GetPlayerViewOffset( true );
		Vector3 vecStandViewOffset = GetPlayerViewOffset( false );
		Vector3 temp = EyeLocalPosition;

		temp.z = (vecDuckViewOffset.z - fMore) * duckFraction + vecStandViewOffset.z * (1 - duckFraction);

		EyeLocalPosition = temp;
	}
}
