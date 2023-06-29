﻿#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using Mapify.Editor.StateUpdaters;
using Mapify.Editor.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CompressionLevel = System.IO.Compression.CompressionLevel;
using Object = UnityEngine.Object;

namespace Mapify.Editor
{
    public static class MapExporter
    {
        private const string DEFAULT_MAPS_FOLDER_PATH = "steamapps/common/Derail Valley/BepInEx/content/maps";

        private static string LastReleaseExportPath {
            get => EditorPrefs.GetString("Mapify.Export.Release.LastExportPath");
            set => EditorPrefs.SetString("Mapify.Export.Release.LastExportPath", value);
        }

        private static string LastDebugExportPath {
            get => EditorPrefs.GetString("Mapify.Export.Debug.LastExportPath");
            set => EditorPrefs.SetString("Mapify.Export.Debug.LastExportPath", value);
        }

        public static void OpenExportPrompt(bool releaseMode)
        {
            string mapName = EditorAssets.FindAsset<MapInfo>()?.mapName;
            if (releaseMode)
                ExportRelease(mapName);
            else
                ExportDebug(mapName);
        }

        private static void ExportRelease(string mapName)
        {
            string startingPath;
            string name;
            if (!string.IsNullOrEmpty(LastReleaseExportPath) && Directory.GetParent(LastReleaseExportPath)?.Exists == true)
            {
                startingPath = Path.GetDirectoryName(LastReleaseExportPath);
                name = Path.GetFileName(LastReleaseExportPath);
            }
            else
            {
                startingPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                name = $"{mapName}.zip";
            }

            string exportFilePath = EditorUtility.SaveFilePanel("Export Map", startingPath, name, "zip");
            if (string.IsNullOrWhiteSpace(exportFilePath))
                return;
            LastReleaseExportPath = exportFilePath;

            string tmpFolder = Path.Combine(Path.GetTempPath(), $"dv-mapify-{Path.GetRandomFileName()}");
            string bepInExDir = Path.Combine(tmpFolder, "BepInEx");
            string exportDir = Path.Combine(bepInExDir, "content", "maps", mapName);
            if (Directory.Exists(tmpFolder))
                Directory.Delete(tmpFolder, true);
            Directory.CreateDirectory(exportDir);

            bool success = Export(exportDir, false);

            if (success)
            {
                EditorUtility.DisplayProgressBar("Mapify", "Creating zip file", 0);
                ZipFile.CreateFromDirectory(bepInExDir, exportFilePath, CompressionLevel.NoCompression, true);
                EditorUtility.ClearProgressBar();
                if (EditorUtility.DisplayDialog("Mapify", "Export complete!", "Open Folder", "Ok"))
                    EditorUtility.RevealInFinder(exportFilePath);
            }

            if (Directory.Exists(tmpFolder))
                Directory.Delete(tmpFolder, true);
        }

        private static void ExportDebug(string mapName)
        {
            string startingPath;
            string name;
            if (!string.IsNullOrEmpty(LastDebugExportPath) && Directory.Exists(LastDebugExportPath))
            {
                startingPath = Path.GetDirectoryName(LastDebugExportPath);
                name = Path.GetFileName(LastDebugExportPath);
            }
            else
            {
                startingPath = GetDefaultMapsFolder();
                name = mapName;
            }

            string exportFolderPath = EditorUtility.SaveFolderPanel("Export Map", startingPath, name);
            if (string.IsNullOrWhiteSpace(exportFolderPath))
                return;
            LastDebugExportPath = exportFolderPath;

            bool success = Export(exportFolderPath, true);

            if (success && EditorUtility.DisplayDialog("Mapify", "Export complete!", "Open Folder", "Ok"))
                EditorUtility.RevealInFinder(exportFolderPath);
        }

        private static bool Export(string exportFolderPath, bool uncompressed)
        {
            DirectoryInfo directory = new DirectoryInfo(exportFolderPath);

            if (directory.GetFiles().Length > 0 || directory.GetDirectories().Length > 0)
            {
                int result = EditorUtility.DisplayDialogComplex("Clear Folder",
                    "The directory you selected isn't empty, would you like to clear the files from the folder before proceeding? \n \n WARNING: THIS WILL DELETE ALL FILES (EXCLUDING DIRECTORIES) IN THE FOLDER.",
                    "Clear Folder",
                    "Cancel",
                    "Skip");
                switch (result)
                {
                    case 0:
                        foreach (FileInfo file in directory.GetFiles())
                            file.Delete();
                        break;
                    case 1:
                        return false;
                }
            }

            BuildUpdater.Update();

            AssetBundleBuild[] builds = CreateBuilds(EditorSceneManager.GetSceneByPath(Scenes.TERRAIN));

            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
                exportFolderPath,
                builds,
                uncompressed ? BuildAssetBundleOptions.UncompressedAssetBundle : BuildAssetBundleOptions.None,
                BuildTarget.StandaloneWindows64
            );
            bool success = manifest != null;

            BuildUpdater.Cleanup();

            string mapInfoPath = Path.Combine(exportFolderPath, Names.MAP_INFO_FILE);
            if (!success)
            {
                Debug.LogWarning("Build was canceled or failed!");
                File.Delete(mapInfoPath); // Prevents the mod from loading an incomplete asset bundle
            }
            else
            {
                using (StreamWriter writer = File.CreateText(mapInfoPath))
                {
                    MapInfo mapInfo = EditorAssets.FindAsset<MapInfo>();
                    string version = File.ReadLines("Assets/Mapify/version.txt").First().Trim();
                    writer.Write(JsonUtility.ToJson(new BasicMapInfo(mapInfo.mapName, version)));
                }
            }

            return success;
        }

        private static AssetBundleBuild[] CreateBuilds(Scene terrainScene)
        {
            Terrain[] sortedTerrain = terrainScene.GetAllComponents<Terrain>()
                .Where(terrain => terrain.gameObject.activeInHierarchy)
                .ToArray()
                .Sort();

            List<AssetBundleBuild> builds = new List<AssetBundleBuild>(sortedTerrain.Length + 2);
            for (int i = 0; i < sortedTerrain.Length; i++)
                builds.Add(new AssetBundleBuild {
                    assetBundleName = $"terraindata_{i}",
                    assetNames = new[] { AssetDatabase.GetAssetPath(sortedTerrain[i].terrainData) }
                });

            string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
            List<string> assetPaths = new List<string>(allAssetPaths.Length - sortedTerrain.Length);
            List<string> scenePaths = new List<string>();
            for (int i = 0; i < allAssetPaths.Length; i++)
            {
                string assetPath = allAssetPaths[i];
                if (!assetPath.StartsWith("Assets/")) continue;
                AssetImporter importer = AssetImporter.GetAtPath(assetPath);
                if (importer == null || importer.GetType() == typeof(MonoImporter)) continue;
                Object obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                if (obj is TerrainData) continue;
                (obj is SceneAsset ? scenePaths : assetPaths).Add(assetPath);

                EditorUtility.DisplayProgressBar("Gathering assets", assetPath, i / (float)allAssetPaths.Length);
            }

            EditorUtility.ClearProgressBar();

            builds.Add(new AssetBundleBuild {
                assetBundleName = Names.ASSETS_ASSET_BUNDLE,
                assetNames = assetPaths.ToArray()
            });
            builds.Add(new AssetBundleBuild {
                assetBundleName = Names.SCENES_ASSET_BUNDLE,
                assetNames = scenePaths.ToArray()
            });

            return builds.ToArray();
        }

        private static string GetDefaultMapsFolder()
        {
            // search for the user's DV install
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return GetWindowsMapsFolder();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return GetLinuxMapsFolder();
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to find default save path");
                Debug.LogException(e);
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        private static string GetWindowsMapsFolder()
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                string driveRoot = drive.RootDirectory.FullName;
                string potentialPath = Path.Combine(driveRoot, "Program Files", "Steam", DEFAULT_MAPS_FOLDER_PATH);
                if (Directory.Exists(potentialPath)) return potentialPath;

                potentialPath = Path.Combine(driveRoot, "Program Files (x86)", "Steam", DEFAULT_MAPS_FOLDER_PATH);
                if (Directory.Exists(potentialPath)) return potentialPath;
            }

            return null;
        }

        private static string GetLinuxMapsFolder()
        {
            string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string potentialPath = Path.Combine(homePath, ".steam", "steam", DEFAULT_MAPS_FOLDER_PATH);
            return Directory.Exists(potentialPath) ? potentialPath : null;
        }
    }
}
#endif
