using Sandbox;
using System.Linq;

namespace Amper.Source1;

public partial class ViewModel : BaseViewModel
{
	public Source1Weapon Weapon { get; set; }

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
		ClearWeapon( weapon );

		SetModel( viewmodel );
		Weapon = weapon;
	}

	public override void OnNewModel( Model model )
	{
		ClearAttachments();

		if ( Model != null )
			SetupAttachments();
	}

	public virtual void ClearWeapon( Source1Weapon weapon )
	{
		if ( Weapon != weapon )
			return;

		ClearAttachments();

		Model = null;
		Weapon = null;
	}

	public virtual void SetupAttachments() { }

	public ModelEntity CreateAttachment<T>( string model = "" ) where T : ModelEntity, new()
	{
		var attach = new T { Owner = Owner, EnableViewmodelRendering = true };
		attach.SetParent( this, true );
		attach.SetModel( model );
		return attach;
	}

	public ModelEntity CreateAttachment( string model = "" )
	{
		return CreateAttachment<ModelEntity>( model );
	}

	public virtual void ClearAttachments()
	{
		foreach ( var attach in Children.Where( x => x.IsAuthority ).ToArray() ) 
			attach.Delete();
	}

	protected override void OnAnimGraphTag( string tag, AnimGraphTagEvent fireMode )
	{
		base.OnAnimGraphTag( tag, fireMode );
		Weapon?.OnViewModelAnimGraphTag( tag, fireMode );
	}

	public override void OnAnimEventGeneric( string name, int intData, float floatData, Vector3 vectorData, string stringData )
	{
		base.OnAnimEventGeneric( name, intData, floatData, vectorData, stringData );
		Weapon?.OnViewModelAnimEventGeneric( name, intData, floatData, vectorData, stringData );
	}
}
