using Godot;
using System.Collections.Generic;

public partial class Shop : Control
{
	private GameState _gameState;
	private ShopState _shop;

	private GridContainer _shopGrid;
	private GridContainer _rightGrid;   // vault or character inventory
	private Label _goldLabel;
	private ItemDetailPanel _detail;

	private const string ShopId = "GeneralStore";

	public bool FromShop { get; set; } = false;
	
	private void OnShopMessage(string msg) => ShowFloatingMessage(msg);
	private void OnTransactionCompleted()  => RefreshAll();

	private const int ShopGridSlots = 48;   // 6 cols × 8 rows, adjust to taste
	private const int RightGridSlots = 48;

	public override void _Ready()
	{
		_gameState = GetNode<GameState>("/root/GameState");
		_shopGrid  = GetNode<GridContainer>("ShopGrid");
		_rightGrid = GetNode<GridContainer>("RightGrid");
		_goldLabel = GetNode<Label>("GoldLabel");

		GetNode<Button>("BackButton").Pressed += OnBackPressed;

		// Build the reusable detail panel into DetailPanel
		var detailContainer = GetNode<PanelContainer>("DetailPanel");
		_detail = new ItemDetailPanel();
		_detail.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_detail.SizeFlagsVertical   = SizeFlags.ExpandFill;
		detailContainer.AddChild(_detail);
		_detail.SetContext(ItemDetailContext.Shop);
		_detail.ClearItem();

		ShopSignals.MessageRaised        += OnShopMessage;
		ShopSignals.TransactionCompleted += OnTransactionCompleted;
	
		// Load or resume shop state
		if (_gameState.CurrentShop != null && _gameState.CurrentShop.Id == ShopId)
			_shop = _gameState.CurrentShop;
		else
		{
			_shop = ShopManager.LoadShop(ShopId);
			_gameState.CurrentShop = _shop;
		}

		GD.Print($"Shop loaded: {_shop?.Name}, base stock: {_shop?.BaseStock.Count}");
		GD.Print($"Vault items: {_gameState.PartyVault.Items.Count}");

		RefreshAll();
	}

	public override void _ExitTree()
	{
		ShopSignals.MessageRaised        -= OnShopMessage;
		ShopSignals.TransactionCompleted -= OnTransactionCompleted;
	}

	public void RefreshAll()
	{
		RefreshGold();
		RefreshShopGrid();
		RefreshRightGrid();
	}

	private void RefreshGold()
	{
		_goldLabel.Text = $"Gold: {_gameState.Gold}";
	}

	private void RefreshShopGrid()
	{
		foreach (Node c in _shopGrid.GetChildren()) c.QueueFree();

		var stock = _shop.GetDisplayStock();
		for (int i = 0; i < ShopGridSlots; i++)
		{
			if (i < stock.Count)
				_shopGrid.AddChild(MakeSlot(stock[i], isShop: true));
			else
				_shopGrid.AddChild(MakeEmptySlot(isShop: true));
		}
	}

	private void RefreshRightGrid()
	{
		foreach (Node c in _rightGrid.GetChildren()) c.QueueFree();

		var vault = _gameState.PartyVault;
		for (int i = 0; i < RightGridSlots; i++)
		{
			if (i < vault.Items.Count)
				_rightGrid.AddChild(MakeSlot(vault.Items[i], isShop: false));
			else
				_rightGrid.AddChild(MakeEmptySlot(isShop: false));
		}
	}

	private InventorySlotButton MakeSlot(Equipment item, bool isShop)
	{
		var slot = new InventorySlotButton();
		slot.Item       = item;
		slot.SourceType = isShop
			? InventoryDragData.SourceType.Shop
			: InventoryDragData.SourceType.Vault;
		slot.CustomMinimumSize = new Vector2(64, 64);
		slot.CustomMaximumSize = new Vector2(64, 64);
		slot.ExpandIcon    = true;
		slot.IconAlignment = HorizontalAlignment.Center;
		slot.MouseFilter   = Control.MouseFilterEnum.Stop;
		slot.IsShopSlot    = isShop;

		if (!string.IsNullOrEmpty(item.Icon) && ResourceLoader.Exists(item.Icon))
			slot.Icon = GD.Load<Texture2D>(item.Icon);

		var capturedItem = item;
		slot.MouseEntered += () => _detail.ShowItem(capturedItem);

		return slot;
	}

	private void OnBackPressed()
	{
		GetNode<Main>("/root/Main").CallDeferred(
			nameof(Main.SwitchScene), "res://Town/Town.tscn");
	}

	// Floating "Insufficient Gold" style message
	public void ShowFloatingMessage(string text)
	{
		var label = new Label();
		label.Text = text;
		label.AddThemeFontSizeOverride("font_size", 28);
		label.AddThemeColorOverride("font_color", new Color(1, 0.3f, 0.3f));
		label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0));
		label.LayoutMode   = 1;
		label.AnchorLeft   = 0.4f;
		label.AnchorRight  = 0.6f;
		label.AnchorTop    = 0.45f;
		label.AnchorBottom = 0.5f;
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.ZIndex = 100;
		AddChild(label);

		var tween = CreateTween();
		tween.TweenInterval(2.5f);
		tween.TweenProperty(label, "modulate:a", 0.0f, 0.5f);
		tween.TweenCallback(Callable.From(() => label.QueueFree()));
	}
	
	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		return data.Obj is InventoryDragData;
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		if (data.Obj is not InventoryDragData drag) return;

		// Which grid did we drop over?
		bool overShop = _shopGrid.GetGlobalRect().HasPoint(GetGlobalMousePosition());
		bool overRight = _rightGrid.GetGlobalRect().HasPoint(GetGlobalMousePosition());

		if (drag.FromShop && overRight)
		{
			// BUY — shop → vault/character
			DoBuy(drag);
		}
		else if (!drag.FromShop && overShop)
		{
			// SELL — vault/character → shop
			DoSell(drag);
		}
		// dropping shop→shop or vault→vault does nothing here
	}

	private void DoBuy(InventoryDragData drag)
	{
		var item = drag.Item;
		var target = _gameState.PartyVault; // buy to vault for now; character next
		bool ok = ShopManager.Buy(_shop, item, _gameState, target, out string msg);
		if (!ok)
			ShowFloatingMessage(msg);
		RefreshAll();
	}

	private void DoSell(InventoryDragData drag)
	{
		var item = drag.Item;
		int count = drag.Count;
		var source = _gameState.PartyVault; // selling from vault for now
		ShopManager.Sell(_shop, item, count, _gameState, source, out string msg);
		RefreshAll();
	}
	
	private InventorySlotButton MakeEmptySlot(bool isShop)
	{
		var slot = new InventorySlotButton();
		slot.Item       = null;
		slot.SourceType = isShop
			? InventoryDragData.SourceType.Shop
			: InventoryDragData.SourceType.Vault;
		slot.IsShopSlot        = isShop;
		slot.CustomMinimumSize = new Vector2(64, 64);
		slot.MouseFilter       = Control.MouseFilterEnum.Stop;
		return slot;
	}
}
