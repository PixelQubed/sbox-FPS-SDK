using Sandbox;

namespace Amper.Source1;

partial class GameMovement
{
	public virtual void FullWalkMove()
	{
		if ( !CheckWater() )
		{
			StartGravity();
		}

		// If we are leaping out of the water, just update the counters.
		if ( Player.WaterJumpTime != 0 )
		{
			// Try to jump out of the water (and check to see if we still are).
			WaterJump();
			TryPlayerMove();

			CheckWater();
			return;
		}

		// If we are swimming in the water, see if we are nudging against a place we can jump up out
		// of, and, if so, start out jump.  Otherwise, if we are not moving up, then reset jump timer to 0.
		// Also run the swim code if we're a ghost or have the TF_COND_SWIMMING_NO_EFFECTS condition
		if ( InWater() )
		{
			FullWalkMoveUnderwater();
			return;
		}

		if ( WishJump() )
		{
			CheckJumpButton();
		}

		CheckVelocity();

		if ( Player.GroundEntity.IsValid() ) 
		{
			Move.Velocity.z = 0;
			Friction();
			WalkMove();
		}
		else
		{
			AirMove();
		}

		// Set final flags.
		CategorizePosition();

		// Make sure velocity is valid.
		CheckVelocity();

		// Add any remaining gravitational component.
		if ( !CheckWater() )
		{
			FinishGravity();
		}
	}

	public virtual void WalkMove()
	{
		Move.ViewAngles.AngleVectors( out var forward, out var right, out var up );
		var oldGround = Player.GroundEntity;

		var fmove = Move.ForwardMove;
		var smove = Move.SideMove;

		if ( forward[2] != 0 )
		{
			forward[2] = 0;
			forward = forward.Normal;
		}

		if ( right[2] != 0 )
		{
			right[2] = 0;
			right = right.Normal;
		}

		Vector3 wishvel = 0;
		for ( int i = 0; i < 2; i++ )
			wishvel[i] = forward[i] * fmove + right[i] * smove;

		wishvel[2] = 0;

		var wishspeed = wishvel.Length;
		var wishdir = wishvel.Normal;

		//
		// Clamp to server defined max speed
		//
		if ( wishspeed != 0 && wishspeed > Move.MaxSpeed )
		{
			wishvel *= Move.MaxSpeed / wishspeed;
			wishspeed = Move.MaxSpeed;
		}

		var acceleration = sv_accelerate;

		// if our wish speed is too low, we must increase acceleration or we'll never overcome friction
		// Reverse the basic friction calculation to find our required acceleration
		var wishspeedThreshold = 100 * sv_friction / sv_accelerate;
		if ( wishspeed > 0 && wishspeed < wishspeedThreshold )
		{
			float speed = Move.Velocity.Length;
			float flControl = (speed < sv_stopspeed) ? sv_stopspeed : speed;
			acceleration = (flControl * sv_friction) / wishspeed + 1;
		}

		// Set pmove velocity
		Move.Velocity[2] = 0;
		Accelerate( wishdir, wishspeed, acceleration );
		Move.Velocity[2] = 0;

		// Clamp the players speed in x,y.
		float newSpeed = Move.Velocity.Length;
		if ( newSpeed > Move.MaxSpeed )
		{
			float flScale = Move.MaxSpeed / newSpeed;
			Move.Velocity[0] *= flScale;
			Move.Velocity[1] *= flScale;
		}

		Move.Velocity += Player.BaseVelocity;
		var spd = Move.Velocity.Length;

		if ( spd < 1 )
		{
			Move.Velocity = 0;
			Move.Velocity -= Player.BaseVelocity;
			return;
		}

		// first try just moving to the destination	
		var dest = Vector3.Zero;
		dest[0] = Move.Position[0] + Move.Velocity[0] * Time.Delta;
		dest[1] = Move.Position[1] + Move.Velocity[1] * Time.Delta;
		dest[2] = Move.Position[2];

		var trace = TraceBBox( Move.Position, dest );
		// didn't hit anything.
		if ( trace.Fraction == 1 )
		{
			Move.Position = trace.EndPosition;
			Move.Velocity -= Player.BaseVelocity;

			StayOnGround();
			return;
		}

		if ( oldGround == null && Player.WaterLevel == 0 )
		{
			Move.Velocity -= Player.BaseVelocity;
			return;
		}

		// If we are jumping out of water, don't do anything more.
		if ( Player.WaterJumpTime != 0 )
		{
			Move.Velocity -= Player.BaseVelocity;
			return;
		}

		StepMove();
		Move.Velocity -= Player.BaseVelocity;

		StayOnGround();
	}

	/// <summary>
	/// Remove ground friction from velocity
	/// </summary>
	public virtual void Friction()
	{
		// If we are in water jump cycle, don't apply friction
		if ( Player.IsJumpingFromWater )
			return;

		// Calculate speed
		var speed = Move.Velocity.Length;
		if ( speed < 0.1f )
			return;

		var drop = 0f;

		if ( Player.GroundEntity != null )
		{
			var friction = sv_friction * Player.SurfaceFriction;
			var control = (speed < sv_stopspeed) ? sv_stopspeed : speed;

			// Add the amount to the drop amount.
			drop += control * friction * Time.Delta;
		}

		// scale the velocity
		float newspeed = speed - drop;
		if ( newspeed < 0 )
			newspeed = 0;

		if ( newspeed != speed )
		{
			newspeed /= speed;
			Move.Velocity *= newspeed;
		}
	}

	public virtual void AirMove()
	{
		Move.ViewAngles.AngleVectors( out var forward, out var right, out var up );

		var fmove = Move.ForwardMove;
		var smove = Move.SideMove;

		forward[2] = 0;
		right[2] = 0;
		forward = forward.Normal;
		right = right.Normal;

		Vector3 wishvel = 0;
		for ( var i = 0; i < 2; i++ )
			wishvel[i] = forward[i] * fmove + right[i] * smove;
		wishvel[2] = 0;

		var wishdir = wishvel.Normal;
		var wishspeed = wishvel.Length;

		if ( wishspeed != 0 && wishspeed > Move.MaxSpeed )
		{
			wishvel *= Move.MaxSpeed / wishspeed;
			wishspeed = Move.MaxSpeed;
		}

		AirAccelerate( wishdir, wishspeed, sv_airaccelerate );

		Move.Velocity += Player.BaseVelocity;
		TryPlayerMove();
		Move.Velocity -= Player.BaseVelocity;
	}
}
