using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Amper.Source1;

partial class GameMovement
{
	public virtual void FullWalkMoveUnderwater()
	{
		if ( Player.WaterLevelType == WaterLevelType.Waist )
			CheckWaterJump();

		// If we are falling again, then we must not trying to jump out of water any more.
		if ( Move.Velocity.z < 0 && Player.m_flWaterJumpTime != 0 )
			Player.m_flWaterJumpTime = 0.0f;

		// Was jump button pressed?
		if ( Input.Down( InputButton.Jump ) )
			CheckJumpButton();

		// Perform regular water movement
		WaterMove();

		// Redetermine position vars
		CategorizePosition();

		// If we are on ground, no downward velocity.
		if ( Player.GroundEntity.IsValid() ) 
		{
			Move.Velocity.z = 0;
		}
	}

	protected void WaterMove()
	{
		Move.ViewAngles.AngleVectors( out var forward, out var right, out var up );

		var wishvel = forward * Move.ForwardMove + right * Move.SideMove;

		// if we have the jump key down, move us up as well
		if ( Input.Down( InputButton.Jump ) )
		{
			wishvel.z += Move.MaxSpeed;
		}

		// Sinking after no other movement occurs
		else if ( Move.ForwardMove == 0 && Move.SideMove == 0 && Move.UpMove == 0 )
		{
			wishvel.z -= 60;
		}
		else  // Go straight up by upmove amount.
		{
			// exaggerate upward movement along forward as well
			float upwardMovememnt = Move.ForwardMove * forward.z * 2;
			upwardMovememnt = Math.Clamp( upwardMovememnt, 0, Move.MaxSpeed );
			wishvel.z += Move.UpMove + upwardMovememnt;
		}

		var wishdir = wishvel.Normal;
		var wishspeed = wishvel.Length;

		// Cap speed.
		if ( wishspeed > Move.MaxSpeed )
		{
			wishvel *= Move.MaxSpeed / wishspeed;
			wishspeed = Move.MaxSpeed;
		}

		// Slow us down a bit.
		wishspeed *= 0.8f;

		// Water friction
		var temp = Move.Velocity;
		var speed = temp.Length;

		var newspeed = 0f;
		if ( speed != 0 )
		{
			newspeed = speed - Time.Delta * speed * sv_friction * Player.m_surfaceFriction;
			if ( newspeed < 0.1f )
			{
				newspeed = 0;
			}

			Move.Velocity *= newspeed / speed;
		}
		else
		{
			newspeed = 0;
		}

		// water acceleration
		if ( wishspeed >= .1f )
		{
			var addspeed = wishspeed - newspeed;
			if ( addspeed > 0 )
			{
				wishvel = wishvel.Normal;

				var accelspeed = sv_accelerate * wishspeed * Time.Delta * Player.m_surfaceFriction;
				if ( accelspeed > addspeed ) accelspeed = addspeed;

				Move.Velocity += accelspeed * wishvel;
			}
		}

		Move.Velocity += Player.BaseVelocity;

		// Now move
		// assume it is a stair or a slope, so press down from stepheight above
		var dest = Move.Position + Move.Velocity * Time.Delta;

		var pm = TraceBBox( Move.Position, dest );
		if ( pm.Fraction == 1 )
		{
			var start = dest.WithZ( dest.z + sv_stepsize + 1 );

			pm = TraceBBox( start, dest );
			if ( !pm.StartedSolid )
			{
				// walked up the step, so just keep result and exit
				Move.Position = pm.EndPosition;
				Move.Velocity -= Player.BaseVelocity;
				return;
			}

			// Try moving straight along out normal path.
			TryPlayerMove();
		}
		else
		{
			if ( !Player.GroundEntity.IsValid() ) 
			{
				TryPlayerMove();
				Move.Velocity -= Player.BaseVelocity;
				return;
			}

			StepMove();
		}

		Move.Velocity -= Player.BaseVelocity;
	}

	public virtual void WaterJump( )
	{
		if ( Player.m_flWaterJumpTime > 10000 )
			Player.m_flWaterJumpTime = 10000;

		if ( !Player.IsJumpingFromWater )
			return;

		Player.m_flWaterJumpTime -= 1000.0f * Time.Delta;

		if ( Player.m_flWaterJumpTime <= 0 || Player.WaterLevelType == WaterLevelType.NotInWater ) 
		{
			Player.m_flWaterJumpTime = 0;
			Player.RemoveFlag( PlayerFlags.FL_WATERJUMP );
		}

		Move.Velocity[0] = Player.m_vecWaterJumpVel[0];
		Move.Velocity[1] = Player.m_vecWaterJumpVel[1];
	}

	public virtual bool CheckWaterJumpButton()
	{
		// See if we are water jumping.  If so, decrement count and return.
		if ( Player.IsJumpingFromWater )
		{
			Player.m_flWaterJumpTime -= Time.Delta;
			if ( Player.m_flWaterJumpTime < 0 )
			{
				Player.m_flWaterJumpTime = 0;
			}

			return false;
		}

		// In water above our waist.
		if ( Player.WaterLevelType >= WaterLevelType.Waist )
		{
			// Swimming, not jumping.
			SetGroundEntity( null );

			// We move up a certain amount.
			Move.Velocity.z = 100;

			// Play swimming sound.
			if ( Player.m_flSwimSoundTime <= 0 )
			{
				// Don't play sound again for 1 second.
				Player.m_flSwimSoundTime = 1000;
				Player.OnWaterWade();
			}

			return false;
		}

		return true;
	}
}
