using Sandbox;

namespace Amper.Source1;

partial class Source1Player
{
	public Entity HoveredEntity { get; private set; }
	public Entity Using { get; protected set; }

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

	protected virtual void StopUsing()
	{
		Using = null;
	}

	protected virtual void UseFail()
	{
		PlaySound( "player_use_fail" );
	}

	public bool CanUse( Entity entity )
	{
		if ( entity is not IUse use )
			return false;

		if ( !use.IsUsable( this ) )
			return false;

		return Vector3.DistanceBetween( entity.Position, Position ) < sv_max_use_distance;
	}

	[ConVar.Replicated] public static float sv_max_use_distance { get; set; } = 100;
}
