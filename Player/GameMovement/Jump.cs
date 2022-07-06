using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Amper.Source1;

partial class GameMovement
{
	public bool WishJump() => Input.Pressed( InputButton.Jump );

	public bool CanJump()
	{
		if ( !Player.IsAlive )
			return false;

		if ( !Player.GroundEntity.IsValid() )
			return false;

		return true;
	}

	public virtual bool CheckJumpButton()
	{
		if ( !CheckWaterJumpButton() )
			return false;

		if ( !CanJump() )
			return false;

		SetGroundEntity( null );

		Player.DoJumpSound( Move.Position, Player.m_pSurfaceData, 1 );
		Player.SetAnimParameter( "b_jump", true );

		var startz = Move.Velocity[2];
		if ( Player.m_bDucking || Player.Flags.HasFlag( PlayerFlags.FL_DUCKING ) )
		{
			Move.Velocity[2] = JumpImpulse;
		}
		else
		{
			Move.Velocity[2] += JumpImpulse;
		}

		FinishGravity();
		OnJump( Move.Velocity.z - startz );

		return true;
	}

	public virtual void OnJump(float impulse ) { }
}
