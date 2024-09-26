﻿namespace Mod.DynamicEncounters.Overrides.Common;

public static class Resources
{
    private const string Namespace = "Mod.DynamicEncounters.Overrides.Resources"; 
    public static string CommonJs => ResourceLoader
        .GetStringContents($"{Namespace}.common.js");
    public static string CreateRootDivJs => ResourceLoader
        .GetStringContents($"{Namespace}.create-root-div.js");
    public static string NpcAppJs => ResourceLoader
        .GetStringContents($"{Namespace}.npc-app.js");
    public static string NpcAppCss => ResourceLoader
        .GetStringContents($"{Namespace}.npc-app.css");
}