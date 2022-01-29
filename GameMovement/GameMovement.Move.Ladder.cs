using Sandbox;
using System;

namespace Source1
{
	partial class Source1GameMovement
	{
		Vector3 LadderNormal { get; set; }
		public virtual float LadderDistance => 2;
		public virtual float ClimbSpeed => 200;

		public virtual void FullLadderMove()
		{
			CheckWater();

			if ( WishJump() ) Jump();

			Velocity -= BaseVelocity;
			TryPlayerMove();
			Velocity += BaseVelocity;
		}

		public virtual bool LadderMove()
		{
			if ( Player.MoveType == MoveType.MOVETYPE_NOCLIP ) 
				return false;

			if ( !GameHasLadders() )
				return false;

			Vector3 wishdir;

			// If I'm already moving on a ladder, use the previous ladder direction
			if ( Player.MoveType == MoveType.MOVETYPE_LADDER ) 
			{
				wishdir = -LadderNormal;
			}
			else
			{
				// otherwise, use the direction player is attempting to move
				if ( ForwardMove != 0 || SideMove != 0 ) 
				{
					wishdir = Input.Rotation.Forward * ForwardMove + Input.Rotation.Right * SideMove;
					wishdir = wishdir.Normal;
				}
				else
				{
					// Player is not attempting to move, no ladder behavior
					return false;
				}
			}

			// wishdir points toward the ladder if any exists
			var end = VectorMA( Position, LadderDistance, wishdir );

			var pm = SetupBBoxTrace( Position, end )
				.HitLayer( CollisionLayer.All, false )
				.HitLayer( CollisionLayer.LADDER, true )
				.Run();

			if ( pm.Fraction == 1 )
				return false;

			Player.MoveType = MoveType.MOVETYPE_LADDER;
			LadderNormal = pm.Normal;
			// On ladder, convert movement to be relative to the ladder

			var floor = Position;
			floor.z += GetPlayerMins().z - 1;

			bool onFloor = Physics.GetPointContents( floor ).HasFlag( CollisionLayer.Solid ) || IsGrounded(); 

			// player->SetGravity( 0 );

			float climbSpeed = ClimbSpeed;

			float forwardSpeed = 0, rightSpeed = 0;
			if ( Input.Down( InputButton.Back ) ) 
				forwardSpeed -= climbSpeed;

			if ( Input.Down( InputButton.Forward ) )
				forwardSpeed += climbSpeed;

			if ( Input.Down( InputButton.Left ) )
				rightSpeed -= climbSpeed;

			if ( Input.Down( InputButton.Right ) ) 
				rightSpeed += climbSpeed;

			if ( Input.Down( InputButton.Jump ) )
			{
				Player.MoveType = MoveType.MOVETYPE_WALK;
				// player->SetMoveCollide( MOVECOLLIDE_DEFAULT );

				Velocity = pm.Normal * 270;
			}
			else
			{
				if ( forwardSpeed != 0 || rightSpeed != 0 )
				{
					var velocity = Input.Rotation.Forward * forwardSpeed;
					velocity = VectorMA( velocity, rightSpeed, Input.Rotation.Right );

					Vector3 tmp = Vector3.Up;
					tmp[2] = 1;
					var perp = Vector3.Cross( tmp, pm.Normal );
					perp = perp.Normal;

					// decompose velocity into ladder plane
					float normal = Vector3.Dot( velocity, pm.Normal );

					// This is the velocity into the face of the ladder
					var cross = pm.Normal * normal;

					// This is the player's additional velocity
					var lateral = velocity - cross;

					// This turns the velocity into the face of the ladder into velocity that
					// is roughly vertically perpendicular to the face of the ladder.
					// NOTE: It IS possible to face up and move down or face down and move up
					// because the velocity is a sum of the directional velocity and the converted
					// velocity through the face of the ladder -- by design.
					tmp = Vector3.Cross( pm.Normal, perp );

					Velocity = VectorMA( lateral, -normal, tmp );

					if ( onFloor && normal > 0 )    // On ground moving away from the ladder
					{
						Velocity = VectorMA( Velocity, ClimbSpeed, pm.Normal );
					}
				}
				else
				{
					Velocity = 0;
				}
			}
			return true;
		}

		public virtual bool GameHasLadders()
		{
			return true;
		}
	}
}
