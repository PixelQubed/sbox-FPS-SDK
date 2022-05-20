using Sandbox;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Source1;

public static partial class Util
{
	public static string GetMapDisplayName( string map )
	{
		var mapname = map.Split( "." ).Last();

		// split words by "_"
		var words = mapname.Split( "_" );
		for ( var i = 0; i < words.Length; i++ )
		{
			var word = words[i];

			if ( word.Length == 0 )
				continue;

			// capitalize first letter in the word
			words[i] = string.Concat( word[0].ToString().ToUpper(), word.AsSpan( 1 ) );
		}

		// If there are at least two words in the map name, cut the first one
		if ( words.Length > 1 )
			words = words.Skip( 1 ).ToArray();

		// join all the words back separated by whitespace
		var name = string.Join( " ", words );
		return name;
	}

	public static string Compress( this string s )
	{
		var bytes = Encoding.Unicode.GetBytes( s );
		using ( var msi = new MemoryStream( bytes ) )
		using ( var mso = new MemoryStream() )
		{
			using ( var gs = new GZipStream( mso, CompressionMode.Compress ) )
			{
				msi.CopyTo( gs );
			}
			return Convert.ToBase64String( mso.ToArray() );
		}
	}

	public static string Decompress( this string s )
	{
		var bytes = Convert.FromBase64String( s );
		using ( var msi = new MemoryStream( bytes ) )
		using ( var mso = new MemoryStream() )
		{
			using ( var gs = new GZipStream( msi, CompressionMode.Decompress ) )
			{
				gs.CopyTo( mso );
			}
			return Encoding.Unicode.GetString( mso.ToArray() );
		}
	}

	public static float RemapClamped( this float value, float oldLow, float oldHigh, float newLow = 0, float newHigh = 1 )
	{
		value = value.Remap( oldLow, oldHigh, newLow, newHigh );

		if ( newLow < newHigh )
		{
			value = Math.Clamp( value, newLow, newHigh );
		}
		else
		{
			value = Math.Clamp( value, newHigh, newLow );
		}
		return value;
	}

	// hermite basis function for smooth interpolation
	// Similar to Gain() above, but very cheap to call
	// value should be between 0 & 1 inclusive
	public static float SimpleSpline( float value )
	{
		float valueSquared = value * value;

		// Nice little ease-in, ease-out spline-like curve
		return (3 * valueSquared - 2 * valueSquared * value);
	}

	public static string JPGToPNG( string jpg )
	{
		if ( !string.IsNullOrEmpty( jpg ) )
		{
			string noExtension = Path.Combine( Path.GetDirectoryName( jpg ), Path.GetFileNameWithoutExtension( jpg ) );
			return $"/{noExtension}.png";
		}
		return "";
	}
}
