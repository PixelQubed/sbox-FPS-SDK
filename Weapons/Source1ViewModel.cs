using Sandbox;

namespace Amper.Source1;

public partial class Source1ViewModel : BaseViewModel
{
	[Net] public Source1Weapon Weapon { get; set; }

	public override void Spawn()
	{
		base.Spawn();
		EnableViewmodelRendering = true;
	}

	public override void PostCameraSetup( ref CameraSetup camSetup )
	{
		base.PostCameraSetup( ref camSetup );

		var visible = ShouldDraw();
		EnableDrawing = visible;

		if ( visible )
			CalculateView( camSetup );

	}

	public virtual bool ShouldDraw() => true;
	public virtual void CalculateView( CameraSetup camSetup ) { }

	public virtual void SetWeaponModel( string viewmodel, Source1Weapon weapon )
	{
		if ( !IsClient ) 
			return;

		Weapon = weapon;
		SetModel( viewmodel );
	}
}
