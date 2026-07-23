using System;

public static class LootContext
{
	// Whichever loot inventory is currently open for drag/drop
	public static Inventory Active { get; private set; }
	public static Action RefreshCallback { get; private set; }

	public static void Set(Inventory inventory, Action onRefresh)
	{
		Active          = inventory;
		RefreshCallback = onRefresh;
	}

	public static void Clear()
	{
		Active          = null;
		RefreshCallback = null;
	}

	public static void Refresh() => RefreshCallback?.Invoke();
}
