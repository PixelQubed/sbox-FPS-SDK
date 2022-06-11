using Sandbox;

namespace Amper.Source1;

partial class Source1Player
{
	[Net] public Entity HoveredEntity { get; private set; }
	[Net] public Entity Using { get; protected set; }

	protected virtual Entity FindHovered()
	{
		var tr = Trace.Ray( EyePosition, EyePosition + EyeRotation.Forward * 5000 )
			.Ignore( this )
			.HitLayer( CollisionLayer.Solid )
			.HitLayer( CollisionLayer.Debris )
			.Run();

		if ( !tr.Entity.IsValid() ) 
			return null;

		if ( tr.Entity.IsWorld ) 
			return null;

		return tr.Entity;
	}

	protected virtual void SimulateHover()
	{
		// The entity we're currently looking at.
		HoveredEntity = FindHovered();

		using var _ = Prediction.Off();

		// If we pressed use button.
		if ( Input.Pressed( InputButton.Use ) )
		{
			if ( CanUse( HoveredEntity ) )
			{
				// Start using the hovered entity.
				StartUsing( HoveredEntity );
			}
		}

		// If we stopped pressing use key, stop using.
		if ( !Input.Down( InputButton.Use ) )
		{
			StopUsing();
			return;
		}

		// We dont have an entity to use.
		if ( !Using.IsValid() )
			return;

		if ( !CanContinueUsing( Using ) ) 
			StopUsing();
	}

	protected void StopUsing()
	{
		Using = null;
	}

	public void StartUsing( Entity entity )
	{
		Using = entity;
	}

	public virtual bool CanUse( Entity entity )
	{
		if ( entity is not IUse use )
			return false;

		if ( !use.IsUsable( this ) )
			return false;

		if ( entity.Position.Distance( Position ) > sv_max_use_distance )
			return false;

		return true;
	}

	public virtual bool CanContinueUsing( Entity entity )
	{
		if ( HoveredEntity != entity )
			return false;

		if ( entity is IUse use && use.OnUse( this ) )
			return true;

		return false;
	}

	[ConVar.Replicated] public static float sv_max_use_distance { get; set; } = 100;
}
