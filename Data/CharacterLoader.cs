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
				if (character != null) {
					characters.Add(character);
					foreach (var domain in character.Domains.Keys)
						DomainRegistry.Validate(domain, $"character '{character.Name}'");
				}
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
			SpriteTopOffset   = def.SpriteTopOffset,
			SpriteRightOffset = def.SpriteRightOffset,
			KnownAbilities    = def.KnownAbilities ?? new List<string>(),
			Domains = def.Domains ?? new Dictionary<string, int>(),
		};
		
		c.Initialize();
		
		if (def.StartingEquipment != null)
		{
			foreach (var entry in def.StartingEquipment)
			{
				var item = EquipmentLoader.LoadEquipment(entry.Id);
				if (item == null) continue;

				if (entry.Count > 1 || item.IsStackable)
				{
					// Clone the item and set stack count directly
					item.StackCount = entry.Count;
					c.PersonalInventory.Items.Add(item);  // add directly as one stack
				}
				else
				{
					// Single non-stackable items get equipped
					bool equipped = c.Equip(item);
					if (!equipped)
						GD.PrintErr($"{c.Name} could not equip {item.Name}");
				}
			}
		}

		return c;
	}
}

public class StartingEquipmentEntry
{
	public string Id { get; set; }
	public int Count { get; set; } = 1;
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
	public List<StartingEquipmentEntry> StartingEquipment { get; set; }
	public string Portrait { get; set; }
	public string BattleSprite { get; set; }
	public float SpriteTopOffset { get; set; } = 0f;
	public float SpriteRightOffset { get; set; } = 0f;
	public List<string> KnownAbilities { get; set; } = new List<string>();
	public Dictionary<string, int> Domains { get; set; } = new Dictionary<string, int>();
}
