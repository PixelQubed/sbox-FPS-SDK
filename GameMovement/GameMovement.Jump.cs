using Sandbox;
using System;

namespace Source1;

public partial class Source1GameMovement
{
	public float JumpTime { get; set; }

	public virtual bool WishJump()
	{
		return Input.Pressed( InputButton.Jump );
	}

	public virtual bool CanJump()
	{
		if ( IsInAir )
			return false;

		if ( IsDucked )
			return false;

		// Yeah why not.
		return true;
	}

	/// <summary>
	/// Returns true if we succesfully made a jump.
	/// </summary>
	/// <returns></returns>
	public virtual bool CheckJumpButton()
	{
		if ( !CheckWaterJumpButton() )
			return false;

		if ( !CanJump() )
			return false;

		ClearGroundEntity();

		Player.DoJumpSound( Position, Player.SurfaceData, 1 );

		AddEvent( "jump" );

		float flGroundFactor = 1.0f;
		float startz = Velocity.z;

		if ( IsDucking )
			Velocity = Velocity.WithZ( JumpImpulse * flGroundFactor );
		else
			Velocity = Velocity.WithZ( startz + JumpImpulse * flGroundFactor );

		FinishGravity();
		OnJump( Velocity.z - startz );

		return true;
	}

	public virtual float JumpImpulse => 321;

	public virtual void OnJump( float velocity )
	{

	}
}
