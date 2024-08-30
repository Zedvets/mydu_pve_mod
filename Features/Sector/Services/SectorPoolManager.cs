﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BotLib.BotClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Common;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.Sector.Data;
using Mod.DynamicEncounters.Features.Sector.Interfaces;
using Mod.DynamicEncounters.Helpers;
using NQ;

namespace Mod.DynamicEncounters.Features.Sector.Services;

public class SectorPoolManager(IServiceProvider serviceProvider) : ISectorPoolManager
{
    public const double SectorGridSnap = DistanceHelpers.OneSuInMeters * 20;

    private readonly IRandomProvider _randomProvider = serviceProvider.GetRequiredService<IRandomProvider>();

    private readonly ISectorInstanceRepository _sectorInstanceRepository =
        serviceProvider.GetRequiredService<ISectorInstanceRepository>();

    private readonly ILogger<SectorPoolManager> _logger = serviceProvider.CreateLogger<SectorPoolManager>();

    public async Task<IEnumerable<SectorInstance>> GenerateSectorPool(SectorGenerationArgs args)
    {
        var count = await _sectorInstanceRepository.GetCountAsync();
        var missingQuantity = args.Quantity - count;

        if (missingQuantity <= 0)
        {
            return await _sectorInstanceRepository.GetAllAsync();
        }

        var random = _randomProvider.GetRandom();

        var randomMinutes = random.Next(0, 60);

        for (var i = 0; i < missingQuantity; i++)
        {
            var radius = MathFunctions.Lerp(
                args.MinRadius,
                args.MaxRadius,
                random.NextDouble()
            );
            
            var position = random.RandomDirectionVec3() * radius;
            position += args.CenterPosition;
            position = position.GridSnap(SectorGridSnap);

            var encounter = random.PickOneAtRandom(args.Encounters);

            var instance = new SectorInstance
            {
                Id = Guid.NewGuid(),
                Sector = position,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow + args.ExpirationTimeSpan + TimeSpan.FromMinutes(randomMinutes * i),
                OnLoadScript = encounter.OnLoadScript,
                OnSectorEnterScript = encounter.OnSectorEnterScript,
            };

            await _sectorInstanceRepository.AddAsync(instance);
        }

        return await _sectorInstanceRepository.GetAllAsync();
    }

    public async Task LoadUnloadedSectors(Client client)
    {
        var scriptService = serviceProvider.GetRequiredService<IScriptService>();
        var unloadedSectors = (await _sectorInstanceRepository.FindUnloadedAsync()).ToList();

        if (!unloadedSectors.Any())
        {
            _logger.LogDebug("No Sectors {Count} Need Loading", unloadedSectors.Count);
            return;
        }

        foreach (var sector in unloadedSectors)
        {
            try
            {
                await scriptService.ExecuteScriptAsync(
                    sector.OnLoadScript,
                    new ScriptContext(
                        serviceProvider,
                        new HashSet<ulong>(),
                        sector.Sector,
                        client
                    )
                );

                await _sectorInstanceRepository.SetLoadedAsync(sector.Id, true);

                _logger.LogInformation("Loaded Sector {Id}({Sector})", sector.Id, sector.Sector);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to Load Sector {Id}({Sector})", sector.Id, sector.Sector);

                await _sectorInstanceRepository.SetLoadedAsync(sector.Id, false);
                throw;
            }
        }
    }

    public async Task ExecuteSectorCleanup(SectorGenerationArgs args)
    {
        await _sectorInstanceRepository.DeleteExpiredAsync();
        
        _logger.LogDebug("Executed Sector Cleanup");
    }

    public async Task ActivateEnteredSectors(Client client)
    {
        var sectorsToActivate = (await _sectorInstanceRepository.FindSectorsRequiringStartupAsync()).ToList();

        if (!sectorsToActivate.Any())
        {
            _logger.LogDebug("No sectors need startup");
            return;
        }

        var scriptService = serviceProvider.GetRequiredService<IScriptService>();

        foreach (var sector in sectorsToActivate)
        {
            _logger.LogInformation(
                "Starting up sector({Sector}) encounter: '{Encounter}'",
                sector.Sector,
                sector.OnSectorEnterScript
            );

            try
            {
                await scriptService.ExecuteScriptAsync(
                    sector.OnSectorEnterScript,
                    new ScriptContext(
                        serviceProvider,
                        new HashSet<ulong>(),
                        sector.Sector,
                        client
                    )
                );

                await _sectorInstanceRepository.TagAsStartedAsync(sector.Id);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "Failed to start encounter({Encounter}) at sector({Sector})",
                    sector.OnSectorEnterScript,
                    sector.Sector
                );
                throw;
            }
        }
    }

    private Task ExpireSector(SectorInstance instance)
    {
        return _sectorInstanceRepository.DeleteAsync(instance.Id);
    }

    private struct ConstructSectorRow
    {
        public ulong id { get; set; }
        public long sector_x { get; set; }
        public long sector_y { get; set; }
        public long sector_z { get; set; }

        public Vec3 SectorToVec3() => new() { x = sector_x, y = sector_y, z = sector_z };
    }
}