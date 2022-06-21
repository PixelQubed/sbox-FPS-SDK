using Sandbox;
using System;

namespace Amper.Source1;

public enum WaterLevelType
{
	NotInWater,
	Feet,
	Waist,
	Eyes
}

public partial class GameMovement
{
	protected float WaterJumpTime { get; set; }
	protected Vector3 WaterJumpVelocity { get; set; }
	protected bool IsJumpingFromWater => WaterJumpTime > 0;
	protected TimeSince TimeSinceSwimSound { get; set; }
	protected WaterLevelType LastWaterLevelType { get; set; }


	public virtual void FullWalkMoveUnderwater()
	{
		if ( Player.WaterLevelType == WaterLevelType.Waist )
		{
			CheckWaterJump();
		}

		// If we are falling again, then we must not trying to jump out of water any more.
		if ( (Velocity.z < 0.0f) && IsJumpingFromWater )
		{
			WaterJumpTime = 0.0f;
		}

		m_flWaterEntryTime

		// Was jump button pressed?
		if ( Input.Down( InputButton.Jump ) )
		{
			CheckJumpButton();
		}

		// Perform regular water movement
		WaterMove();

		// Redetermine position vars
		CategorizePosition();

		// If we are on ground, no downward velocity.
		if ( IsGrounded )
		{
			Velocity = Velocity.WithZ( 0 );
		}

		SetTag( "swimming" );
	}

	public virtual bool CheckWaterJumpButton()
	{
		// See if we are water jumping.  If so, decrement count and return.
		if ( IsJumpingFromWater )
		{
			WaterJumpTime -= Time.Delta;
			if ( WaterJumpTime < 0 )
			{
				WaterJumpTime = 0;
			}

			return false;
		}

		// In water above our waist.
		if ( Player.WaterLevelType >= WaterLevelType.Waist )
		{
			// Swimming, not jumping.
			ClearGroundEntity();

			// We move up a certain amount.
			Velocity = Velocity.WithZ( 100 );

			// Play swimming sound.
			if ( TimeSinceSwimSound > 1 )
			{
				// Don't play sound again for 1 second.
				TimeSinceSwimSound = 0;
				Player.OnWaterWade();
			}

			return false;
		}

		return true;
	}
}
