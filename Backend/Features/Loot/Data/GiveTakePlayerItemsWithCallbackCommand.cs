using System.Collections.Generic;
using NQ;

namespace Mod.DynamicEncounters.Features.Loot.Data;

public class GiveTakePlayerItemsWithCallbackCommand(
    PlayerId playerId,
    IEnumerable<ElementQuantityRef> items,
    EntityId owner,
    Dictionary<string, PropertyValue> properties,
    string onSuccessCallbackUrl,
    string onFailCallbackUrl
)
{
    public PlayerId PlayerId { get; } = playerId;
    public IEnumerable<ElementQuantityRef> Items { get; } = items;
    public EntityId Owner { get; } = owner;
    public Dictionary<string, PropertyValue> Properties { get; set; } = properties;
    public string OnSuccessCallbackUrl { get; set; } = onSuccessCallbackUrl;
    public string OnFailCallbackUrl { get; } = onFailCallbackUrl;
}