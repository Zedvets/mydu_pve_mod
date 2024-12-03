using System;

namespace Mod.DynamicEncounters.Overrides.Common;

public static class Config
{
    public static string GetPveModBaseUrl()
    {
        return Environment.GetEnvironmentVariable("DYNAMIC_ENCOUNTERS_URL") ??
               "http://moddynamicencounters:8080";
    }
}