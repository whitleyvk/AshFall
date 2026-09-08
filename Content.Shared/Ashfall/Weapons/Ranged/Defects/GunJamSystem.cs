using Content.Shared.Ashfall.Weapons.Ranged.Defects.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Ashfall.Weapons.Ranged.Defects;

/// <summary>
/// Handles per-shot jam chance for guns with GunJamDefectComponent.
/// A jammed gun cannot fire until the player racks the bolt (Z / Use In Hand or context verb).
/// </summary>
public sealed partial class GunJamSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunJamDefectComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<GunJamDefectComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<GunJamDefectComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<GunJamDefectComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
    }

    private void OnAttemptShoot(Entity<GunJamDefectComponent> ent, ref AttemptShootEvent args)
    {
        if (!ent.Comp.IsJammed)
            return;

        args.Cancelled = true;

        if (_timing.CurTime < ent.Comp.NextPopupTime)
            return;

        ent.Comp.NextPopupTime = _timing.CurTime + ent.Comp.PopupCooldown;
        _popup.PopupClient(Loc.GetString("gun-jam-blocked"), ent, args.User, PopupType.SmallCaution);
    }

    private void OnGunShot(Entity<GunJamDefectComponent> ent, ref GunShotEvent args)
    {
        if (ent.Comp.IsJammed)
            return;

        if (!SharedRandomExtensions.PredictedProb(_timing, ent.Comp.JamChance, GetNetEntity(ent)))
            return;

        ent.Comp.IsJammed = true;
        Dirty(ent, ent.Comp);

        _audio.PlayPredicted(ent.Comp.SoundJamRack, ent.Owner, args.User);
        _popup.PopupClient(Loc.GetString("gun-jam-jammed"), ent, args.User, PopupType.SmallCaution);
    }

    private void OnUseInHand(Entity<GunJamDefectComponent> ent, ref UseInHandEvent args)
    {
        if (!ent.Comp.IsJammed)
            return;

        ClearJam(ent, args.User);
        args.Handled = true;
    }

    private void OnGetVerbs(Entity<GunJamDefectComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || !ent.Comp.IsJammed)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("gun-jam-verb-clear"),
            Act = () => ClearJam(ent, user),
            Priority = 1
        });
    }

    public void ClearJam(Entity<GunJamDefectComponent> ent, EntityUid user)
    {
        ent.Comp.IsJammed = false;
        Dirty(ent, ent.Comp);

        _audio.PlayPredicted(ent.Comp.SoundJamRack, ent.Owner, user);

        if (_timing.CurTime < ent.Comp.NextPopupTime)
            return;

        ent.Comp.NextPopupTime = _timing.CurTime + ent.Comp.PopupCooldown;
        _popup.PopupClient(Loc.GetString("gun-jam-cleared"), ent, user);
    }
}
