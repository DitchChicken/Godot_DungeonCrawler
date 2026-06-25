using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public static class CharacterLoader
{
	public static List<Character> LoadRoster()
	{
		var characters = new List<Character>();
		
		// Get all json files in the Characters directory
		var dir = DirAccess.Open("res://Data/Characters/");
		if (dir == null)
		{
			GD.PrintErr("Could not open Characters directory");
			return characters;
		}

		dir.ListDirBegin();
		string fileName = dir.GetNext();
		
		while (fileName != "")
		{
			if (!dir.CurrentIsDir() && fileName.EndsWith(".json"))
			{
				var character = LoadCharacter($"res://Data/Characters/{fileName}");
				if (character != null)
					characters.Add(character);
			}
			fileName = dir.GetNext();
		}

		dir.ListDirEnd();
		return characters;
	}

	private static Character LoadCharacter(string path)
	{
		var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PrintErr($"Could not open character file: {path}");
			return null;
		}

		string json = file.GetAsText();
		file.Close();

		var options = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		};

		var def = JsonSerializer.Deserialize<CharacterDef>(json, options);
		
		var c = new Character
		{
			Name         = def.Name,
			Race         = Enum.Parse<Race>(def.Race),
			ClassType    = Enum.Parse<ClassType>(def.ClassType),
			Gender       = def.Gender,
			Age          = def.Age,
			Height       = def.Height,
			Weight       = def.Weight,
			Backstory    = def.Backstory,
			Strength     = def.Strength,
			Constitution = def.Constitution,
			Dexterity    = def.Dexterity,
			Intelligence = def.Intelligence,
			Wisdom       = def.Wisdom,
			Charisma     = def.Charisma,
			Portrait     = def.Portrait,
			BattleSprite = def.BattleSprite,
		};
		
		c.Initialize();
		
		// Load and equip starting equipment
		if (def.StartingEquipment != null)
		{
			foreach (var equipmentId in def.StartingEquipment)
			{
				var item = EquipmentLoader.LoadEquipment(equipmentId);
				if (item != null)
				{
					bool equipped = c.Equip(item);
					if (!equipped)
						GD.PrintErr($"{c.Name} could not equip {item.Name} — requirements not met");
				}
			}
		}

		return c;
	}
}

public class CharacterDef
{
	public string Name { get; set; }
	public string Race { get; set; }
	public string ClassType { get; set; }
	public string Gender { get; set; }
	public int Age { get; set; }
	public float Height { get; set; }
	public float Weight { get; set; }
	public string Backstory { get; set; }
	public int Strength { get; set; }
	public int Constitution { get; set; }
	public int Dexterity { get; set; }
	public int Intelligence { get; set; }
	public int Wisdom { get; set; }
	public int Charisma { get; set; }
	public List<string> StartingEquipment { get; set; }
	public string Portrait { get; set; }
	public string BattleSprite { get; set; }
}
