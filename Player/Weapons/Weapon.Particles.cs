using System;
using System.Collections.Generic;
using Sandbox;

namespace Amper.Source1;

partial class Source1Weapon
{
	struct PersistentParticleData
	{
		public Entity Entity;
		public string Path;
		public string Attachment;
	}

	Dictionary<Particles, PersistentParticleData> PersistentParticles { get; set; } = new();

	public Particles CreatePersistentParticle( string effect, string attachment )
	{
		var effectEntity = GetEffectEntity();
		var particle = Particles.Create( effect, effectEntity, attachment );

		if ( particle == null )
			return null;

		PersistentParticles[particle] = new()
		{
			Entity = effectEntity,
			Path = effect,
			Attachment = attachment
		};

		return particle;
	}

	public void DisposePersistentParticle( Particles effect, bool immediate = false )
	{
		if ( effect == null )
			return;

		effect.Destroy( immediate );
		PersistentParticles.Remove( effect );
	}

	[Event.Frame]
	public virtual void EffectThink()
	{
		if ( ShouldAutoDisposePersistentParticles() )
		{
			DisposeAllPesistentParticles( true );
			return;
		}

		var effectEntity = GetEffectEntity();
		foreach ( var pair in PersistentParticles )
		{
			var particle = pair.Key;
			var data = pair.Value;

			if ( data.Entity != effectEntity )
			{
				particle.SetEntityAttachment( 0, effectEntity, data.Attachment );
				data.Entity = effectEntity;
				PersistentParticles[particle] = data;
			}
		}
	}

	private bool ShouldAutoDisposePersistentParticles()
	{
		if ( PersistentParticles.Count == 0 )
			return false;

		if ( !IsValid )
			return true;

		if ( !IsDeployed )
			return true;

		if ( IsDormant )
			return true;

		var ownerPlayer = Player;
		if ( !ownerPlayer.IsValid() )
			return true;

		return false;
	}

	public void DisposeAllPesistentParticles( bool immediate = false )
	{
		foreach ( var pair in PersistentParticles ) 
			DisposePersistentParticle( pair.Key, immediate );
	}
}
