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
		//
		// Reload
		//

		// Player has requested a reload.
		if ( WishReload() || ShouldAutoReload() )
			Reload();

		// We're in the process of reloading.
		if ( IsReloading ) 
			SimulateReload();

		//
		// Attacks
		//

		if ( WishPrimaryAttack() )
			SimulatePrimaryAttack();
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
		SendAnimParameter( "b_deploy" );
	}

	public virtual void OnHolster( Source1Player owner )
	{
		NextAttackTime = Time.Now;

		ClearViewModel();
	}

	public virtual void SetupViewModel()
	{
		GetViewModelEntity()?.SetWeaponModel( GetViewModelPath(), this );
	}

	public virtual void ClearViewModel()
	{
		GetViewModelEntity()?.ClearWeapon();
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

	public virtual AnimatedEntity GetEffectEntity()
	{
		return IsLocalPawn && IsFirstPersonMode
			? Player?.GetViewModel( ViewModelIndex )
			: this;
	}

	protected override void OnDestroy()
	{
		ClearViewModel();
		base.OnDestroy();
	}

	/// <summary>
	/// An anim tag has been fired from the viewmodel.
	/// </summary>
	public virtual void OnViewModelAnimGraphTag( string tag, AnimGraphTagEvent type )
	{
	}

	/// <summary>
	/// An anim tag has been fired from the viewmodel.
	/// </summary>
	public virtual void OnPlayerAnimGraphTag( string tag, AnimGraphTagEvent type )
	{
	}

	public virtual void OnViewModelAnimEventGeneric( string name, int intData, float floatData, Vector3 vectorData, string stringData ) { }
	public virtual void OnPlayerAnimEventGeneric( string name, int intData, float floatData, Vector3 vectorData, string stringData ) { }
}
