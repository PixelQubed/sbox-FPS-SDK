using Sandbox;
using System;

namespace Source1
{
	public enum WaterLevelType
	{
		NotInWater,
		Feet,
		Waist,
		Eyes
	}

	public partial class Source1GameMovement
	{
		protected float WaterJumpTime { get; set; }
		protected Vector3 WaterJumpVelocity { get; set; }
		protected bool IsJumpingFromWater => WaterJumpTime > 0;
		protected TimeSince TimeSinceSwimSound { get; set; }
		protected WaterLevelType LastWaterLevelType { get; set; }

		public virtual float WaterJumpHeight => 8;

		protected void CheckWaterJump()
		{
			var forward = Input.Rotation.Forward;

			// Already water jumping.
			if ( IsJumpingFromWater ) 
				return;

			// Don't hop out if we just jumped in
			if ( Velocity.z < -180 ) 
				return; // only hop out if we are moving up

			// See if we are backing up
			var flatvelocity = Velocity.WithZ( 0 );

			// Must be moving
			var curspeed = flatvelocity.Length;
			flatvelocity = flatvelocity.Normal;

			// see if near an edge
			var flatforward = forward.WithZ( 0 );
			flatforward = flatforward.Normal;

			// Are we backing into water from steps or something?  If so, don't pop forward
			if ( curspeed != 0 && Vector3.Dot( flatvelocity, flatforward ) < 0 )
				return;

			var vecStart = Position + (GetPlayerMins() + GetPlayerMaxs()) * .5f;
			var vecEnd = VectorMA( vecStart, 24.0f, flatforward );

			var tr = TraceBBox( vecStart, vecEnd );
			if ( tr.Fraction < 1 )        // solid at waist
			{
				vecStart.z = Position.z + GetPlayerViewOffset().z + WaterJumpHeight;
				vecEnd = VectorMA( vecStart, 24.0f, flatforward );
				WaterJumpVelocity = VectorMA( 0, -50.0f, tr.Normal );

				tr = TraceBBox( vecStart, vecEnd );
				if ( tr.Fraction == 1.0 )       // open at eye level
				{
					// Now trace down to see if we would actually land on a standable surface.
					vecStart = vecEnd;
					vecEnd.z -= 1024.0f;

					tr = TraceBBox( vecStart, vecEnd );
					if ( (tr.Fraction < 1.0f) && (tr.Normal.z >= 0.7) ) 
					{
						Velocity = Velocity.WithZ( 256 );
						Player.Tags.Add( PlayerTags.WaterJump );
						WaterJumpTime = 2000.0f;
					}
				}
			}
		}

		public virtual void WaterJump()
		{
			if ( WaterJumpTime > 10000 )
				WaterJumpTime = 10000;

			if ( WaterJumpTime == 0 ) 
				return;

			WaterJumpTime -= 1000.0f * Time.Delta;

			if ( WaterJumpTime <= 0 || Player.WaterLevelType == WaterLevelType.NotInWater ) 
			{
				WaterJumpTime = 0;
				Player.Tags.Remove( PlayerTags.WaterJump );
			}

			var velocity = WaterJumpVelocity.WithZ( Velocity.z );
			Velocity = velocity;
		}

		public virtual bool CheckWater()
		{
			var vPlayerExtents = GetPlayerExtents();
			var playerHeight = vPlayerExtents.z;

			var eyeHeight = EyePosLocal.z;

			var waterFraction = Player.WaterLevel.Fraction;
			var waterHeight = waterFraction * playerHeight;

			// last water type
			var lastWaterType = Player.WaterLevelType;

			Player.WaterLevelType = WaterLevelType.NotInWater;


			if ( waterFraction > 0 )
			{
				Player.WaterLevelType = WaterLevelType.Feet;

				if ( waterHeight > eyeHeight )
				{
					Player.WaterLevelType = WaterLevelType.Eyes;
				}
				else if ( waterFraction > 0.5f )
				{
					Player.WaterLevelType = WaterLevelType.Waist;
				}
			}

			// check water events
			var newWaterType = Player.WaterLevelType;
			if ( newWaterType != lastWaterType )
			{
				if ( lastWaterType == WaterLevelType.NotInWater ) Player.OnEnterWater();
				if ( newWaterType == WaterLevelType.NotInWater ) Player.OnLeaveWater();

				if ( lastWaterType == WaterLevelType.Eyes ) Player.OnLeaveUnderwater();
				if ( newWaterType == WaterLevelType.Eyes ) Player.OnEnterUnderwater();
			}

			return waterFraction >= 0.5f;
		}

		protected void WaterMove()
		{
			var forward = Input.Rotation.Forward;
			var right = Input.Rotation.Right;
			var up = Input.Rotation.Up;

			//
			// user intentions
			//
			var wishvel = forward * ForwardMove + right * SideMove;

			// if we have the jump key down, move us up as well
			if ( Input.Down( InputButton.Jump ) ) 
			{
				wishvel[2] += MaxSpeed;
			}

			// Sinking after no other movement occurs
			else if ( ForwardMove == 0 && SideMove == 0 && UpMove == 0 ) 
			{
				wishvel[2] -= 60;       // drift towards bottom
			}
			else  // Go straight up by upmove amount.
			{
				// exaggerate upward movement along forward as well
				float upwardMovememnt = ForwardMove * forward.z * 2;
				upwardMovememnt = Math.Clamp( upwardMovememnt, 0, MaxSpeed );
				wishvel[2] += UpMove + upwardMovememnt;
			}

			var wishdir = wishvel;

			// Copy it over and determine speed
			var wishspeed = wishdir.Length;
			wishdir = wishdir.Normal;

			// Cap speed.
			if ( wishspeed > MaxSpeed )
			{
				wishvel *= MaxSpeed / wishspeed;
				wishspeed = MaxSpeed;
			}

			// Slow us down a bit.
			wishspeed *= 0.8f;

			// Water friction
			var temp = Velocity;
			var speed = temp.Length;

			var newspeed = 0f;
			if ( speed != 0 ) 
			{
				newspeed = speed - Time.Delta * speed * sv_friction * SurfaceFriction;
				if ( newspeed < 0.1f ) newspeed = 0;
				Velocity *= newspeed / speed;
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

					var accelspeed = sv_accelerate * wishspeed * Time.Delta * SurfaceFriction;
					if ( accelspeed > addspeed ) accelspeed = addspeed;

					Velocity += accelspeed * wishvel;
				}
			}

			Velocity += BaseVelocity;

			// Now move
			// assume it is a stair or a slope, so press down from stepheight above
			var dest = VectorMA( Position, Time.Delta, Velocity );

			var pm = TraceBBox( Position, dest);
			if ( pm.Fraction == 1.0f ) 
			{
				var start = dest;
				if ( Player.AllowAutoMovement )
				{
					start[2] += sv_stepsize + 1;
				}

				pm = TraceBBox( start, dest );
				if ( !pm.StartedSolid ) 
				{
					// walked up the step, so just keep result and exit
					Position = pm.EndPos;
					Velocity -= BaseVelocity;
					return;
				}

				// Try moving straight along out normal path.
				TryPlayerMove();
			}
			else
			{
				if ( !IsGrounded() )
				{
					TryPlayerMove();
					Velocity -= BaseVelocity;
					return;
				}

				StepMove( dest );
			}

			Velocity -= BaseVelocity;
		}

		public bool InWater()
		{
			return Player.WaterLevelType > WaterLevelType.Feet;
		}

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
			if ( IsGrounded() )
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
}
