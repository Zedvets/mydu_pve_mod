using System.Collections.Generic;
using Mod.DynamicEncounters.Features.Common.Data;
using Mod.DynamicEncounters.Features.Spawner.Data;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Interfaces;

public interface ISelectRadarTargetEffect : IEffect
{
    NpcRadarContact? GetTarget(Params @params);
    
    public class Params
    {
        public double TargetDistance { get; set; }
        public IEnumerable<NpcRadarContact> Contacts { get; set; } 
        public BehaviorContext Context { get; set; }
        public double DecisionTimeSeconds { get; set; } = 30;
    }
}