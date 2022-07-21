using System;
using System.Collections.Generic;

namespace Amper.Source1;

public interface IResponseSpeaker<Concepts, Contexts> where Concepts : Enum where Contexts : Enum
{
	public ResponseController<Concepts, Contexts> ResponseController { get; set; }
}

public class ResponseController<Concepts, Contexts> where Concepts : Enum where Contexts : Enum
{
	List<ResponseData<Concepts, Contexts>> LoadedData { get; set; } = new();
	Dictionary<string, Criterion> CriterionDictionary { get; set; } = new();

	public void Load( ResponseData<Concepts, Contexts> data )
	{
		if ( !data.IsValid() ) 
			return;

		Reset();
		ParseLoadData( data );
	}

	void ParseLoadData( ResponseData<Concepts, Contexts> data )
	{
		if ( LoadedData.Contains( data ) )
		{
			Log.Info( $"<Responses> Warning: Attempt to load \"{data.ResourcePath}\" will cause recursion. Haulting..." );
			return;
		}

		// Try loading prefab first.
		if ( !string.IsNullOrEmpty( data.Base ) )
		{
			if ( ResourceLibrary.TryGet<ResponseData<Concepts, Contexts>>( data.Base, out var responseData ) ) 
				ParseLoadData( responseData );
		}

		ParseCriteriaFromData( data.Criteria );
		LoadedData.Add( data );
	}

	public void Reset()
	{
		LoadedData.Clear();
		CriterionDictionary.Clear();
	}

	private readonly Dictionary<string, CompareSign> SignStrings = new()
	{
		{"=",   CompareSign.Equal },
		{"!=",  CompareSign.NotEqual },
		{"<",   CompareSign.Less },
		{">",   CompareSign.Greater },
		{"<=",	CompareSign.LessOrEqual },
		{">=",	CompareSign.GreaterOrEqual }
	};

	void ParseCriteriaFromData( Dictionary<string, ResponseData<Concepts, Contexts>.Criterion> dictionary )
	{
		foreach ( var pair in dictionary )
		{
			var criterionData = pair.Value;

			var name = pair.Key;
			var context = criterionData.Context;
			var rawValue = criterionData.Value;

			// Figure out what type of comparison we're providing in the asset.

			var sign = CompareSign.Equal;
			var substrCount = 0;

			foreach ( var signPair in SignStrings )
			{
				if ( rawValue.StartsWith( signPair.Key ) )
				{
					sign = signPair.Value;
					substrCount = signPair.Key.Length;
					break;
				}
			}

			if ( substrCount > 0 )
				rawValue = rawValue.Substring( substrCount );

			Log.Info( $"<Responses> Parsed Criterion \"{name}\" (\"{context}\" {sign} {rawValue})" );
		}
	}

	struct Criterion
	{
		public string Name { get; set; }
		public Contexts Context { get; set; }
		public CompareSign Sign { get; set; }
		public string Value { get; set; }
	}

	enum CompareSign
	{
		Equal,
		NotEqual,
		Less,
		Greater,
		LessOrEqual,
		GreaterOrEqual
	}
}
