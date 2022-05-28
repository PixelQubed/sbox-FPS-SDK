using Sandbox;

namespace Amper.Source1;

partial class Source1Player
{
	/// <summary>
	/// Entity that the client is looking at.
	/// </summary>
	public Entity HoveredEntity { get; private set; }

	protected virtual Entity FindHovered()
	{
		var tr = Trace.Ray( EyePosition, EyePosition + EyeRotation.Forward * 5000 )
			.Ignore( this )
			.HitLayer( CollisionLayer.Solid )
			.HitLayer( CollisionLayer.Debris )
			.Run();

		if ( !tr.Entity.IsValid() ) return null;
		if ( tr.Entity == null ) return null;
		if ( tr.Entity.IsWorld ) return null;

		return tr.Entity;
	}

	protected virtual void SimulateHover()
	{
		// The entity we're currently looking at.
		HoveredEntity = FindHovered();

		// Turn prediction off
		using ( Prediction.Off() )
		{
			if ( Input.Pressed( InputButton.Use ) )
			{
				if ( CanUse( HoveredEntity ) )
				{
					Using = HoveredEntity;
				}
				else
				{
					UseFail();
				}
			}

			if ( !Input.Down( InputButton.Use ) )
			{
				StopUsing();
				return;
			}

			if ( !Using.IsValid() )
				return;

			// If we move too far away or something we should probably ClearUse()?

			//
			// If use returns true then we can keep using it
			//
			if ( Using is IUse use && use.OnUse( this ) )
				return;

			StopUsing();
		}
	}

	public bool CanUse( Entity entity )
	{
		return entity is IUse use && use.IsUsable( this ) && Vector3.DistanceBetween( entity.Position, Position ) < MaxUseDistance;
	}

	[ConVar.Replicated( "sv_max_use_distance" )]
	public static float MaxUseDistance { get; set; } = 100;
}
