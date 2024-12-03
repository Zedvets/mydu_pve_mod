using System;
using System.Collections.Generic;
using NQutils.Def;

namespace Mod.DynamicEncounters.Features.Common.Data;

public class WeaponItem(ulong itemTypeId, string itemTypeName, WeaponUnit weaponUnit, IEnumerable<AmmoItem> ammoItems)
{
    public ulong ItemTypeId { get; set; } = itemTypeId;
    public string ItemTypeName { get; set; } = itemTypeName;
    public string DisplayName { get; set; } = weaponUnit.DisplayName;
    public double BaseDamage { get; set; } = weaponUnit.BaseDamage;
    public double BaseAccuracy { get; set; } = weaponUnit.BaseAccuracy;
    public double FalloffDistance { get; set; } = weaponUnit.FalloffDistance;
    public double FalloffTracking { get; set; } = weaponUnit.FalloffTracking;
    public double FalloffAimingCone { get; set; } = weaponUnit.FalloffAimingCone;
    public double BaseOptimalTracking { get; set; } = weaponUnit.BaseOptimalTracking;
    public double BaseOptimalDistance { get; set; } = weaponUnit.BaseOptimalDistance;
    public double BaseOptimalAimingCone { get; set; } = weaponUnit.BaseOptimalAimingCone;
    public double OptimalCrossSectionDiameter { get; set; } = weaponUnit.OptimalCrossSectionDiameter;
    public double BaseCycleTime { get; set; } = weaponUnit.BaseCycleTime;
    public double BaseReloadTime { get; set; } = weaponUnit.BaseReloadTime;
    public double MagazineVolume { get; set; } = weaponUnit.MagazineVolume;

    public double GetNumberOfShotsInMagazine(
        AmmoItem ammoItem,
        double magazineBuffFactor = 1.5d
    ) => MagazineVolume * magazineBuffFactor / ammoItem.UnitVolume;

    public double GetTimeToEmpty(
        AmmoItem ammoItem,
        double magazineBuffFactor = 1.5d,
        double cycleTimeBuffFactor = 1 / 1.5625d
    ) => GetNumberOfShotsInMagazine(ammoItem, magazineBuffFactor) * (BaseCycleTime * cycleTimeBuffFactor);

    public double GetReloadTime(double reloadTimeBuffFactor) => BaseReloadTime * reloadTimeBuffFactor;

    public double GetTotalCycleTime(
        AmmoItem ammoItem,
        double magazineBuffFactor = 1.5d,
        double cycleTimeBuffFactor = 1 / 1.5625d,
        double reloadTimeBuffFactor = 1 / 1.5625d
    ) => GetTimeToEmpty(ammoItem, magazineBuffFactor, cycleTimeBuffFactor) + GetReloadTime(reloadTimeBuffFactor);

    public double GetSustainedRateOfFire(
        AmmoItem ammoItem,
        double magazineBuffFactor = 1.5d,
        double cycleTimeBuffFactor = 1 / 1.5625d,
        double reloadTimeBuffFactor = 1 / 1.5625d
    ) => GetNumberOfShotsInMagazine(ammoItem, magazineBuffFactor) /
         Math.Clamp(
             GetTotalCycleTime(ammoItem, magazineBuffFactor, cycleTimeBuffFactor, reloadTimeBuffFactor),
             0.1d,
             60d
         );

    public double GetShotWaitTime(
        AmmoItem ammoItem,
        double magazineBuffFactor = 1.5d,
        double cycleTimeBuffFactor = 1 / 1.5625d,
        double reloadTimeBuffFactor = 1 / 1.5625d
    )
    {
        cycleTimeBuffFactor = Math.Clamp(cycleTimeBuffFactor, 0.1d, 5d);
        reloadTimeBuffFactor = Math.Clamp(reloadTimeBuffFactor, 0.1d, 5d);
        magazineBuffFactor = Math.Clamp(magazineBuffFactor, 0.1d, 5d);

        var result = 1d / Math.Clamp(
            GetSustainedRateOfFire(ammoItem, magazineBuffFactor, cycleTimeBuffFactor, reloadTimeBuffFactor),
            0.1d,
            60d
        );

        if (result <= 0.5d)
        {
            return Math.Clamp(BaseCycleTime, 0.5d, 60);
        }

        return result;
    }

    public double GetShotWaitTimePerGun(
        AmmoItem ammoItem,
        int weaponCount,
        double magazineBuffFactor = 1.5d,
        double cycleTimeBuffFactor = 1 / 1.5625d,
        double reloadTimeBuffFactor = 1 / 1.5625d
    )
    {
        return GetShotWaitTime(ammoItem, magazineBuffFactor, cycleTimeBuffFactor, reloadTimeBuffFactor) /
            Math.Clamp(weaponCount, 1, 10);
    }

    private IEnumerable<AmmoItem> AmmoItems { get; } = ammoItems;

    public IEnumerable<AmmoItem> GetAmmoItems() => AmmoItems;
}