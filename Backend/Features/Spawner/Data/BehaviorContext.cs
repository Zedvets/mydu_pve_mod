using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Features.Common.Data;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Events.Data;
using Mod.DynamicEncounters.Features.Events.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Interfaces;
using Mod.DynamicEncounters.Helpers;
using NQ;

namespace Mod.DynamicEncounters.Features.Spawner.Data;

public class BehaviorContext(
    ulong constructId,
    long factionId,
    Guid? territoryId,
    Vec3 sector,
    IServiceProvider serviceProvider,
    IPrefab prefab
) : BaseContext
{
    public ulong? TargetConstructId { get; private set; }
    public Vec3 TargetMovePosition { get; private set; }

    [Newtonsoft.Json.JsonIgnore]
    [JsonIgnore]
    public IEnumerable<Vec3> TargetElementPositions { get; set; } = [];

    private double _deltaTime;

    public double DeltaTime
    {
        get => _deltaTime;
        set => _deltaTime = Math.Clamp(value, 1 / 60f, 1 / 20f);
    }

    public const string AutoTargetMovePositionEnabledProperty = "AutoTargetMovePositionEnabled";
    public const string AutoSelectAttackTargetConstructProperty = "AutoSelectAttackTargetConstruct";

    public Vec3 Velocity { get; set; }
    public Vec3? Position { get; set; }
    public Quat Rotation { get; set; }
    public HashSet<ulong> PlayerIds { get; set; } = new();
    public ulong ConstructId { get; } = constructId;
    public long FactionId { get; } = factionId;
    public Guid? TerritoryId { get; } = territoryId;
    public Vec3 Sector { get; } = sector;
    public IServiceProvider ServiceProvider { get; init; } = serviceProvider;

    public ConcurrentDictionary<string, bool> PublishedEvents = [];

    [Newtonsoft.Json.JsonIgnore]
    [JsonIgnore]
    public IPrefab Prefab { get; set; } = prefab;

    public List<Waypoint> Waypoints { get; set; } = [];
    public Waypoint? TargetWaypoint { get; set; }

    public DateTime? TargetSelectedTime { get; set; }

    public bool IsAlive { get; set; } = true;

    public bool IsActiveWreck { get; set; }

    public Task NotifyEvent(string @event, BehaviorEventArgs eventArgs)
    {
        // TODO for custom events
        return Task.CompletedTask;
    }

    public async Task NotifyCoreStressHighAsync(BehaviorEventArgs eventArgs)
    {
        if (PublishedEvents.ContainsKey(nameof(NotifyCoreStressHighAsync)))
        {
            return;
        }

        await Prefab.Events.OnCoreStressHigh.ExecuteAsync(
            new ScriptContext(
                eventArgs.Context.ServiceProvider,
                eventArgs.Context.FactionId,
                eventArgs.Context.PlayerIds.ToHashSet(),
                eventArgs.Context.Sector,
                eventArgs.Context.TerritoryId
            )
            {
                ConstructId = eventArgs.ConstructId
            }
        );

        PublishedEvents.TryAdd(nameof(NotifyCoreStressHighAsync), true);
    }

    public async Task NotifyConstructDestroyedAsync(BehaviorEventArgs eventArgs)
    {
        if (PublishedEvents.ContainsKey(nameof(NotifyConstructDestroyedAsync)))
        {
            return;
        }

        var eventService = ServiceProvider.GetRequiredService<IEventService>();

        var taskList = new List<Task>();

        // send event for all players piloting constructs
        // TODO #limitation = not considering gunners and boarders
        var logger = eventArgs.Context.ServiceProvider.CreateLogger<BehaviorContext>();

        try
        {
            if (eventArgs.Context.PlayerIds.Count == 0)
            {
                if (eventArgs.Context.TargetConstructId.HasValue)
                {
                    logger.LogWarning("Could not find any players. Fallback logic will use target construct owner");

                    var constructService = eventArgs.Context.ServiceProvider
                        .GetRequiredService<IConstructService>();
                    var constructInfo = await constructService.NoCache()
                        .GetConstructInfoAsync(eventArgs.Context.TargetConstructId.Value);

                    if (constructInfo?.mutableData.pilot != null)
                    {
                        var playerId = constructInfo.mutableData.pilot.Value;
                        eventArgs.Context.PlayerIds.Add(playerId);

                        logger.LogWarning("Found Player({Player}) on NOCACHE attempt", playerId);
                    }
                    else if (constructInfo != null && eventArgs.Context.PlayerIds.Count == 0)
                    {
                        var owner = constructInfo.mutableData.ownerId;

                        if (owner.IsPlayer())
                        {
                            eventArgs.Context.PlayerIds.Add(owner.playerId);
                            logger.LogWarning("Found Player({Player}) OWNER", owner.playerId);
                        }
                        else
                        {
                            logger.LogError("Owner is an Organization({Org}). This is not handled yet.",
                                owner.organizationId);
                        }
                    }
                }
                else
                {
                    logger.LogError("Can't use fallback - no target construct");
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to give quanta to Target Construct ({Construct}) Pilot",
                eventArgs.Context.TargetConstructId);
        }

        logger.LogInformation("NPC Defeated by players: {Players}", string.Join(", ", eventArgs.Context.PlayerIds));

        var tasks = eventArgs.Context.PlayerIds.Select(id =>
            eventService.PublishAsync(
                new PlayerDefeatedNpcEvent(
                    id,
                    eventArgs.Context.Sector,
                    eventArgs.ConstructId,
                    eventArgs.Context.FactionId,
                    eventArgs.Context.PlayerIds.Count
                )
            )
        );

        taskList.AddRange(tasks);

        var scriptExecutionTask = Prefab.Events.OnDestruction.ExecuteAsync(
            new ScriptContext(
                eventArgs.Context.ServiceProvider,
                eventArgs.Context.FactionId,
                eventArgs.Context.PlayerIds.ToHashSet(),
                eventArgs.Context.Sector,
                eventArgs.Context.TerritoryId
            )
            {
                ConstructId = eventArgs.ConstructId
            }
        );

        taskList.Add(scriptExecutionTask);

        await Task.WhenAll(taskList);

        PublishedEvents.TryAdd(nameof(NotifyConstructDestroyedAsync), true);
    }

    public async Task NotifyShieldHpHalfAsync(BehaviorEventArgs eventArgs)
    {
        if (PublishedEvents.ContainsKey(nameof(NotifyShieldHpHalfAsync)))
        {
            return;
        }

        await Prefab.Events.OnShieldHalfAction.ExecuteAsync(
            new ScriptContext(
                eventArgs.Context.ServiceProvider,
                eventArgs.Context.FactionId,
                eventArgs.Context.PlayerIds,
                eventArgs.Context.Sector,
                eventArgs.Context.TerritoryId
            )
            {
                ConstructId = eventArgs.ConstructId
            }
        );

        PublishedEvents.TryAdd(nameof(NotifyShieldHpHalfAsync), true);
    }

    public async Task NotifyShieldHpLowAsync(BehaviorEventArgs eventArgs)
    {
        if (PublishedEvents.ContainsKey(nameof(NotifyShieldHpLowAsync)))
        {
            return;
        }

        await Prefab.Events.OnShieldLowAction.ExecuteAsync(
            new ScriptContext(
                eventArgs.Context.ServiceProvider,
                eventArgs.Context.FactionId,
                eventArgs.Context.PlayerIds,
                eventArgs.Context.Sector,
                eventArgs.Context.TerritoryId
            )
            {
                ConstructId = eventArgs.ConstructId
            }
        );

        PublishedEvents.TryAdd(nameof(NotifyShieldHpLowAsync), true);
    }

    public async Task NotifyShieldHpDownAsync(BehaviorEventArgs eventArgs)
    {
        if (PublishedEvents.ContainsKey(nameof(NotifyShieldHpDownAsync)))
        {
            return;
        }

        await Prefab.Events.OnShieldDownAction.ExecuteAsync(
            new ScriptContext(
                eventArgs.Context.ServiceProvider,
                eventArgs.Context.FactionId,
                eventArgs.Context.PlayerIds,
                eventArgs.Context.Sector,
                eventArgs.Context.TerritoryId
            )
            {
                ConstructId = eventArgs.ConstructId
            }
        );

        PublishedEvents.TryAdd(nameof(NotifyShieldHpDownAsync), true);
    }

    public void Deactivate<T>() where T : IConstructBehavior
    {
        var name = typeof(T).FullName;
        var key = $"{name}_FINISHED";

        if (!Properties.TryAdd(key, false))
        {
            Properties[key] = false;
        }
    }

    public bool IsBehaviorActive<T>() where T : IConstructBehavior
    {
        return IsBehaviorActive(typeof(T));
    }

    public bool IsBehaviorActive(Type type)
    {
        var name = type.FullName;
        var key = $"{name}_FINISHED";

        if (Properties.TryGetValue(key, out var finished) && finished is bool finishedBool)
        {
            return !finishedBool;
        }

        return true;
    }

    public void SetTargetMovePosition(Vec3 position)
    {
        TargetMovePosition = position;
    }

    public void SetTargetConstructId(ulong? constructId)
    {
        // can't target itself
        if (constructId == ConstructId)
        {
            return;
        }

        TargetConstructId = constructId;
        TargetSelectedTime = DateTime.UtcNow;
    }

    
}