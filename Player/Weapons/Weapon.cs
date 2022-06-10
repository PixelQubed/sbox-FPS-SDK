using Sandbox;

namespace Amper.Source1;

public partial class Source1Weapon : AnimatedEntity
{
	public Source1Player Player => Owner as Source1Player;
	[Net] public int ViewModelIndex { get; set; }
	[Net] public int AmmoTypeNumber { get; set; }

	public override void Spawn()
	{
		base.Spawn();

		MoveType = MoveType.Physics;
		CollisionGroup = CollisionGroup.Interactive;
		PhysicsEnabled = true;
		UsePhysicsCollision = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;

		// Assume.
		ViewModelIndex = 0;
	}

	public virtual bool CanEquip( Source1Player player ) => true;

	public override void Simulate( Client cl )
	{
		SimulateReload();
		SimulateAttack();

		if ( sv_debug_weapons && IsLocalPawn )
			DebugScreenText( Time.Delta );
	}

	/// <summary>
	/// This simulates weapon's attack abilities.
	/// </summary>
	public virtual void SimulateAttack()
	{
		SimulatePrimaryAttack();
		SimulateSecondaryAttack();
	}

	public virtual bool ShouldAutoReload()
	{
		if ( !CanAttack() )
			return false;

		return Clip == 0 && NeedsAmmo();
	}


	/// <summary>
	/// The weapon has been picked up by someone
	/// </summary>
	public virtual void OnEquip( Source1Player owner )
	{
		if ( IsClient )
			return;

		SetParent( owner, true );
		Owner = owner;
		MoveType = MoveType.None;

		EnableAllCollisions = false;
		EnableDrawing = false;
	}

	/// <summary>
	/// The weapon has been dropped by the owner
	/// </summary>
	public virtual void OnDrop( Source1Player owner )
	{
		if ( IsClient )
			return;

		SetParent( null );
		Owner = null;
		MoveType = MoveType.Physics;

		EnableDrawing = true;
		EnableAllCollisions = true;
	}

	public virtual void OnDeploy( Source1Player owner )
	{
		EnableDrawing = true;
		NextAttackTime = Time.Now + GetDeployTime();

		SetupViewModel();
		SetupAnimParameters();
	}

	public virtual void SetupAnimParameters()
	{
		SendAnimParameter( "b_deploy" );
	}

	public virtual void OnHolster( Source1Player owner )
	{
		EnableDrawing = false;
		NextAttackTime = Time.Now;

		ClearViewModel();
	}

	public virtual void SetupViewModel()
	{
		GetViewModelEntity()?.SetWeaponModel( GetViewModelPath(), this );
	}

	public virtual void ClearViewModel()
	{
		GetViewModelEntity()?.ClearWeapon( this );
	}

	public virtual ViewModel GetViewModelEntity()
	{
		var player = Owner as Source1Player;
		return player?.GetViewModel( ViewModelIndex );
	}

	public virtual string GetViewModelPath() => "";

	public virtual void SendAnimParameter( string name, bool value = true )
	{
		SendPlayerAnimParameter( name, value );
		SendViewModelAnimParameter( name, value );
	}

	public virtual void SendPlayerAnimParameter( string name, bool value = true )
	{
		Player?.SetAnimParameter( name, value );
	}

	public virtual void SendViewModelAnimParameter( string name, bool value = true )
	{
		Player?.GetViewModel( ViewModelIndex )?.SetAnimParameter( name, value );
	}

	[Net, Predicted] public int Clip { get; set; }
	public virtual bool HasAmmo() => Clip > 0 || !NeedsAmmo();
	public virtual bool NeedsAmmo() => true;

	protected override void OnDestroy()
	{
		ClearViewModel();
		base.OnDestroy();
	}

	/// <summary>
	/// An anim tag has been fired from the viewmodel.
	/// </summary>
	public virtual void OnViewModelAnimGraphTag( string tag, AnimGraphTagEvent type ) { }

	/// <summary>
	/// An anim tag has been fired from the viewmodel.
	/// </summary>
	public virtual void OnPlayerAnimGraphTag( string tag, AnimGraphTagEvent type ) { }

	public virtual void OnViewModelAnimEventGeneric( string name, int intData, float floatData, Vector3 vectorData, string stringData ) { }
	public virtual void OnPlayerAnimEventGeneric( string name, int intData, float floatData, Vector3 vectorData, string stringData ) { }

	public virtual void RenderHud( Vector2 screenSize ) 
	{
		var center = screenSize * .5f;
		DrawCrosshair( screenSize, center );
	}

	public virtual void DrawCrosshair( Vector2 screenSize, Vector2 center ) { }

	protected virtual void DebugScreenText( float interval ) { }
	[ConVar.Replicated] public static bool sv_debug_weapons { get; set; }
}
