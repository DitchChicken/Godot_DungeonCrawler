using Godot;
using System.Text.Json;

public static class SceneLoader
{
	public static Scene Load(string sceneId)
	{
		string path = $"res://Data/Scenes/{sceneId}.json";
		var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PrintErr($"Could not load scene: {path}");
			return null;
		}

		string json = file.GetAsText();
		file.Close();

		try
		{
			return JsonSerializer.Deserialize<Scene>(json, JsonConfig.Options);
		}
		catch (JsonException ex)
		{
			GD.PrintErr($"JSON error in {path}\n  {ex.Message}");
			return null;
		}
	}
}
