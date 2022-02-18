using Sandbox;

namespace Source1
{
	public abstract class Source1Event
	{
		public abstract class Round
		{
			/// <summary>
			/// Round restart
			/// </summary>
			public class RestartAttribute : EventAttribute
			{
				public RestartAttribute() : base( "Round_Restart" ) { }
			}

			/// <summary>
			/// Called when round is active, players can move
			/// </summary>
			public class ActiveAttribute : EventAttribute
			{
				public ActiveAttribute() : base( "Round_Active" ) { }
			}

			/// <summary>
			/// When a team wins a round
			/// </summary>
			public class WinAttribute : EventAttribute
			{
				public WinAttribute() : base( "Round_Win" ) { }
			}
		}

		public abstract class Game
		{
			public class StartAttribute : EventAttribute
			{
				public StartAttribute() : base( "Game_Start" ) { }
			}

			public class RestartAttribute : EventAttribute
			{
				public RestartAttribute() : base( "Game_Restart" ) { }
			}

			public class OverAttribute : EventAttribute
			{
				public OverAttribute() : base( "Game_Over" ) { }
			}
		}

		public abstract class Player
		{
			#region Hurt
			public class HurtAttribute : EventAttribute
			{
				public HurtAttribute() : base( "Player_Hurt" ) { }
			}

			public struct HurtArgs
			{
				public Source1Player Victim;
				public Entity Attacker;
				public Entity Inflictor;
				public Entity Assister;
				public Entity Weapon;
				public DamageFlags Flags;
				public Vector3 Position;
				public float Damage;

				public HurtArgs( Source1Player victim, Entity attacker, Entity inflictor, Entity assister, Entity weapon, DamageFlags flags, Vector3 position, float damage )
				{
					Victim = victim;
					Attacker = attacker;
					Inflictor = inflictor;
					Assister = assister;
					Weapon = weapon;
					Flags = flags;
					Position = position;
					Damage = damage;
				}
			}
			#endregion

			#region Death
			public class DeathAttribute : EventAttribute
			{
				public DeathAttribute() : base( "Player_Death" ) { }
			}

			public struct DeathArgs
			{
				public Source1Player Victim;
				public Entity Attacker;
				public Entity Inflictor;
				public Entity Assister;
				public Entity Weapon;
				public DamageFlags Flags;

				public DeathArgs( Source1Player victim, Entity attacker, Entity inflictor, Entity assister, Entity weapon, DamageFlags flags )
				{
					Victim = victim;
					Attacker = attacker;
					Inflictor = inflictor;
					Assister = assister;
					Weapon = weapon;
					Flags = flags;
				}
			}
			#endregion

			#region Spawn
			public class SpawnAttribute : EventAttribute
			{
				public SpawnAttribute() : base( "Player_Spawn" ) { }
			}

			public struct SpawnArgs
			{
				public Source1Player Player;

				public SpawnArgs( Source1Player player )
				{
					Player = player;
				}
			}
			#endregion

			#region Change Team
			public class ChangeTeamAttribute : EventAttribute
			{
				public ChangeTeamAttribute() : base( "Player_ChangeTeam" ) { }
			}

			public struct ChangeTeamArgs
			{
				public Source1Player Player;
				public int Team;

				public ChangeTeamArgs( Source1Player player, int team )
				{
					Player = player;
					Team = team;
				}

			}
			#endregion
		}
	}
}
