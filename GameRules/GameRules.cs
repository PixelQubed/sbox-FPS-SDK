using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Source1
{
	abstract partial class GameRules : Entity
	{
		public static GameRules Instance { get; set; }
		public virtual ViewVectors ViewVectors => default;

		public override void Spawn()
		{
			Instance = this;
		}
	}
}
