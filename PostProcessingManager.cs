using System;
using System.Collections.Generic;

namespace Amper.Source1;

public class PostProcessingManager
{
	private const string EnabledCombo = "D_ENABLED";

	private Dictionary<Type, BasePostProcess> Effects = new();
	private Dictionary<Type, bool> EnabledCached = new();

	public virtual void FrameSimulate() { }

	public T GetOrCreate<T>() where T : BasePostProcess, new()
	{
		var type = typeof( T );

		if ( Effects.TryGetValue( type, out var effect ) )
			return (T)effect;

		effect = new T();
		Effects.Add( type, effect );
		PostProcess.Add( effect );
		SetVisible( effect, false );
		return (T)effect;
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
}
