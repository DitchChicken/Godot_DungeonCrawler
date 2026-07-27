using System.Text.Json;
using System.Text.Json.Serialization;

public static class JsonConfig
{
	// Shared options for all game-data deserialization
	public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true,
		Converters = { new JsonStringEnumConverter() }
	};
}
