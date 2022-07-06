using Sandbox;

namespace Amper.Source1;

partial class GameMovement
{
	protected virtual void ShowDebugOverlay()
	{
		if ( sv_debug_movement && Player.Client.IsListenServerHost && Host.IsServer )
		{
			DebugOverlay.ScreenText( CreateDebugString(), new Vector2( 60, 250 ) );
		}
	}

	protected virtual string CreateDebugString()
	{
		var str = 
			$"[PLAYER]\n" +
			$"GroundEntity          {Player.GroundEntity}\n" +
			$"MoveType              {Player.MoveType}\n" +
			$"\n" +

			$"[Move Data]\n" +
			$"ForwardMove           {Move.ForwardMove}\n" +
			$"SideMove              {Move.SideMove}\n" +
			$"UpMove                {Move.UpMove}\n" +
			$"Velocity              {Move.Velocity}\n" +
			$"\n" +

			$"[DUCK]\n" +
			$"m_bDucking            {Player.m_bDucking}\n" +
			$"m_bDucked             {Player.m_bDucked}\n" +
			$"m_nDuckTimeMsecs      {Player.m_nDuckTimeMsecs}\n" +
			$"m_nDuckJumpTimeMsecs  {Player.m_nDuckJumpTimeMsecs}\n" +
			$"m_bInDuckJump         {Player.m_bInDuckJump}\n" +
			$"\n" +

			$"[WATER]\n" +
			$"Water Level           {Player.WaterLevelType}\n" +
			$"m_flWaterJumpTime     {Player.m_flWaterJumpTime}";

		return str;
	}

	[ConVar.Replicated] public static bool sv_debug_movement { get; set; }
}
