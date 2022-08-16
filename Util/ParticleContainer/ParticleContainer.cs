using Sandbox;
using System.Collections.Generic;

namespace Amper.Source1;

public class ParticleContainer
{
	IHasParticleContainer Outer;
	Entity Entity;

	List<ContainedParticle> ParticleEffects = new();

	public ParticleContainer( IHasParticleContainer outer )
	{
		Host.AssertClient();
		Assert.NotNull( outer );

		Event.Register( this );

		Outer = outer;
		Entity = outer as Entity;
		if ( !Entity.IsValid() )
		{
			Log.Error( "Particle container cannot accept containing classes that aren't Entities." );
			return;
		}
	}

	[Event.Tick.Client]
	public void Update()
	{
		// Check if our client isn't valid anymore so we operating.
		if ( !Entity.IsValid() )
		{
			// Dispose of everything.
			DeleteAllParticles();

			// Stop receiving event updates.
			Event.Unregister( this );
			return;
		}

		// Draw contained particles that are applied to this entity.
		if ( cl_debug_contained_particles )
			DebugDraw();

		var effectEntity = Outer.EffectEntity;
		if ( !effectEntity.IsValid() )
		{
			Log.Info( $"{Entity}'s Effect Entity is invalid, deleting all particles" );
			DeleteAllParticles();
			return;
		}

		// Loop and proccess everything.
		for ( var i = ParticleEffects.Count - 1; i >= 0; i-- )
		{
			var particle = ParticleEffects[i];

			// First we should check if we should check if this particle needs to be visible.
			CheckParticleVisibility( particle );

			// Check if effect should move to other effect entity.
			if ( particle.EffectEntity != effectEntity )
			{
				particle.EffectEntity = effectEntity;
				particle.Particle.SetEntity( 0, effectEntity, particle.Follow );
			}
		}
	}

	void DeleteAllParticles( bool immediate = false )
	{
		if ( ParticleEffects.Count == 0 )
			return;

		// Loop and proccess everything.
		for ( var i = ParticleEffects.Count - 1; i >= 0; i-- )
		{
			var particle = ParticleEffects[i];
			Delete( particle, immediate );
		}
	}

	void CheckParticleVisibility( ContainedParticle particle )
	{
		var shouldDraw = true;

		var effectEntity = particle.EffectEntity;
		if ( effectEntity.IsFirstPersonMode )
			shouldDraw = false;

		particle.Particle.EnableDrawing = shouldDraw;
	}


	void DebugDraw()
	{
		var str = "";
		for ( var i = 0; i < ParticleEffects.Count; i++ )
		{
			str += $"{i}: {ParticleEffects[i].Effect}\n";
		}

		DebugOverlay.Text( str, Entity.EyePosition );
	}

	[ConVar.Client] public static bool cl_debug_contained_particles { get; set; }

	public ContainedParticle Create( string effect, string attachment, bool follow = true )
	{
		var particles = Particles.Create( effect, Outer.EffectEntity, attachment, follow );
		return Create( particles, effect, attachment, follow );
	}

	public ContainedParticle Create( string effect, bool follow = true )
	{
		var particles = Particles.Create( effect, Outer.EffectEntity, follow );
		return Create( particles, effect, "", follow );
	}

	ContainedParticle Create( Particles particles, string effect, string attachment, bool follow )
	{
		var effectEntity = Outer.EffectEntity;
		Assert.NotNull( effectEntity );

		var containedParticle = new ContainedParticle( this, effectEntity, particles, effect, attachment, follow );
		ParticleEffects.Add( containedParticle );
		return containedParticle;
	}

	public void Delete( ContainedParticle particle, bool immediate = false )
	{
		if ( particle == null )
			return;

		particle.Particle?.Destroy( immediate );
		ParticleEffects.Remove( particle );
	}
}

public interface IHasParticleContainer
{
	public ParticleContainer ParticleContainer { get; }
	public Entity EffectEntity { get; }
}

public class ContainedParticle
{
	public ParticleContainer Container;
	public Entity EffectEntity;
	public Particles Particle;
	public string Effect;
	public bool Follow;
	public string Attachment;

	public ContainedParticle( ParticleContainer container, Entity effectEntity, Particles particle, string effect, string attachment, bool follow )
	{
		Container = container;
		Particle = particle;
		Follow = follow;
		Attachment = attachment;
		Effect = effect;
		EffectEntity = effectEntity;
	}

	public void Delete( bool immediate = false )
	{
		Container?.Delete( this, immediate );
	}
}
