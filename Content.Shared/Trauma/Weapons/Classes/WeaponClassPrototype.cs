// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Knowledge;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Weapons.Classes;

/// <summary>
/// A class of weapon that is generally handled the same between its weapons.
/// Many similar weapons can use the same class stored in <see cref="WeaponClassComponent"/> which all benefit from weapon training knowledge.
/// </summary>
[Prototype]
public sealed partial class WeaponClassPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Human readable name of this weapon class.
    /// </summary>
    [DataField(required: true)]
    public LocId Name = string.Empty;

    /// <summary>
    /// The training knowledge associated with this weapon class.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Knowledge;

    /// <summary>
    /// Skill curve to scale melee weapon damage by.
    /// Default is [-21%, +43%] neutral at lvl 28
    /// </summary>
    [DataField]
    public SkillCurve MeleeDamage = new SumSkillCurve()
    {
        Curves = new()
        {
            new LinearSkillCurve()
            {
                CurveScale = 0.2f
            },
            new CubicSkillCurve()
            {
                SkillOffset = -0.45f,
                CurveScale = 1.7f,
                CurveOffset = 0.95f
            }
        }
    };

    /// <summary>
    /// Skill curve to scale gun aiming speed by.
    /// Default is [-60%, +90%] with high penalty at lvl 0
    /// </summary>
    [DataField]
    public SkillCurve AimSpeed = new LinearSkillCurve()
    {
        SkillScale = 1.5f,
        SkillOffset = 0.4f
    };
}
