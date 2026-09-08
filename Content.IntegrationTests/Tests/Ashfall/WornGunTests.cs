using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.Ashfall;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class WornGunTests : InteractionTest
{
    [TestCase("WeaponPistolMk58Worn")]
    [TestCase("WeaponPistolViperWorn")]
    [TestCase("WeaponPistolCobraWorn")]
    [TestCase("WeaponRevolverDeckardWorn")]
    [TestCase("WeaponSubMachineGunWt550Worn")]
    [TestCase("WeaponSubMachineGunC20rWorn")]
    [TestCase("WeaponSniperMosinWorn")]
    [TestCase("WeaponSniperHristovWorn")]
    [TestCase("WeaponRifleLecterWorn")]
    [TestCase("WeaponRifleEstocWorn")]
    [TestCase("WeaponShotgunKammererWorn")]
    [TestCase("WeaponShotgunBulldogWorn")]
    [TestCase("WeaponShotgunEnforcerWorn")]
    [TestCase("WeaponShotgunHushpupWorn")]
    [TestCase("WeaponLightMachineGunL6Worn")]
    [TestCase("WeaponEnergyCrossbowWorn")]
    public async Task WornGunCanBeUsed(string prototype)
    {
        var item = await PlaceInHands(prototype);
        var gunUid = ToServer(item);
        var gun = SComp<GunComponent>(gunUid);
        Assert.Multiple(() =>
        {
            Assert.That(gun.MinAngleModified.Theta, Is.GreaterThanOrEqualTo(Angle.Zero.Theta));
            Assert.That(gun.MaxAngleModified.Theta, Is.GreaterThanOrEqualTo(gun.MinAngleModified.Theta));
        });

        await UseInHand();
        await RunSeconds(0.5f);
        await AttemptShoot(TargetCoords, assert: false);
    }
}
