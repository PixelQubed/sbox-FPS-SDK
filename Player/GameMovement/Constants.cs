namespace Amper.Source1;

partial class GameMovement
{
	public virtual float AirSpeedCap => 30;
	public virtual float TimeToDuck => .2f;
	public virtual float TimeToUnduck => .2f;
	public virtual float JumpImpulse => 268.3281572999747f;

	//
	// Ducking
	//

	protected const int GAMEMOVEMENT_DUCK_TIME = 1000;
	protected int TIME_TO_DUCK_MSECS => (int)(TimeToDuck * 1000);
	protected int TIME_TO_UNDUCK_MSECS => (int)(TimeToUnduck * 1000);
	protected int GAMEMOVEMENT_TIME_TO_UNDUCK_MSECS => TIME_TO_UNDUCK_MSECS;
	protected int GAMEMOVEMENT_TIME_TO_UNDUCK_MSECS_INV => GAMEMOVEMENT_DUCK_TIME - GAMEMOVEMENT_TIME_TO_UNDUCK_MSECS;

	//
	// Jumping
	//

	public const int GAMEMOVEMENT_JUMP_TIME = 510;
	public const float WATERJUMP_HEIGHT = 8;
	public const float NON_JUMP_VELOCITY = 140;

	public const float PLAYER_MAX_SAFE_FALL_SPEED = 580;
	public const float PLAYER_MIN_BOUNCE_SPEED = 200;



	public float CHECK_LADDER_INTERVAL => .2f;
	public int CHECK_LADDER_TICK_INTERVAL => (int)(CHECK_LADDER_TICK_INTERVAL / Global.TickInterval);

	public const int MAX_CLIP_PLANES = 5;
	public const float DIST_EPSILON = 0.3125f;

	public virtual float GetCurrentGravity()
	{
		return sv_gravity * GameRules.Current.GetGravityMultiplier();
	}
}
