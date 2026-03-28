using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace NATMP.Gameplay.Maze
{
    /// <summary>
    /// Load layout manual qua Addressables dạng <see cref="TextAsset"/> (.bytes import trong Unity).
    /// </summary>
    public static class MazeManualLayoutAddressables
    {
        public static bool TryLoad(string address, int expectedWidth, int expectedHeight, out bool[,] walkable, out Vector2Int bugStart)
        {
            walkable = null;
            bugStart = new Vector2Int(2, 2);
            if (string.IsNullOrWhiteSpace(address))
                return false;

            address = address.Trim();
            AsyncOperationHandle<TextAsset> handle = default;
            try
            {
                handle = Addressables.LoadAssetAsync<TextAsset>(address);
                var ta = handle.WaitForCompletion();
                if (ta == null || ta.bytes == null || ta.bytes.Length == 0)
                {
                    if (handle.IsValid())
                        Addressables.Release(handle);
                    return false;
                }

                bool ok = MazeManualLayoutBinary.TryParse(ta.bytes, expectedWidth, expectedHeight, out walkable, out bugStart);
                if (handle.IsValid())
                    Addressables.Release(handle);
                return ok;
            }
            catch (Exception e)
            {
                UnityLogger.LogWarning($"[MazeManualLayoutAddressables] Không load được '{address}': {e.Message}");
                if (handle.IsValid())
                    Addressables.Release(handle);
                return false;
            }
        }
    }
}
