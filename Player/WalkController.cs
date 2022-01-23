using Sandbox;
using System;

namespace Source1
{
	public partial class S1GameMovement : PawnController
	{
		public enum SpeedCropType
		{
			Reset,
			Duck,
			Weapon
		}

		public SpeedCropType SpeedCropped { get; set; }
		Player Player { get; set; }
		MoveHelper MoveHelper { get; set; }

		protected int m_nOldWaterLevel;
		protected float m_flWaterEntryTime;
		protected int m_nOnLadder;

		protected Vector3 m_vecForward;
		protected Vector3 m_vecRight;
		protected Vector3 m_vecUp;

		// Cache used to remove redundant calls to GetPointContents().
		// int m_CachedGetPointContents[MAX_PLAYERS][MAX_PC_CACHE_SLOTS];
		// Vector m_CachedGetPointContentsPoint[MAX_PLAYERS][MAX_PC_CACHE_SLOTS];	

		protected Vector3 m_vecProximityMins;      // Used to be globals in sv_user.cpp.
		protected Vector3 m_vecProximityMaxs;

		public int m_iSpeedCropped;
		// float m_flStuckCheckTime[MAX_PLAYERS + 1][2]; // Last time we did a full test

		public override void Simulate()
		{
			base.Simulate();

			float flStoreFrametime = Time.Delta;

			// ResetGetPointContentsCache();

			// Cropping movement speed scales mv->m_fForwardSpeed etc. globally
			// Once we crop, we don't want to recursively crop again, so we set the crop
			//  flag globally here once per usercmd cycle.
			SpeedCropped = SpeedCropType.Reset;

			// StartTrackPredictionErrors should have set this
			Player = Pawn as Player;

			// mv = pMove;
			// mv->m_flMaxSpeed = pPlayer->GetPlayerMaxSpeed();

			// CheckV( player->CurrentCommandNumber(), "StartPos", mv->GetAbsOrigin() );

			// DiffPrint( "start %f %f %f", mv->GetAbsOrigin().x, mv->GetAbsOrigin().y, mv->GetAbsOrigin().z );

			// Run the command.
			// PlayerMove();

			// FinishMove();

			// DiffPrint( "end %f %f %f", mv->GetAbsOrigin().x, mv->GetAbsOrigin().y, mv->GetAbsOrigin().z );

			// CheckV( player->CurrentCommandNumber(), "EndPos", mv->GetAbsOrigin() );

			// This is probably not needed, but just in case.
			// gpGlobals->frametime = flStoreFrametime;

			// 	player = NULL;
		}

		//-----------------------------------------------------------------------------
		// Purpose: 
		// Input  : ducked - 
		// Output : const Vector
		//-----------------------------------------------------------------------------
		public Vector3 GetPlayerMins( bool ducked )
		{
			return ducked ? VEC_DUCK_HULL_MIN_SCALED( player ) : VEC_HULL_MIN_SCALED( player );
		}

		//-----------------------------------------------------------------------------
		// Purpose: 
		// Input  : ducked - 
		// Output : const Vector
		//-----------------------------------------------------------------------------
		public Vector3 GetPlayerMaxs( bool ducked )
		{
			return ducked ? VEC_DUCK_HULL_MAX_SCALED( player ) : VEC_HULL_MAX_SCALED( player );
		}
	}
}
