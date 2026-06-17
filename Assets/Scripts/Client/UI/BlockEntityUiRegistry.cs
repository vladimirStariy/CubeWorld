using System.Collections.Generic;
using UnityEngine;

public sealed class BlockEntityUiRegistry
{
    private readonly List<IBlockEntityUiProvider> providers = new();

    public void Register(IBlockEntityUiProvider provider)
    {
        if (provider != null && !providers.Contains(provider))
        {
            providers.Add(provider);
        }
    }

    public bool TryGetProvider(Vector3Int blockPosition, BlockWorldServer server, out IBlockEntityUiProvider provider)
    {
        for (int i = 0; i < providers.Count; i++)
        {
            var candidate = providers[i];
            if (candidate != null && candidate.CanOpen(blockPosition, server))
            {
                provider = candidate;
                return true;
            }
        }

        provider = null;
        return false;
    }
}
