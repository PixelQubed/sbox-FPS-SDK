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
		float m_flWaterJumpTime { get; set; }
		Vector3 m_vecWaterJumpVel { get; set; }
		bool IsJumpingFromWater => m_flWaterJumpTime > 0;

		public virtual float WaterJumpHeight => 8;

		void CheckWaterJump()
		{
			var forward = Input.Rotation.Forward;

			// Already water jumping.
			if ( IsJumpingFromWater ) 
				return;

			Log.Info( "sus" );

			// Don't hop out if we just jumped in
			if ( Velocity.z < -180 ) 
				return; // only hop out if we are moving up

			// See if we are backing up
			var flatvelocity = Velocity.WithZ( 0 );

			// Must be moving
			var curspeed = flatvelocity.Length;

			// see if near an edge
			var flatforward = forward.WithZ( 0 );
			flatforward = flatforward.Normal;

			// Are we backing into water from steps or something?  If so, don't pop forward
			if ( curspeed != 0.0 && Vector3.Dot( flatvelocity, flatforward ) < 0 ) 
				return;

			var vecStart = Position + (GetPlayerMins() + GetPlayerMaxs()) * .5f;
			var vecEnd = VectorMA( vecStart, 24.0f, flatforward );

			var tr = TraceBBox( vecStart, vecEnd );
			if ( tr.Fraction < 1 )        // solid at waist
			{
				vecStart.z = Position.z + GetPlayerViewOffset().z + WaterJumpHeight;
				vecEnd = VectorMA( vecStart, 24.0f, flatforward );
				m_vecWaterJumpVel = VectorMA( 0, -50.0f, tr.Normal );

				tr = TraceBBox( vecStart, vecEnd );
				if ( tr.Fraction == 1.0 )       // open at eye level
				{
					// Now trace down to see if we would actually land on a standable surface.
					vecStart = vecEnd;
					vecEnd.z -= 1024.0f;

					tr = TraceBBox( vecStart, vecEnd );
					if ( (tr.Fraction < 1.0f) && (Vector3.GetAngle( Vector3.Up, tr.Normal ) >= sv_maxstandableangle) ) 
					{
						Velocity = Velocity.WithZ( 256 );
						Player.Tags.Add( PlayerTags.WaterJump );
						m_flWaterJumpTime = 2000.0f;
					}
				}
			}
		}

		public virtual void WaterJump()
		{
			if ( m_flWaterJumpTime > 10000 )
				m_flWaterJumpTime = 10000;

			if ( m_flWaterJumpTime == 0 ) 
				return;

			m_flWaterJumpTime -= 1000.0f * Time.Delta;

			if ( m_flWaterJumpTime <= 0 || Player.WaterLevelType == WaterLevelType.NotInWater ) 
			{
				m_flWaterJumpTime = 0;
				Player.Tags.Remove( PlayerTags.WaterJump );
			}

			var velocity = m_vecWaterJumpVel.WithZ( Velocity.z );
			Velocity = velocity;
		}

		public virtual bool CheckWater()
		{
			var vPlayerExtents = GetPlayerExtents();
			var playerHeight = vPlayerExtents.z;

			var eyeHeight = EyePosLocal.z;

			var waterFraction = Player.WaterLevel.Fraction;
			var waterHeight = waterFraction * playerHeight;

			Player.WaterLevelType = WaterLevelType.NotInWater;

			if ( waterFraction > 0 )
			{
				Player.WaterLevelType = WaterLevelType.Feet;

				Log.Info( $"{waterHeight} {eyeHeight}" );
				if ( waterHeight > eyeHeight )
				{
					Player.WaterLevelType = WaterLevelType.Eyes;
				}
				else if ( waterFraction > 0.5f )
				{
					Player.WaterLevelType = WaterLevelType.Waist;
				}
			}

			return waterFraction >= 0.5f;
		}
		//-----------------------------------------------------------------------------
		// Purpose: 
		//-----------------------------------------------------------------------------
		void WaterMove(  )
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
			temp = temp.Normal;

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

			var addspeed = 0f;
			var accelspeed = 0f;

			// water acceleration
			if ( wishspeed >= 0.1f )  // old !
			{
				addspeed = wishspeed - newspeed;
				if ( addspeed > 0 )
				{
					wishvel = wishvel.Normal;

					accelspeed = sv_accelerate * wishspeed * Time.Delta * SurfaceFriction;
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
	}
}
