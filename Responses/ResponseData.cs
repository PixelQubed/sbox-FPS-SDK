using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Sandbox;

namespace Amper.Source1;

public partial class ResponseData<T, U> : GameResource where T : Enum where U : Enum
{

	public Dictionary<string, CriterionData> Criteria { get; set; }
	public List<Response> Responses { get; set; }

	public void Cringe()
	{
		
	}

	public struct CriterionData
	{
		public U Context { get; set; }
		public string Value { get; set; }
		public override string ToString()
		{
			return $"{Context} {Value}";
		}
	}

	public struct Response
	{
		public T Concept { get; set; }
		public List<string> Criteria { get; set; }
		[ResourceType( "sound" )] public string SoundEvent { get; set; }

		public override string ToString()
		{
			var name = $"{Concept}";

			if ( Criteria != null && Criteria.Count > 0 ) 
			{
				name += " while ";
				name += string.Join( ", ", Criteria );
			}

			return name;
		}
	}
}
