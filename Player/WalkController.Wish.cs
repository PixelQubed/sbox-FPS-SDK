using Sandbox;
using System;

namespace Source1
{
	public partial class S1GameMovement : PawnController
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
			return 140;
		}

		//
		// Duck
		//

		public virtual bool WishDuck()
		{
			return Input.Down( InputButton.Duck );
		}

		public virtual bool CanDuck()
		{
			return true;
		}

		public virtual float GetDuckSpeed()
		{
			return 60;
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
			return 80;
		}

		//
		// Normal
		//

		public virtual float GetNormalSpeed()
		{
			return 80;
		}

		public virtual float GetWishSpeed()
		{
			if ( CanDuck() && WishDuck() ) return GetDuckSpeed();
			if ( CanSprint() && WishSprint() ) return GetSprintSpeed();
			if ( CanWalk() && WishWalk() ) return GetWalkSpeed();

			return GetNormalSpeed();
		}
	}
}
