using System.Collections.Generic;

public class ShopData
{
	public string Id { get; set; }
	public string Name { get; set; }
	public List<ShopStockEntry> Stock { get; set; } = new List<ShopStockEntry>();
}

public class ShopStockEntry
{
	public string Id { get; set; }
	public int Price { get; set; } = 0;      // 0 = use item's goldCost
	public int Quantity { get; set; } = -1;  // -1 = unlimited
}
