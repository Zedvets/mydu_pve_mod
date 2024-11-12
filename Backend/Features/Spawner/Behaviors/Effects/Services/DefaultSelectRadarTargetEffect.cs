﻿using System;
using Mod.DynamicEncounters.Features.Common.Data;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Interfaces;
using Mod.DynamicEncounters.Helpers;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Services;

public class DefaultSelectRadarTargetEffect : ISelectRadarTargetEffect
{
    private Random Random { get; set; } = new();
    private double AccumulatedDeltaTime { get; set; }
    private NpcRadarContact? LastSelectedTarget { get; set; }
    
    public NpcRadarContact? GetTarget(ISelectRadarTargetEffect.Params @params)
    {
        AccumulatedDeltaTime += @params.Context.DeltaTime;

        if (LastSelectedTarget == null || AccumulatedDeltaTime > 5)
        {
            LastSelectedTarget = Random.PickOneAtRandom(@params.Contacts);
            AccumulatedDeltaTime -= 5;
        }
        
        return LastSelectedTarget;
    }
}