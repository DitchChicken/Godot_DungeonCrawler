using Godot;

public enum ItemDetailContext { None, Shop, Inventory }

public partial class ItemDetailPanel : PanelContainer
{
	private TextureRect _icon;
	private Label _nameLabel;
	private Label _typeLabel;
	private Label _statsLabel;
	private Label _descriptionLabel;
	private Label _priceLabel;

	private ItemDetailContext _context = ItemDetailContext.None;

	public override void _Ready()
	{
		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 12);
		margin.AddThemeConstantOverride("margin_right", 12);
		margin.AddThemeConstantOverride("margin_top", 8);
		margin.AddThemeConstantOverride("margin_bottom", 8);
		AddChild(margin);

		var hbox = new HBoxContainer();
		hbox.AddThemeConstantOverride("separation", 12);
		margin.AddChild(hbox);

		// Left — icon
		_icon = new TextureRect();
		_icon.CustomMinimumSize = new Vector2(96, 96);
		_icon.StretchMode       = TextureRect.StretchModeEnum.KeepAspectCentered;
		_icon.ExpandMode        = TextureRect.ExpandModeEnum.IgnoreSize;
		hbox.AddChild(_icon);

		// Right — text column
		var vbox = new VBoxContainer();
		vbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		vbox.AddThemeConstantOverride("separation", 4);
		hbox.AddChild(vbox);

		_nameLabel = new Label();
		_nameLabel.AddThemeFontSizeOverride("font_size", 20);
		vbox.AddChild(_nameLabel);

		_typeLabel = new Label();
		_typeLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
		vbox.AddChild(_typeLabel);

		_statsLabel = new Label();
		_statsLabel.AutowrapMode = TextServer.AutowrapMode.Word;
		vbox.AddChild(_statsLabel);

		_descriptionLabel = new Label();
		_descriptionLabel.AutowrapMode = TextServer.AutowrapMode.Word;
		_descriptionLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.7f));
		vbox.AddChild(_descriptionLabel);

		_priceLabel = new Label();
		_priceLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.85f, 0.0f));
		vbox.AddChild(_priceLabel);

		ClearItem();
	}

	public void SetContext(ItemDetailContext context) => _context = context;

	public void ShowItem(Equipment item)
	{
		if (item == null) { ClearItem(); return; }

		Visible = true;

		// Icon
		if (!string.IsNullOrEmpty(item.Icon) && ResourceLoader.Exists(item.Icon))
			_icon.Texture = GD.Load<Texture2D>(item.Icon);
		else
			_icon.Texture = null;

		// Name (respects identification)
		_nameLabel.Text = item.DisplayName;

		// Type line — slot / consumable type
		_typeLabel.Text = BuildTypeLine(item);

		// Stats — damage, armor, bonuses
		_statsLabel.Text = BuildStatsLine(item);

		_descriptionLabel.Text = item.Description ?? "";

		// Price — only in shop context
		if (_context == ItemDetailContext.Shop)
		{
			int buy  = ShopManager.GetBuyPrice(item);
			int sell = ShopManager.GetSellPrice(item);
			_priceLabel.Text    = $"Buy: {buy}g    Sell: {sell}g";
			_priceLabel.Visible = true;
		}
		else
		{
			_priceLabel.Visible = false;
		}
	}

	private string BuildTypeLine(Equipment item)
	{
		if (item.ConsumableType != null && item.ConsumableType != "None")
			return item.ConsumableType;
		if (item.Slot != EquipmentSlot.None)
			return item.Slot.ToString();
		return "Miscellaneous";
	}

	private string BuildStatsLine(Equipment item)
	{
		var parts = new System.Collections.Generic.List<string>();

		if (item.BaseDamageMax > 0)
			parts.Add($"Damage: {item.BaseDamageMin}-{item.BaseDamageMax}");
		if (item.ArmorClass != 0)
			parts.Add($"AC: {item.ArmorClass}");
		if (item.InitiativeModifier != 0)
			parts.Add($"Init: {item.InitiativeModifier:+0;-0}");
		if (item.Weight > 0)
			parts.Add($"Weight: {item.Weight}");

		// Stat bonuses
		if (item.BonusStrength != 0)     parts.Add($"STR {item.BonusStrength:+0;-0}");
		if (item.BonusConstitution != 0) parts.Add($"CON {item.BonusConstitution:+0;-0}");
		if (item.BonusDexterity != 0)    parts.Add($"DEX {item.BonusDexterity:+0;-0}");
		if (item.BonusIntelligence != 0) parts.Add($"INT {item.BonusIntelligence:+0;-0}");
		if (item.BonusWisdom != 0)       parts.Add($"WIS {item.BonusWisdom:+0;-0}");
		if (item.BonusCharisma != 0)     parts.Add($"CHA {item.BonusCharisma:+0;-0}");
		if (item.BonusHP != 0)           parts.Add($"HP {item.BonusHP:+0;-0}");
		if (item.BonusMana != 0)         parts.Add($"MP {item.BonusMana:+0;-0}");

		return string.Join("   ", parts);
	}

	public void ClearItem()
	{
		_icon.Texture         = null;
		_nameLabel.Text       = "";
		_typeLabel.Text       = "";
		_statsLabel.Text      = "";
		_descriptionLabel.Text = "";
		_priceLabel.Text      = "";
		_priceLabel.Visible   = false;
	}
}
