using Sandbox;
using System;

namespace Source1
{
	public partial class Source1GameMovement
	{
		public virtual void FullNoClipMove( float factor, float maxacceleration )
		{
			float maxspeed = sv_maxspeed * factor;

			var forward = Input.Rotation.Forward;
			var right = Input.Rotation.Right;

			if ( Input.Down( InputButton.Run ) ) factor /= 2.0f;

			// Copy movement amounts
			float fmove = ForwardMove * factor;
			float smove = SideMove * factor;

			var wishvel = forward * fmove + right * smove;
			wishvel = wishvel.WithZ( wishvel.z + UpMove * factor );

			var wishdir = wishvel.Normal;
			var wishspeed = wishvel.Length;

			//
			// Clamp to server defined max speed
			//
			if ( wishspeed > maxspeed )
			{
				wishvel *= maxspeed / wishspeed;
				wishspeed = maxspeed;
			}

			if ( maxacceleration > 0.0 )
			{
				// Set pmove velocity
				Accelerate( wishdir, wishspeed, maxacceleration );

				float spd = Velocity.Length;
				if ( spd < 1 )
				{
					Velocity = 0;
					return;
				}

				// Bleed off some speed, but if we have less than the bleed
				//  threshhold, bleed the theshold amount.
				float control = (spd < maxspeed / 4) ? (maxspeed / 4) : spd;

				float friction = sv_friction * SurfaceFriction;

				// Add the amount to the drop amount.
				float drop = control * friction * Time.Delta;

				// scale the velocity
				float newspeed = spd - drop;
				if ( newspeed < 0 )
					newspeed = 0;

				// Determine proportion of old speed we are using.
				newspeed /= spd;
				Velocity *= newspeed;
			}
			else
			{
				Velocity = wishvel;
			}

			// Just move ( don't clip or anything )
			var vecOut = VectorMA( Position, Time.Delta, Velocity );
			Position = vecOut;

			// Zero out velocity if in noaccel mode
			if ( maxacceleration < 0.0f )
			{
				Velocity = 0;
			}
		}

		public virtual void FullObserverMove()
		{
			var mode = Player.ObserverMode;

			if ( mode == ObserverMode.InEye || mode == ObserverMode.Chase )
			{

				// return;
			}

			if ( mode != ObserverMode.Roaming )
			{
				// don't move in fixed or death cam mode
				// return;
			}

			if ( sv_spectator_noclip )
			{
				// roam in noclip mode
				FullNoClipMove( sv_spectator_speed, sv_spectator_accelerate );
				return;
			}

			// do a full clipped free roam move:

			var forward = Input.Rotation.Forward;
			var right = Input.Rotation.Right;
			var up = Input.Rotation.Up;	

			// Copy movement amounts

			float factor = sv_spectator_speed;
			if ( Input.Down( InputButton.Run ) ) 
			{
				factor /= 2.0f;
			}

			float fmove = ForwardMove * factor;
			float smove = SideMove * factor;

			var wishvel = forward * fmove + right * smove;
			wishvel = wishvel.WithZ( wishvel.z + UpMove );

			var wishdir = wishvel.Normal;
			var wishspeed = wishvel.Length;

			//
			// Clamp to server defined max speed
			//

			float maxspeed = sv_maxvelocity;


			if ( wishspeed > maxspeed )
			{
				wishvel *= MaxSpeed / wishspeed;
				wishspeed = maxspeed;
			}

			// Set pmove velocity, give observer 50% acceration bonus
			Accelerate( wishdir, wishspeed, sv_spectator_accelerate );

			float spd = Velocity.Length;
			if ( spd < 1.0f )
			{
				Velocity = 0;
				return;
			}

			float friction = sv_friction;

			// Add the amount to the drop amount.
			float drop = spd * friction * Time.Delta;

			// scale the velocity
			float newspeed = spd - drop;

			if ( newspeed < 0 )
				newspeed = 0;

			// Determine proportion of old speed we are using.
			newspeed /= spd;

			Velocity *= newspeed;
			CheckVelocity();

			TryPlayerMove();
		}
	}
}
