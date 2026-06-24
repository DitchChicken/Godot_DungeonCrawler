using System.Collections.Generic;

public class DollSlotDef
{
	public string Slot { get; set; }
	public float X { get; set; }
	public float Y { get; set; }
	public float Width { get; set; }
	public float Height { get; set; }
}

public class EquipmentDollDef
{
	public string Id { get; set; }
	public string BaseImage { get; set; }
	public List<DollSlotDef> Slots { get; set; } = new List<DollSlotDef>();
}
