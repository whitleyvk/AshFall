// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Gameplay;
using Content.Client.UserInterface.Screens;
using Content.Medical.Client.Targeting;
using Content.Medical.Client.UserInterface.Systems.Targeting.Widgets;
using Content.Medical.Common.Targeting;
using Content.Medical.Shared.Targeting;
using Content.Shared.Input;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.Player;
using Robust.Shared.Input.Binding;

namespace Content.Medical.Client.UserInterface.Systems.Targeting;

public sealed partial class TargetingUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>, IOnSystemChanged<TargetingSystem>
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IEntityNetworkManager _entNet = default!;
    [Dependency] private IPlayerManager _player = default!;

    private TargetingComponent? _targetingComponent;
    private TargetingControl? TargetingControl;
    private TargetingRadialMenu? _radialMenu;

    public override void Initialize()
    {
        base.Initialize();

        UIManager.OnScreenChanged += screens =>
        {
            if (screens.New is not {} screen)
                return;

            try
            {
                // separated hud has its own container for it
                var container = screen.FindControl<LayoutContainer>("ViewportContainer");
                CreateTargetingControl(container);
            }
            catch (ArgumentException)
            {
                // default hud, just add it directly
                CreateTargetingControl(screen);
            }
        };
    }

    public void OnSystemLoaded(TargetingSystem system)
    {
        system.TargetingStartup += AddTargetingControl;
        system.TargetingShutdown += RemoveTargetingControl;
        system.TargetChange += CycleTarget;
    }

    public void OnSystemUnloaded(TargetingSystem system)
    {
        system.TargetingStartup -= AddTargetingControl;
        system.TargetingShutdown -= RemoveTargetingControl;
        system.TargetChange -= CycleTarget;
    }

    private void CreateTargetingControl(LayoutContainer parent)
    {
        TargetingControl?.Orphan();
        var control = new TargetingControl();
        control.OnSetTarget += CycleTarget;
        parent.AddChild(control);
        TargetingControl = control;
        LayoutContainer.SetAnchorAndMarginPreset(control, LayoutContainer.LayoutPreset.BottomRight, margin: 5);
        UpdateVisibility();
    }

    public void OnStateEntered(GameplayState state)
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenTargetingMenu,
                InputCmdHandler.FromDelegate(_ => ToggleTargetingRadialMenu()))
            .Register<TargetingUIController>();

        UpdateVisibility();
    }

    public void OnStateExited(GameplayState state)
    {
        CommandBinds.Unregister<TargetingUIController>();
        CloseTargetingRadialMenu();
    }

    public void ToggleTargetingRadialMenu()
    {
        if (_radialMenu != null)
        {
            CloseTargetingRadialMenu();
            return;
        }

        if (_player.LocalEntity is not {} user
            || !_entMan.TryGetComponent<TargetingComponent>(user, out var comp))
        {
            return;
        }

        _radialMenu = new TargetingRadialMenu();
        _radialMenu.SetCurrentTarget(comp.Target);
        _radialMenu.OnTargetSelected += CycleTarget;
        _radialMenu.OnClose += OnRadialMenuClosed;
        _radialMenu.OpenOverMouseScreenPosition();
    }

    private void OnRadialMenuClosed()
    {
        if (_radialMenu == null)
            return;

        _radialMenu.OnTargetSelected -= CycleTarget;
        _radialMenu.OnClose -= OnRadialMenuClosed;
        _radialMenu = null;
    }

    public void CloseTargetingRadialMenu()
    {
        if (_radialMenu == null)
            return;

        _radialMenu.Close();
    }

    public void AddTargetingControl(TargetingComponent component)
    {
        _targetingComponent = component;
        _radialMenu?.SetCurrentTarget(component.Target);
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (TargetingControl is not {} control)
            return;

        control.SetTargetDollVisible(_targetingComponent != null);

        if (_targetingComponent is {} comp)
            control.SetBodyPartsVisible(comp.Target);
    }

    public void RemoveTargetingControl()
    {
        _targetingComponent = null;
        CloseTargetingRadialMenu();
        UpdateVisibility();
    }

    public void CycleTarget(TargetBodyPart bodyPart)
    {
        if (_player.LocalEntity is not {} user
            || !_entMan.TryGetComponent<TargetingComponent>(user, out var comp)
            || comp.Target == bodyPart)
            return;

        _entNet.SendSystemNetworkMessage(new ChangeTargetMessage(bodyPart));
        TargetingControl?.SetBodyPartsVisible(bodyPart);
        _radialMenu?.SetCurrentTarget(bodyPart);
    }
}
