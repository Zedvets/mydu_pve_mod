using System;
using System.Threading.Tasks;
using System.Timers;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Features.Interfaces;
using Mod.DynamicEncounters.Features.TaskQueue.Interfaces;
using Mod.DynamicEncounters.Helpers;

namespace Mod.DynamicEncounters;

public class HighTickModLoop(int framesPerSecond) : ModBase
{
    private StopWatch _stopWatch = new();
    private DateTime _lastTickTime;
    
    public override async Task Start()
    {
        _stopWatch.Start();
        _lastTickTime = DateTime.UtcNow;

        if (framesPerSecond <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(framesPerSecond), "Frames per second should be > 0");
        }
        
        while (true)
        {
            await OnTick();
        }
    }

    private async Task OnTick()
    {
        var currentTickTime = DateTime.UtcNow;
        var deltaTime = currentTickTime - _lastTickTime;
        _lastTickTime = currentTickTime;

        var fpsSeconds = 1d / framesPerSecond;
        if (deltaTime.TotalSeconds < fpsSeconds)
        {
            var waitSeconds = Math.Max(0, fpsSeconds - deltaTime.TotalSeconds);
            await Task.Delay(TimeSpan.FromSeconds(waitSeconds));
        }
            
        await Tick(deltaTime);
        
        _stopWatch = new StopWatch();
        _stopWatch.Start();
    }

    public virtual Task Tick(TimeSpan deltaTime)
    {
        return Task.CompletedTask;
    }
}

public class TaskQueueLoop : ModBase
{
    public override Task Start()
    {
        var provider = ServiceProvider;
        var logger = provider.CreateLogger<TaskQueueLoop>();
        var taskQueueService = provider.GetRequiredService<ITaskQueueService>();
        var featureService = provider.GetRequiredService<IFeatureReaderService>();

        var taskCompletionSource = new TaskCompletionSource();
        
        var timer = new Timer(5000);
        timer.Elapsed += async (sender, args) =>
        {
            try
            {
                var isEnabled = await featureService.GetEnabledValue<TaskQueueLoop>(false);

                if (isEnabled)
                {
                    await taskQueueService.ProcessQueueMessages();
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to execute {Name}", nameof(TaskQueueLoop));
            }
        };
        
        timer.Start();

        // It will never complete because we're not setting result
        return taskCompletionSource.Task;
    }
}