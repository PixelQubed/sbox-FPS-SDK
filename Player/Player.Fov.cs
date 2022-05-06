using Sandbox;

namespace Source1;

partial class Source1Player
{
	public float ForcedFieldOfView { get; private set; }
	public Entity ForcedFieldOfViewRequester { get; private set; }
	public float ForcedFieldOfViewChangeTime { get; set; }
	public float? ForcedFieldOfViewStartWith { get; set; }

	[ClientRpc]
	public void SetFieldOfView( Entity requester, float fov, float speed = 0, float startWith = -1 )
	{
		if ( fov > 0 && (requester == null || !requester.IsValid) )
		{
			Log.Error( "SetFieldOfView - requester must be set to a valid entity." );
			return;
		}

		ForcedFieldOfView = fov;
		ForcedFieldOfViewChangeTime = speed;
		ForcedFieldOfViewRequester = requester;
		ForcedFieldOfViewStartWith = startWith > 0 ? startWith : null;
	}

	public void ResetFieldOfView( float speed = 0, float startWith = -1 )
	{
		SetFieldOfView( null, 0, speed, startWith );
	}

	/// <summary>
	/// If current field of view is requested by this entity, we will reset it.
	/// </summary>
	public void ResetFieldOfViewFromRequester( Entity requester, float speed = 0, float startWith = -1 )
	{
		if ( ForcedFieldOfViewRequester != requester )
			return;

		ResetFieldOfView( speed, startWith );
	}
}
