﻿using System.Threading.Tasks;
using Backend;
using Backend.AWS;
using Backend.Fixture;
using Backend.Fixture.Construct;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Common;
using Mod.DynamicEncounters.Helpers;
using NQ;
using NQ.Interfaces;
using NQutils.Sql;

namespace Mod.DynamicEncounters.Api.Controllers;

[Route("spawner")]
public class SpawnerController : Controller
{
    public class SpawnRequest
    {
        public ulong ConstructId { get; set; }
        public string File { get; set; }
        public Vec3 Position { get; set; }
    }
    
    [HttpPost]
    [Route("asteroid")]
    public async Task<IActionResult> SpawnAsync([FromBody] SpawnRequest request)
    {
        var provider = ModBase.ServiceProvider;
        var orleans = provider.GetOrleans();

        var constructInfoGrain = orleans.GetConstructInfoGrain(request.ConstructId);
        var constructInfo = await constructInfoGrain.Get();

        var random = provider.GetRequiredService<IRandomProvider>().GetRandom();

        var offset = random.RandomDirectionVec3() * 2000;
        var pos = offset + constructInfo.rData.position;
        
        var asteroidManagerGrain = orleans.GetAsteroidManagerGrain();
        var asteroidId = await asteroidManagerGrain.SpawnAsteroid(
            1, request.File, pos, 2
        );

        await asteroidManagerGrain.ForcePublish(asteroidId);
        
        return Ok(asteroidId);
    }
}