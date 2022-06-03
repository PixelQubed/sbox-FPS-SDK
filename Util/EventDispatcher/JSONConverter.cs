using Sandbox;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Amper.Source1;

public class EntityJSONConverter : JsonConverter<Entity>
{
	public override Entity Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options ) => Entity.FindByIndex( reader.GetInt32() );
	public override void Write( Utf8JsonWriter writer, Entity entity, JsonSerializerOptions options ) => writer.WriteNumberValue( entity.NetworkIdent );

	public override bool CanConvert( Type typeToConvert )
	{
		// Allow conversion of Entity and subclasses of Entity.
		return typeToConvert.IsSubclassOf( typeof( Entity ) ) || base.CanConvert( typeToConvert );
	}
}

public class ClientJSONConverter : JsonConverter<Client>
{
	public override Client Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
	{
		var netId = reader.GetInt32();
		return Client.All.FirstOrDefault( x => x.NetworkIdent == netId );
	}

	public override void Write( Utf8JsonWriter writer, Client client, JsonSerializerOptions options ) => writer.WriteNumberValue( client.NetworkIdent );
}

public class AssetJSONConverter : JsonConverter<GameResource>
{
	public override GameResource Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options ) => ResourceLibrary.Get<GameResource>( reader.GetString() );
	public override void Write( Utf8JsonWriter writer, GameResource asset, JsonSerializerOptions options ) => writer.WriteStringValue( asset.Path );

	public override bool CanConvert( Type typeToConvert )
	{
		// Allow conversion of Entity and GameResource of Entity.
		return typeToConvert.IsSubclassOf( typeof( GameResource ) ) || base.CanConvert( typeToConvert );
	}
}
