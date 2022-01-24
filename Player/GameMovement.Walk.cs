using Sandbox;
using System;

namespace Source1
{
	public partial class S1GameMovement
	{
		public virtual void FullWalkMove()
		{
			if ( GroundEntity != null )
			{
				WalkMove();
			}
			else
			{
				AirMove();
			}

			// Redetermine position vars
			CategorizePosition();
		}
	}
}
