using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Services;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Data;
using Mod.DynamicEncounters.Helpers;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors;

public class BrainBehavior(ulong constructId, IPrefab prefab) : IConstructBehavior
{
    private readonly ulong _constructId = constructId;
    public IPrefab Prefab { get; } = prefab;
    private ILogger<BrainBehavior> _logger;
    public BehaviorTaskCategory Category => BehaviorTaskCategory.MediumPriority;

    public Task InitializeAsync(BehaviorContext context)
    {
        _logger = context.ServiceProvider.CreateLogger<BrainBehavior>();
        
        return Task.CompletedTask;
    }

    public async Task TickAsync(BehaviorContext context)
    {
        await Task.Yield();
        
        var oppositeV = context.VelocityWithTargetDotProduct < 0;
        var brakeDistance = context.CalculateBrakingDistance();
        var brakingTime = context.CalculateBrakingTime();
        var fromZeroToTargetVelocityTime = context.CalculateAccelerationToTargetSpeedTime(0);

        var timeToMerge = context.CalculateTimeToMergeToDistance(context.GetBestWeaponOptimalRange());
        var totalManeuverTime = brakingTime + fromZeroToTargetVelocityTime;
        
        // _logger.LogInformation("Construct {Construct} Brain. TTM={TTM}, TMT={TMT} OV={OV}, BD={BD}, BT={BT}, FZ_TTV={FZTTV}", 
        //     constructId, 
        //     timeToMerge,
        //     totalManeuverTime,
        //     oppositeV,
        //     brakeDistance,
        //     brakingTime,
        //     fromZeroToTargetVelocityTime    
        // );
        
        if (oppositeV)
        {
            if (totalManeuverTime >= timeToMerge)
            {
                // context.Effects.Activate<IMovementEffect>(new ApplyBrakesMovementEffect(), TimeSpan.FromSeconds(3));
            }
        }

        // var targetMovePosition = context.GetTargetMovePosition();
        // var position = context.Position;
        //
        // var bestWeapon = context.DamageData.GetBestDamagingWeapon();
        // if (position.HasValue && bestWeapon != null)
        // {
        //     var targetDistance = context.GetTargetMoveDistance() * 2;
        //     if (context.IsApproachingTarget() && Math.Abs(targetMovePosition.Dist(position.Value)) < targetDistance)
        //     {
        //         context.TargetRotationPositionMultiplier = -1;
        //     }
        //     else
        //     {
        //         context.TargetRotationPositionMultiplier = 1;
        //     }
        // }
    }
}