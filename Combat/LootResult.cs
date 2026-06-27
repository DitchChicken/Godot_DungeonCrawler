using System.Collections.Generic;

public class LootResult
{
	public int ExperiencePerSurvivor { get; set; } = 0;
	public List<(Equipment item, int count)> Items { get; set; } 
		= new List<(Equipment item, int count)>();

	public void AddItem(Equipment item, int count)
	{
		// Merge stackable items
		if (item.IsStackable)
		{
			for (int i = 0; i < Items.Count; i++)
			{
				if (Items[i].item.Id == item.Id)
				{
					Items[i] = (Items[i].item, Items[i].count + count);
					return;
				}
			}
		}
		Items.Add((item, count));
	}
}
