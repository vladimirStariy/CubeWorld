using System.Collections.Generic;
using UnityEngine;

public sealed class ItemUseRegistry
{
    private readonly List<IItemUseProvider> providers = new();

    public void Register(IItemUseProvider provider)
    {
        if (provider != null && !providers.Contains(provider))
        {
            providers.Add(provider);
        }
    }

    public bool TryUse(
        Vector3Int hitBlock,
        Vector3 faceNormal,
        HotbarItem item,
        IItemUseContext context,
        ItemRegistry items,
        out string message)
    {
        message = null;
        for (int i = 0; i < providers.Count; i++)
        {
            var provider = providers[i];
            if (provider == null || !provider.CanUse(item, items))
            {
                continue;
            }

            if (provider.TryUse(hitBlock, faceNormal, item, items, context, out message))
            {
                return true;
            }
        }

        return false;
    }
}
