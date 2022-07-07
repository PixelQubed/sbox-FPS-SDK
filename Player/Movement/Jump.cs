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

		Player.DoJumpSound( Position, Player.SurfaceData, 1 );
		Player.SetAnimParameter( "b_jump", true );

		var startz = Velocity[2];
		if ( Player.IsDucked )
		{
			Velocity[2] = JumpImpulse;
		}
		else
		{
			Velocity[2] += JumpImpulse;
		}

		FinishGravity();
		OnJump( Velocity.z - startz );

		return true;
	}

	public virtual void OnJump(float impulse ) { }
}
