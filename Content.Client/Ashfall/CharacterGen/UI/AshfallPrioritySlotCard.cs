using System.Numerics;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Content.Shared.Roles;
using Robust.Shared.Timing;

namespace Content.Client.Ashfall.CharacterGen.UI;

/// <summary>
///     One compact priority slot (1..5) under the candidate list. Shows the pinned person's
///     first name, miniature portrait and the job fixed at pin time. Clicking the slot pins
///     the currently drafted candidate + job; the small corner cross clears it.
/// </summary>
public sealed partial class AshfallPrioritySlotCard : PanelContainer
{
    // Same recessed-screen language as the candidate cards.
    private static readonly StyleBoxFlat EmptyBox = new()
    {
        BackgroundColor = Color.FromHex("#161513"),
        BorderColor = Color.FromHex("#5A3D28"),
        BorderThickness = new Thickness(1),
    };

    private static readonly StyleBoxFlat PinnedBox = new()
    {
        BackgroundColor = Color.FromHex("#241E17"),
        BorderColor = Color.FromHex("#8A592D"),
        BorderThickness = new Thickness(1),
    };

    // Temporary "stamp" look flashed on the frame the slot receives a candidate.
    private static readonly StyleBoxFlat PinnedFlashBox = new()
    {
        BackgroundColor = Color.FromHex("#241E17"),
        BorderColor = Color.FromHex("#D48944"),
        BorderThickness = new Thickness(2),
    };

    // Waiting for the role pick: same warm highlight as an inspected candidate card.
    private static readonly StyleBoxFlat PendingBox = new()
    {
        BackgroundColor = Color.FromHex("#241E17"),
        BorderColor = Color.FromHex("#D48944"),
        BorderThickness = new Thickness(1),
    };

    // Occupied slot whose pinned dossier is currently open.
    private static readonly StyleBoxFlat InspectedBox = new()
    {
        BackgroundColor = Color.FromHex("#2B2217"),
        BorderColor = Color.FromHex("#D48944"),
        BorderThickness = new Thickness(1),
    };

    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IResourceCache _resCache = default!;

    private readonly Label _numberLabel;
    private readonly Button _clearButton;
    private readonly Button _moveLeftButton;
    private readonly Button _moveRightButton;
    private readonly ProfilePortraitSpriteView _preview;
    private readonly TextureRect _noise;
    private readonly Label _nameLabel;
    private readonly Label _jobLabel;
    private bool _pinned;
    private bool _pending;
    private bool _inspected;

    // Press-flash state: true once the card has received its first (populate) SetPin.
    private bool _settled;
    private float _pressProgress;
    private const float PressFlashDuration = 0.3f;

    /// <summary>Silently empty when no portrait frames are available, like the dossier overlay.</summary>
    public Texture? NoiseTexture
    {
        get => _noise.Texture;
        set => _noise.Texture = value;
    }

    public int SlotIndex { get; }
    public event Action<int>? SlotPressed;
    public event Action<int>? ClearPressed;

    /// <summary>Fires on a real empty-to-pinned transition (never on the initial populate).</summary>
    public event Action<int>? PinConfirmed;

    /// <summary>Fires with (slotIndex, -1|+1) when the user asks to move the pin.</summary>
    public event Action<int, int>? SlotMovePressed;

    public AshfallPrioritySlotCard(int slotIndex)
    {
        SlotIndex = slotIndex;
        IoCManager.InjectDependencies(this);
        // Height-locked twice over: Min/Max clamp the measure and SetHeight pins the final
        // arranged height, so the card is identical whether empty or pinned, always.
        MinSize = new Vector2(96, 185);
        MaxSize = new Vector2(float.MaxValue, 185);
        SetHeight = 185f;
        HorizontalExpand = true;
        PanelOverride = EmptyBox;
        ToolTip = Loc.GetString("ashfall-personal-files-slot-help");

        var button = new Button
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            StyleClasses = { "ButtonOpenBoth" },
        };
        button.OnPressed += _ => SlotPressed?.Invoke(SlotIndex);
        AddChild(button);

        var layout = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            Margin = new Thickness(4, 3),
            SeparationOverride = 3,
        };
        button.AddChild(layout);

        var header = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
        };
        _numberLabel = new Label
        {
            Text = (slotIndex + 1).ToString(),
            StyleClasses = { "FancyWindowTitle" },
            FontColorOverride = Color.FromHex("#D48944"),
            // Reserves the row height for the clear button, which only appears when pinned.
            MinSize = new Vector2(0, 20),
            HorizontalExpand = true,
        };
        _clearButton = new Button
        {
            Text = "×",
            Visible = false,
            // Pinned size: an unpinned Measure would differ from the pinned one otherwise.
            SetSize = new Vector2(20, 20),
            StyleClasses = { "AshfallUtilityAction" },
        };
        _clearButton.OnPressed += _ => ClearPressed?.Invoke(SlotIndex);
        header.AddChild(_numberLabel);
        header.AddChild(_clearButton);
        layout.AddChild(header);

        var portraitFrame = new PanelContainer
        {
            // Height-clamped from both sides so a loaded portrait can never change the measure.
            MinSize = new Vector2(0, 70),
            MaxSize = new Vector2(float.MaxValue, 70),
            HorizontalExpand = true,
            PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#121009") },
        };
        var clip = new LayoutContainer { RectClipContent = true };
        // SetSize pins the measured size: SpriteView reports the sprite bounds as its desired
        // size, and LayoutContainer inherits child measure, so an unpinned preview inflates
        // the portrait frame (the dossier pins its preview the same way).
        _preview = new ProfilePortraitSpriteView
        {
            MinSize = new Vector2(52, 62),
            SetSize = new Vector2(52, 62),
            PortraitFraction = 0.52f,
            PortraitScale = 0.75f,
            // Dossier framing scaled down to the miniature frame height.
            VerticalOffset = 5f,
        };
        _noise = new TextureRect
        {
            Stretch = TextureRect.StretchMode.Tile,
            MouseFilter = MouseFilterMode.Ignore,
            MinSize = new Vector2(52, 62),
            SetSize = new Vector2(52, 62),
            ModulateSelfOverride = new Color(255, 255, 255, 140),
        };
        clip.AddChild(_preview);
        clip.AddChild(_noise);
        LayoutContainer.SetAnchorPreset(_preview, LayoutContainer.LayoutPreset.Wide);
        LayoutContainer.SetAnchorPreset(_noise, LayoutContainer.LayoutPreset.Wide);
        portraitFrame.AddChild(clip);
        layout.AddChild(portraitFrame);

        _nameLabel = new Label
        {
            Text = Loc.GetString("ashfall-personal-files-slot-empty"),
            FontColorOverride = Color.FromHex("#6B6E6B"),
            // Tahoma ships no bold face in the repo; NotoSansDisplay Bold covers Cyrillic.
            FontOverride = _resCache.GetFont("/Fonts/NotoSansDisplay/NotoSansDisplay-Bold.ttf", 10),
            HorizontalExpand = true,
            ClipText = true,
            Margin = new Thickness(4, 0),
            // Height-clamped both ways: empty and pinned states must measure identically.
            MinSize = new Vector2(0, 15),
            MaxSize = new Vector2(float.MaxValue, 15),
        };
        layout.AddChild(_nameLabel);

        _jobLabel = new Label
        {
            FontColorOverride = Color.FromHex("#878C87"),
            FontOverride = _resCache.GetFont("/Fonts/NotoSansDisplay/NotoSansDisplay-Italic.ttf", 10),
            HorizontalExpand = true,
            ClipText = true,
            Margin = new Thickness(4, 0),
            MinSize = new Vector2(0, 15),
            MaxSize = new Vector2(float.MaxValue, 15),
        };
        layout.AddChild(_jobLabel);

        // Reorder arrows, shown only while the slot holds a pinned candidate.
        var moveRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalAlignment = HAlignment.Center,
            SeparationOverride = 8,
            MinSize = new Vector2(0, 18),
        };
        _moveLeftButton = new Button
        {
            Text = "‹",
            Visible = false,
            SetSize = new Vector2(28, 18),
            StyleClasses = { "AshfallUtilityAction" },
            ToolTip = Loc.GetString("ashfall-personal-files-slot-move-left"),
        };
        _moveLeftButton.OnPressed += _ => SlotMovePressed?.Invoke(SlotIndex, -1);
        _moveRightButton = new Button
        {
            Text = "›",
            Visible = false,
            SetSize = new Vector2(28, 18),
            StyleClasses = { "AshfallUtilityAction" },
            ToolTip = Loc.GetString("ashfall-personal-files-slot-move-right"),
        };
        _moveRightButton.OnPressed += _ => SlotMovePressed?.Invoke(SlotIndex, 1);
        moveRow.AddChild(_moveLeftButton);
        moveRow.AddChild(_moveRightButton);
        layout.AddChild(moveRow);
    }

    public void SetPin(AshfallClientPinnedSlot? pin)
    {
        var wasPinned = _pinned;

        if (pin == null)
        {
            _pinned = false;
            ApplyPanel();
            UpdateMoveArrows();
            _clearButton.Visible = false;
            _nameLabel.Text = Loc.GetString("ashfall-personal-files-slot-empty");
            _nameLabel.FontColorOverride = Color.FromHex("#6B6E6B");
            _jobLabel.Text = string.Empty;
            _preview.ClearPreview();
            _settled = true;
            return;
        }

        _pinned = true;
        _pending = false;
        ApplyPanel();
        UpdateMoveArrows();
        _clearButton.Visible = true;
        _clearButton.ToolTip = Loc.GetString("ashfall-personal-files-slot-clear");
        _nameLabel.Text = GetFirstName(pin.Candidate.Profile.Name);
        _nameLabel.FontColorOverride = Color.FromHex("#E8DFD0");
        var jobProto = _prototypes.TryIndex(pin.Job, out JobPrototype? jp) ? jp : null;
        _jobLabel.Text = jobProto?.LocalizedName ?? pin.Job.Id;
        _preview.LoadPreview(pin.Candidate.Profile, jobProto, showClothes: true);

        // A real pin action (not the initial populate): press flash + the drop sound.
        if (!wasPinned && _settled)
        {
            _pressProgress = 1f;
            PinConfirmed?.Invoke(SlotIndex);
        }

        _settled = true;
    }

    /// <summary>
    ///     Highlights the slot as the armed pin target while the user still has to pick a role.
    /// </summary>
    public void SetPending(bool pending)
    {
        _pending = pending;
        ApplyPanel();
    }

    /// <summary>
    ///     Marks the slot as the one whose pinned dossier is currently open.
    /// </summary>
    public void SetInspected(bool inspected)
    {
        _inspected = inspected;
        ApplyPanel();
        _nameLabel.FontColorOverride = _inspected && _pinned
            ? Color.FromHex("#D48944")
            : _pinned
                ? Color.FromHex("#E8DFD0")
                : Color.FromHex("#6B6E6B");
    }

    private void ApplyPanel()
    {
        PanelOverride = _pending ? PendingBox
            : _inspected && _pinned ? InspectedBox
            : _pinned ? PinnedBox
            : EmptyBox;
    }

    private void UpdateMoveArrows()
    {
        _moveLeftButton.Visible = _pinned && SlotIndex > 0;
        _moveRightButton.Visible = _pinned && SlotIndex < AshfallCharacterPoolConstants.PrioritySlotCount - 1;
    }

    // No structured given/family split exists in the profile model, so the display-only first
    // token is used here; the full name stays untouched everywhere else.
    private static string GetFirstName(string fullName) => fullName.Trim().Split(' ', 2)[0];

    // Pin feedback: an amber "stamp" border slams in with a pressed-in darkening, then a
    // light overshoot fades back. Modulating near-black surfaces alone is invisible, so
    // the border swap carries the effect.
    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_pressProgress <= 0f)
            return;

        _pressProgress = MathF.Max(0f, _pressProgress - args.DeltaSeconds / PressFlashDuration);

        if (_pressProgress > 0.6f)
        {
            PanelOverride = PinnedFlashBox;
            ModulateSelfOverride = new Color(0.72f, 0.68f, 0.6f);
        }
        else
        {
            ApplyPanel();
            var release = _pressProgress / 0.6f;
            ModulateSelfOverride = new Color(1f + 0.4f * release, 1f + 0.25f * release, 1f + 0.1f * release);
        }

        if (_pressProgress <= 0f)
            ModulateSelfOverride = null;
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        _preview.ClearPreview();
    }
}
