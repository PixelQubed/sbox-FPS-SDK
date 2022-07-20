using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Amper.Source1;

public partial class TalkerScript<T> : GameResource where T : Enum
{
	public T Cringe { get; set; }
}
