using Sandbox;
using System;

namespace Source1;

public partial class Source1ViewModel : BaseViewModel
{
	public override void PostCameraSetup( ref CameraSetup camSetup )
	{
		base.PostCameraSetup( ref camSetup );

		var visible = ShouldDraw();
		EnableDrawing = visible;

		if ( visible )
			CalculateView( camSetup );

	}

	public virtual bool ShouldDraw()
	{
		return true;
	}

	public virtual void CalculateView( CameraSetup camSetup )
	{

	}
}
