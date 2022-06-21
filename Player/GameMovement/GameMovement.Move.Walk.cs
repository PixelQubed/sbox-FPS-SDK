using Sandbox;

namespace Amper.Source1;

public partial class GameMovement
{

	public void CheckFalling()
	{
		if ( IsInAir || Player.FallVelocity <= 0 || IsDead )
			return;

		// let any subclasses know that the player has landed and how hard
		OnLand( Player.FallVelocity );

		//
		// Clear the fall velocity so the impact doesn't happen again.
		//
		Player.FallVelocity = 0;
	}

	public virtual void OnLand( float velocity ) 
	{
		// Take specified amount of fall damage when landed.
		Player.OnLanded( velocity );
	}
}
