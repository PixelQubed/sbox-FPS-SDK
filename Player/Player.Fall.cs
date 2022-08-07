using Sandbox;

namespace Amper.Source1;

partial class Source1Player
{
	[ConVar.Replicated] public static bool sv_falldamage { get; set; } = true;
	public float FallVelocity { get; set; }

	public virtual void OnLanded( float velocity )
	{
		TakeFallDamage( velocity );
		LandingEffects( velocity );
	}

	public virtual void TakeFallDamage( float velocity )
	{
		var fallDamage = GameRules.Current.GetPlayerFallDamage( this, velocity );
		if ( fallDamage <= 0 )
			return;

		PlaySound( "player.fallpain" );

		if ( sv_falldamage )
		{
			var fallDmgInfo = DamageInfo.Generic( fallDamage )
				.WithFlag( DamageFlags.Fall )
				.WithPosition( Position );

			TakeDamage( fallDmgInfo );
		}
	}

	public virtual float FatalFallSpeed => 1024;
	public virtual float MaxSafeFallSpeed => 580;
	public virtual float DamageForFallSpeed => 100 / (FatalFallSpeed - MaxSafeFallSpeed);

	public virtual void LandingEffects( float velocity )
	{
		if ( velocity <= 0 )
			return;

		var volume = .5f;
		if ( velocity > MaxSafeFallSpeed / 2 )
		{
			volume = velocity.RemapClamped( MaxSafeFallSpeed / 2, MaxSafeFallSpeed, .85f, 1 );
		}

		DoLandSound( Position, SurfaceData, volume );

		//
		// Knock the screen around a little bit, temporary effect.
		//
		if ( velocity >= MaxSafeFallSpeed )
		{
			ViewPunchAngle = new Vector3( 0, 0, velocity * 0.013f );
		}
	}
}
