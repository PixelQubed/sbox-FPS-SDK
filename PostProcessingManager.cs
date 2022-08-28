using System;
using System.Collections.Generic;
using Sandbox;

namespace Amper.FPS;

public class PostProcessingManager
{
	private const string EnabledCombo = "D_ENABLED";

	Dictionary<Type, BasePostProcess> Effects = new();
	Dictionary<Type, bool> EnabledCached = new();
	Dictionary<Type, bool> ForceEnabled = new();

	public void FrameSimulate()
	{
		Update();

		foreach ( var pair in ForceEnabled )
		{
			var type = pair.Key;
			var enabled = pair.Value;

			SetVisible( type, enabled );
		}
	}

	public virtual void Update() { }

	public T GetOrCreate<T>() where T : BasePostProcess, new()
	{
		var type = typeof( T );
		return (T)GetOrCreate( type );
	}

	public BasePostProcess GetOrCreate( Type type )
	{
		if ( Effects.TryGetValue( type, out var effect ) )
			return effect;

		effect = TypeLibrary.Create<BasePostProcess>(type); 
		Effects.Add( type, effect );
		PostProcess.Add( effect );
		SetVisible( effect, false );
		return effect;
	}

	public bool IsVisible<T>() where T : BasePostProcess
	{
		var type = typeof( T );

		EnabledCached.TryGetValue( type, out var enabled );
		return enabled;
	}

	public void SetVisible<T>( bool visible ) where T : BasePostProcess, new()
	{
		if ( !Host.IsClient )
			return;

		var cachedVisible = IsVisible<T>();
		if ( visible == cachedVisible )
			return;

		var effect = GetOrCreate<T>();
		SetVisible( effect, visible );
	}

	public void SetVisible( BasePostProcess effect, bool visible )
	{
		if ( effect == null )
			return;

		effect.Attributes.SetCombo( EnabledCombo, visible );

		var type = effect.GetType();
		EnabledCached[type] = visible;
	}

	public void SetVisible( Type type, bool visible )
	{
		var effect = GetOrCreate( type );
		if ( effect == null )
			return;

		SetVisible( effect, visible );
	}

	public void SetForced( string name, bool enabled )
	{
		var type = TypeLibrary.GetTypeByName( name );
		ForceEnabled[type] = enabled;
	}

	[ConCmd.Client( "r_postprocess_force" )]
	public static void Command_ForcePostProcessing( string name, bool enabled )
	{
		var manager = GameRules.Current.PostProcessingManager;
		if ( manager == null )
			return;

		try
		{
			manager.SetForced( name, enabled );
			Log.Info( $"Changed \"{name}\" effect visibility: {enabled}." );
		} catch
		{
			Log.Info( $"Failed to change visibility of \"{name}\" effect." );
		}
	}
}
