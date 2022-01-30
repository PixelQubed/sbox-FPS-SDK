using Sandbox;
using System;

namespace Source1
{
	partial class Source1Player
	{
		public virtual void OnWaterWade()
		{
			PlayWaterWadeSound();
		}

		public virtual void OnEnterWater()
		{
			PlayWaterWadeSound();
		}

		public virtual void OnLeaveWater()
		{
			PlayWaterWadeSound();
		}

		public virtual void OnEnterUnderwater()
		{
			StartUnderwaterSound();
		}

		public virtual void OnLeaveUnderwater()
		{
			PlayWaterWadeSound();
			StopUnderwaterSound();
		}

		protected virtual void PlayWaterWadeSound()
		{
			PlaySound( "player.footstep.wade" );
		}

		protected virtual void PlayWaterSplashSound()
		{
			PlaySound( "physics.watersplash" );
		}

		Sound UnderwaterSound { get; set; }

		protected virtual void StartUnderwaterSound()
		{
			if ( !IsLocalPawn ) return;
			UnderwaterSound = PlaySound( "player.underwater" );
		}

		protected virtual void StopUnderwaterSound()
		{
			if ( !IsLocalPawn ) return;
			UnderwaterSound.Stop();
		}
	}
}
