#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.AddressableAssets;

namespace NATMP.Gameplay.Maze.Editor
{
    /// <summary>
    /// Đăng ký (hoặc cập nhật) một file .bytes trong project làm Addressable với địa chỉ cố định.
    /// </summary>
    public static class MazeAddressablesMapRegistration
    {
        public static bool TryRegister(string assetProjectPath, string address, out string errorMessage)
        {
            errorMessage = null;
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                errorMessage =
                    "Chưa có Addressables settings. Window → Asset Management → Addressables → Groups → Create Addressables Settings.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(address))
            {
                errorMessage = "Địa chỉ Addressables rỗng.";
                return false;
            }

            var guid = AssetDatabase.AssetPathToGUID(assetProjectPath);
            if (string.IsNullOrEmpty(guid))
            {
                errorMessage = $"Không tìm thấy asset (cần Refresh sau khi ghi file): {assetProjectPath}";
                return false;
            }

            var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup, false, false);
            entry.SetAddress(address.Trim());
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            return true;
        }

        public static void TryRemoveEntryForAssetPath(string assetProjectPath)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null || string.IsNullOrEmpty(assetProjectPath))
                return;

            var guid = AssetDatabase.AssetPathToGUID(assetProjectPath);
            if (string.IsNullOrEmpty(guid))
                return;

            if (settings.FindAssetEntry(guid) != null)
                settings.RemoveAssetEntry(guid, postEvent: false);
        }

        public static void DeleteAllMazeMapBytesUnderDefaultFolder()
        {
            var folder = MazeAddressableMapConstants.ProjectRelativeFolder;
            if (!AssetDatabase.IsValidFolder(folder))
                return;

            var guids = AssetDatabase.FindAssets("", new[] { folder });
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            foreach (var assetGuid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(assetGuid);
                if (!path.EndsWith(".bytes", StringComparison.OrdinalIgnoreCase))
                    continue;

                TryRemoveEntryForAssetPath(path);
                AssetDatabase.DeleteAsset(path);
            }

            if (settings != null)
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
#endif
