using Sandbox;
using System;

namespace Source1
{
	public partial class S1GameMovement
	{
		public virtual void StartGravity()
		{
			if ( !IsSwimming && !IsTouchingLadder )
			{
				Velocity -= new Vector3( 0, 0, GetCurrentGravity() * 0.5f ) * Time.Delta;
				Velocity += new Vector3( 0, 0, BaseVelocity.z ) * Time.Delta;

				BaseVelocity = BaseVelocity.WithZ( 0 );
			}
		}

		public virtual void FinishGravity()
		{
			if ( !IsSwimming && !IsTouchingLadder )
			{
				Velocity -= new Vector3( 0, 0, GetCurrentGravity() * 0.5f ) * Time.Delta;
			}
		}

		public virtual float GetCurrentGravity()
		{
			return sv_gravity * GameRules.Instance.GetGravityMultiplier();
		}

		public virtual bool InAir()
		{
			return GroundEntity == null;
		}
	}
}
