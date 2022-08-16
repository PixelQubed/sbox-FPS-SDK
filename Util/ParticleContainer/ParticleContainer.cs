using Sandbox;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Amper.Source1;

public class EntityParticleManager
{
	IHasEntityParticleManager Outer;
	Entity Entity;
	Entity EffectEntity;

	List<EntityParticle> ParticleEffects = new();
	List<ParticleContainer> Containers = new();

	public EntityParticleManager( IHasEntityParticleManager outer )
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

		EffectEntity = Outer.EffectEntity;
		if ( !EffectEntity.IsValid() )
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
			UpdateParticleVisibility( particle );

			UpdateParticleEffectEntity( particle );

			// TODO: Check if particle effect was disposed and remove it from here.
			if ( CheckAutoDispose( particle ) )
				particle.Destroy( true );
		}

		UpdateContainers();
	}

	void UpdateContainers()
	{
		foreach ( var container in Containers )
			container.Update();
	}

	void DeleteAllParticles( bool immediate = false )
	{
		if ( ParticleEffects.Count == 0 )
			return;

		// Loop and proccess everything.
		for ( var i = ParticleEffects.Count - 1; i >= 0; i-- )
		{
			var particle = ParticleEffects[i];
			Destroy( particle, immediate );
		}
	}

	void UpdateParticleVisibility( EntityParticle particle )
	{
		// Particle can draw if the first control point is connected
		// to an entity that is currently not being view through first person.
		var canDraw = false;

		var entity = particle.GetControlPoint( 0 )?.Entity;
		if ( entity.IsValid() )
		{
			if ( !entity.IsFirstPersonMode )
				canDraw = true;
		}

		particle.Particle.EnableDrawing = canDraw;
	}

	void UpdateParticleEffectEntity( EntityParticle particle )
	{
		// See if any particle control points are currently attached to the effect entity.
		foreach ( var pair in particle.Points )
		{
			var index = pair.Key;
			var point = pair.Value;

			// This control point doesn't use effect entity so there's not point in checking it.
			if ( !point.UseEffectEntity )
				continue;

			// Check if the effect entity has changed.
			if ( point.Entity == EffectEntity )
				continue;

			var attachment = point.Attachment;
			var follow = point.Follow;
			particle.SetControlPoint( index, EffectEntity, attachment, follow );
		}
	}

	bool CheckAutoDispose( EntityParticle particle )
	{
		if ( particle.ExpirationTime.HasValue )
		{
			if ( Time.Now >= particle.ExpirationTime.Value )
				return true;
		}

		return false;
	}

	void DebugDraw()
	{
		if ( !Entity.IsValid() )
			return;

		if ( !Entity.EnableDrawing )
			return;

		var str = $"{Entity}\n" +
			$"- Effect Entity: {EffectEntity}\n" +
			$"- Particles:\n";

		for ( var i = 0; i < ParticleEffects.Count; i++ )
		{
			var particle = ParticleEffects[i];

			// Display how much time until expiration.
			var time = -1f;
			if ( particle.ExpirationTime.HasValue )
				time = (MathF.Max( particle.ExpirationTime.Value - Time.Now, 0 ) * 100).NearestToInt() / 100f;

			var lifeStr = time == -1 ? "∞" : $"{time}s";

			str += $"  - {i}: {particle.Effect} ({lifeStr}) (visible: {particle.Particle.EnableDrawing})\n";
		}

		str += $"- Containers:\n";

		for ( var i = 0; i < Containers.Count; i++ )
		{
			var container = Containers[i];
			str += $"  - {i}: {container.Attachment}\n";

			var activeBinding = container.ActiveBinding;
			for ( var j = 0; j < container.Bindings.Count; j++ )
			{
				var binding = container.Bindings[j];
				
				var effectName = binding.EffectName;
				if ( string.IsNullOrEmpty( effectName ) )
				{
					effectName = binding.EffectNameDelegate == null 
						? "[! NO NAME !]" 
						: "<DELEGATE>";
				}

				var checkedStr = activeBinding == j 
					? "[✓]" 
					: "[ ]";

				str += $"    - {j}: {checkedStr} {effectName} {binding.Priority}\n";
			}
		}

		var pos = Entity.EyePosition + Vector3.Up * 32;
		DebugOverlay.Text( str, pos );
	}

	[ConVar.Client] public static bool cl_debug_contained_particles { get; set; }

	public EntityParticle Create( string effect, string attachment, bool follow = true, float lifeTime = -1 )
	{
		Assert.NotNull( EffectEntity );

		// Create the particle system
		var pcf = Particles.Create( effect );
		if ( pcf == null )
			return null;

		// Create a container wrapper for this system.
		var entry = new EntityParticle( this, pcf, effect, lifeTime );
		// Attach the particle to the effect entity as first control point.
		SetControlPoint( entry, 0, EffectEntity, attachment, follow );

		// Add particle to the registry.
		ParticleEffects.Add( entry );
		return entry;
	}
	public EntityParticle Create( string effect, bool follow = true, float lifeTime = -1 ) => Create( effect, "", follow, lifeTime );

	public void Destroy( EntityParticle particle, bool immediate = false )
	{
		if ( particle == null )
			return;

		particle.Particle?.Destroy( immediate );
		ParticleEffects.Remove( particle );
	}

	public void SetControlPoint( EntityParticle particle, int point, Entity entity, string attachment, bool follow = true )
	{
		if ( particle == null )
			return;

		particle.Points[point] = new EntityParticle.ControlPoint
		{
			Entity = entity,
			Attachment = attachment,
			Follow = follow,
			UseEffectEntity = entity == EffectEntity
		};

		if ( string.IsNullOrEmpty( attachment ) )
			particle.Particle.SetEntity( point, entity, follow );
		else
			particle.Particle.SetEntityAttachment( point, entity, attachment, follow );
	}

	public void RegisterContainer( ParticleContainer container )
	{
		if ( Containers.Contains( container ) )
			return;

		Containers.Add( container );
	}
}

public interface IHasEntityParticleManager
{
	public EntityParticleManager ParticleManager { get; }
	public Entity EffectEntity { get; }
}

public class EntityParticle
{
	public EntityParticleManager Container;
	public Particles Particle;
	public string Effect;
	public Dictionary<int, ControlPoint> Points = new();
	public float? ExpirationTime;
	public int ControlPointCount => Points.Count;

	[ConVar.Client] public static float cl_particle_manager_auto_dispose_time { get; set; } = 10;

	public EntityParticle( EntityParticleManager container, Particles particle, string effect, float lifeTime = -1 )
	{
		if ( lifeTime < 0 )
			lifeTime = cl_particle_manager_auto_dispose_time;

		Container = container;
		Particle = particle;
		Effect = effect;
		ExpirationTime = Time.Now + lifeTime;
	}

	public void Destroy( bool immediate = false )
	{
		Container?.Destroy( this, immediate );
	}

	public void SetControlPoint( int point, Entity entity, string attachment, bool follow = true )
	{
		Container?.SetControlPoint( this, point, entity, attachment, follow );
	}

	public void SetControlPoint( int point, Entity entity, bool follow = true )
	{
		Container?.SetControlPoint( this, point, entity, "", follow );
	}

	public ControlPoint? GetControlPoint( int point )
	{
		if ( Points.TryGetValue( point, out var value ) )
			return value;

		return null;
	}

	/// <summary>
	/// Calling this function will make particle not be automatically deleted 
	/// to cull particles.
	/// </summary>
	public void MakePersistent()
	{
		ExpirationTime = null;
	}

	public struct ControlPoint
	{
		public Entity Entity;
		public string Attachment;
		public bool Follow;
		public bool UseEffectEntity;
	}
}

public class ParticleContainer
{
	IHasEntityParticleManager Outer;
	public EntityParticle Particle;
	public string Attachment;
	public bool Follow;
	public List<Binding> Bindings = new();
	public int ActiveBinding = -1;

	public ParticleContainer( IHasEntityParticleManager outer, string attachment = "", bool follow = true )
	{
		Assert.NotNull( outer );
		Assert.NotNull( outer.ParticleManager );

		Outer = outer;
		Attachment = attachment;
		Follow = follow;

		outer.ParticleManager.RegisterContainer( this );
	}

	public void StartEffect( EntityParticle particle, bool stopImmediate = true )
	{
		StopEffect( stopImmediate );
		Particle = particle;
	}

	public EntityParticle StartEffect( string effectname, bool stopImmediate = true )
	{
		var particle = Outer.ParticleManager.Create( effectname, Attachment, Follow );
		StartEffect( particle, stopImmediate );
		return particle;
	}

	public void StopEffect( bool immediate = false )
	{
		Particle?.Destroy( immediate );
		Particle = null;
	}

	public void Bind( string effectName, int priority, Func<bool> condition, Action<EntityParticle> onCreated = null )
	{
		AddBinding( effectName, null, priority, condition, onCreated );
	}

	public void Bind( Func<string> effectName, int priority, Func<bool> condition, Action<EntityParticle> onCreated = null )
	{
		AddBinding( "", effectName, priority, condition, onCreated );
	}

	void AddBinding(string effectName, Func<string> nameDelegate, int priority, Func<bool> condition, Action<EntityParticle> onCreated = null )
	{
		if ( string.IsNullOrEmpty( effectName ) )
			Assert.NotNull( nameDelegate );
		Assert.NotNull( condition );

		Bindings.Add( new()
		{
			EffectName = effectName,
			EffectNameDelegate = nameDelegate,
			Priority = priority,
			ConditionDelegate = condition,
			OnCreated = onCreated
		} );

		Bindings = Bindings.OrderByDescending( x => x.Priority ).ToList();
	}

	public void Update()
	{
		var foundActiveBinding = false;

		for ( var i = 0; i < Bindings.Count; i++ )
		{
			var binding = Bindings[i];
			if ( binding.ConditionDelegate == null )
				continue;

			if ( !binding.ConditionDelegate.Invoke() )
				continue;

			// New binding!
			if ( ActiveBinding != i )
			{
				ActiveBinding = i;

				// Calculate effect name.
				var effectName = binding.EffectName;
				if ( binding.EffectNameDelegate != null )
					effectName = binding.EffectNameDelegate.Invoke();

				var effect = StartEffect( effectName );
				effect.MakePersistent();

				if ( binding.OnCreated != null )
					binding.OnCreated( effect );
			}

			foundActiveBinding = true;
			break;
		}

		if ( ActiveBinding >= 0 && !foundActiveBinding )
		{
			StopEffect();
			ActiveBinding = -1;
		}
	}

	public struct Binding
	{
		public string EffectName;
		public int Priority;
		public Func<string> EffectNameDelegate;
		public Func<bool> ConditionDelegate;
		public Action<EntityParticle> OnCreated;
	}
}
