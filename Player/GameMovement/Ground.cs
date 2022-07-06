using Sandbox;

namespace Amper.Source1;

partial class GameMovement
{
	public virtual void SetGroundEntity( TraceResult? pm )
	{
		var newGround = pm.HasValue ? pm.Value.Entity : null;

		var oldGround = Player.GroundEntity;
		var vecBaseVelocity = Player.BaseVelocity;

		if ( !oldGround.IsValid() && newGround.IsValid() )
		{
			// Subtract ground velocity at instant we hit ground jumping
			vecBaseVelocity -= newGround.Velocity;
			vecBaseVelocity.z = newGround.Velocity.z;
		}
		else if ( oldGround.IsValid() && !newGround.IsValid() )
		{
			// Add in ground velocity at instant we started jumping
			vecBaseVelocity += oldGround.Velocity;
			vecBaseVelocity.z = oldGround.Velocity.z;
		}

		Player.BaseVelocity = vecBaseVelocity;
		Player.GroundEntity = newGround;

		// If we are on something...

		if ( newGround.IsValid() ) 
		{
			CategorizeGroundSurface( pm.Value );

			// Then we are not in water jump sequence
			Player.m_flWaterJumpTime = 0;
			Move.Velocity.z = 0;
		}
	}

	public virtual void CategorizeGroundSurface( TraceResult pm )
	{
		Player.m_pSurfaceData = pm.Surface;
		Player.m_surfaceFriction = pm.Surface.Friction;

		// HACKHACK: Scale this to fudge the relationship between vphysics friction values and player friction values.
		// A value of 0.8f feels pretty normal for vphysics, whereas 1.0f is normal for players.
		// This scaling trivially makes them equivalent.  REVISIT if this affects low friction surfaces too much.
		Player.m_surfaceFriction *= 1.25f;
		if ( Player.m_surfaceFriction > 1.0f )
			Player.m_surfaceFriction = 1.0f;
	}
}
