using System;
using System.Collections.Concurrent;
using System.Linq;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Interfaces;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Services;

public class EffectHandler : IEffectHandler
{
    private ConcurrentDictionary<Type, object> DefaultEffects { get; set; } = new();
    private ConcurrentDictionary<Type, EffectEntry> Effects { get; set; } = new();

    public EffectHandler(IServiceProvider provider)
    {
        RegisterDefault<ICalculateTargetMovePositionEffect>(new CalculateTargetPositionWithOffsetEffect(provider));
        RegisterDefault<IMovementEffect>(new BurnToTargetMovementEffect());
        RegisterDefault<ISelectRadarTargetEffect>(new RandomSelectRadarTargetEffect());
    }

    public void RegisterDefault<T>(T effect)
    {
        DefaultEffects.TryAdd(typeof(T), effect);
    }

    public T? GetOrNull<T>() where T : IEffect
    {
        if (Effects.TryGetValue(typeof(T), out var entry))
        {
            if (!entry.IsExpired(DateTime.UtcNow))
            {
                return entry.EffectAs<T>();
            }
        }

        if (DefaultEffects.TryGetValue(typeof(T), out var effect))
        {
            return (T)effect;
        }

        return default;
    }

    public void Activate<T>(T effect, TimeSpan duration)
    {
        if (effect == null)
        {
            return;
        }

        var entry = new EffectEntry(effect, DateTime.UtcNow + duration);
        if (!Effects.TryAdd(typeof(T), entry))
        {
            Effects[typeof(T)] = entry;
        }
    }

    public void Deactivate<T>()
    {
        Effects.TryRemove(typeof(T), out _);
    }

    public void CleanupExpired()
    {
        foreach (var kvp in Effects.ToList())
        {
            if (kvp.Value.IsExpired(DateTime.UtcNow))
            {
                Effects.TryRemove(kvp.Key, out _);
            }
        }
    }

    private class EffectEntry(object effect, DateTime expiresAt)
    {
        private object Effect { get; set; } = effect;
        private DateTime ExpiresAt { get; set; } = expiresAt;

        public T EffectAs<T>() => (T)Effect;

        public bool IsExpired(DateTime dateTime) => dateTime > ExpiresAt;
    }
}