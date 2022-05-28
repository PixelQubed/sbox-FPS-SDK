using Sandbox;

namespace Amper.Source1;

partial class Source1Weapon
{
	[Net, Predicted] public float NextPrimaryAttack { get; set; } = -1;
	[Net, Predicted] public float NextSecondaryAttack { get; set; } = -1;

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

		NextPrimaryAttack = Time.Now;
		NextSecondaryAttack = Time.Now;

		SetupViewModel();
	}

	public virtual void OnHolster( Source1Player owner )
	{
	}

	public virtual void SetupViewModel()
	{
		var player = Owner as Source1Player;
		if ( player == null )
			return;

		var vm = player.GetViewModel();
		if ( vm == null )
			return;

		vm.SetWeaponModel( GetViewModelPath(), this );
	}

	public virtual string GetViewModelPath() => "";
}
