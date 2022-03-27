using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System.Linq;

namespace Source1;

[UseTemplate]
public class ColorFormattedString : Panel
{
	public ColorFormattedString Clear()
	{
		DeleteChildren( true );
		return this;
	}

	public ColorFormattedString AddText( string text )
	{
		Add.Label( text );
		return this;
	}

	public ColorFormattedString AddText( string text, Color color )
	{
		color = color.WithAlpha( 1 );

		var label = Add.Label( text );
		label.Style.FontColor = color;
		return this;
	}

	public ColorFormattedString AddText( string text, string color )
	{
		// add whitespaces
		var label = Add.Label( text );
		label.Style.Set( "color", color );
		return this;
	}
}
