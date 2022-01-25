using Sandbox;
using System;

namespace Source1
{
	public partial class S1GameMovement
	{
		//
		// Sprint
		//

		public virtual bool WishSprint()
		{
			return Input.Down( InputButton.Run );
		}

		public virtual bool CanSprint()
		{
			return true;
		}

		public virtual float GetSprintSpeed()
		{
			return 320;
		}

		//
		// Walk
		//

		public virtual bool WishWalk()
		{
			return Input.Down( InputButton.Walk );
		}

		public virtual bool CanWalk()
		{
			return true;
		}

		public virtual float GetWalkSpeed()
		{
			return 150;
		}

		//
		// Normal
		//

		public virtual float GetNormalSpeed()
		{
			return 190;
		}

		public virtual float GetWishSpeed()
		{
			if ( IsDucked ) return GetDuckSpeed();
			if ( CanSprint() && WishSprint() ) return GetSprintSpeed();
			if ( CanWalk() && WishWalk() ) return GetWalkSpeed();

			return GetNormalSpeed();
		}
	}
}
