﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Features.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Services;
using Mod.DynamicEncounters.Features.Spawner.Extensions;
using Mod.DynamicEncounters.Features.TaskQueue.Interfaces;

namespace Mod.DynamicEncounters.Features.Scripts.Actions;

[ScriptActionName(ActionName)]
public class DelayedScriptAction(ScriptActionItem actionItem) : IScriptAction
{
    public const string ActionName = "delayed-script";
    public string GetKey() => Name;

    public string Name => ActionName;

    public async Task<ScriptActionResult> ExecuteAsync(ScriptContext context)
    {
        var provider = ModBase.ServiceProvider;
        var featureService = provider.GetRequiredService<IFeatureReaderService>();
        var taskQueueService = provider.GetRequiredService<ITaskQueueService>();

        var delaySecondsDefault = await featureService.GetIntValueAsync("POIDespawnDelaySeconds", 60 * 5);
        var delaySeconds = context.Properties.GetOrDefault("DelaySeconds", delaySecondsDefault);
        var delayResult = delaySecondsDefault;
        
        if (int.TryParse($"{delaySeconds}", out var delayInt))
        {
            delayResult = delayInt;
        }

        foreach (var kvp in actionItem.Properties)
        {
            context.Properties.TryAdd(kvp.Key, kvp.Value);
        }

        await taskQueueService.EnqueueScript(
            new ScriptActionItem
            {
                Actions = actionItem.Actions,
                ConstructId = actionItem.ConstructId > 0 ? actionItem.ConstructId : context.ConstructId ?? 0,
                Sector = actionItem.Sector ?? context.Sector,
                FactionId = actionItem.FactionId ?? context.FactionId,
                Properties = context.Properties.ToDictionary(),
            },
            DateTime.UtcNow + TimeSpan.FromSeconds(delayResult)
        );

        return ScriptActionResult.Successful();
    }
}