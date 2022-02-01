using Sandbox;
using System;
using System.Linq;

namespace Source1
{
	partial class Source1Player
	{
		private Rotation WishViewPunch { get; set; }
		public Rotation ViewPunch { get; private set; }

		public virtual void PunchView( float pitch, float yaw, float roll )
		{
			WishViewPunch = Rotation.From( pitch, yaw, roll );
		}

		public virtual void SimulateVisuals()
		{
			if ( IsClient )
			{
				DecayViewPunch();
			}
		}

		public void DecayViewPunch()
		{
			//
			// View Punch Angle
			//

			WishViewPunch = Rotation.Lerp( WishViewPunch, Rotation.Identity, Time.Delta * 5f );
			ViewPunch = Rotation.Lerp( ViewPunch, WishViewPunch, Time.Delta * 10f );
		}
	}
}
