﻿using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Overrides.Common;
using Newtonsoft.Json;
using NQ;

namespace Mod.DynamicEncounters.Overrides.Actions;

public class InteractAction(IServiceProvider provider) : IModActionHandler
{
    public async Task HandleAction(ulong playerId, ModAction action)
    {
        var logger = provider.GetRequiredService<ILoggerFactory>()
            .CreateLogger<InteractAction>();

        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        using var httpClient = httpClientFactory.CreateClient();

        var baseUrl = Config.GetPveModBaseUrl();
        var questInteractUrl = Path.Combine(baseUrl, "quest/interact");

        await httpClient.PostAsync(
            questInteractUrl,
            new StringContent(
                JsonConvert.SerializeObject(new
                {
                    playerId,
                    action.constructId,
                    elementId = action.elementId == 0 ? (ulong?)null : action.elementId
                }),
                Encoding.UTF8,
                "application/json"
            )
        );
    }
}