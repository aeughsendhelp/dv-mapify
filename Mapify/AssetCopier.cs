using System.Collections.Generic;
using Mapify.Editor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mapify
{
    public static class AssetCopier
    {
        private static readonly Dictionary<VanillaAsset, GameObject> prefabs = new Dictionary<VanillaAsset, GameObject>(4);

        public static IEnumerable<VanillaAsset> InstantiatableAssets => prefabs.Keys;

        public static GameObject Instantiate(VanillaAsset asset, bool active = true)
        {
            GameObject go = GameObject.Instantiate(prefabs[asset], WorldMover.Instance.originShiftParent);
            go.SetActive(active);
            return go;
        }

        public static void CopyDefaultAssets(Scene scene, ToSave func)
        {
            if (!scene.isLoaded)
            {
                Main.Logger.Error($"Vanilla scene {scene.name} isn't loaded!");
                return;
            }

            Main.Logger.Log($"Copying default assets from vanilla scene {scene.name}");

            GameObject[] rootObjects = scene.GetRootGameObjects();
            foreach (GameObject rootObject in rootObjects)
            {
                rootObject.SetActive(false);
                SceneManager.MoveGameObjectToScene(rootObject, SceneManager.GetActiveScene());

                IEnumerator<(VanillaAsset, GameObject)> enumerator = func.Invoke(rootObject);
                while (enumerator.MoveNext())
                {
                    (VanillaAsset vanillaAsset, GameObject gameObject) = enumerator.Current;
                    if (prefabs.ContainsKey(vanillaAsset)) continue;
                    gameObject.SetActive(false);
                    gameObject.transform.SetParent(null);
                    gameObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    prefabs.Add(vanillaAsset, gameObject);
                }

                GameObject.Destroy(rootObject);
            }

            Main.Logger.Log($"Unloading vanilla scene {scene.name}");
            // cope
#pragma warning disable CS0618
            SceneManager.UnloadScene(scene);
#pragma warning restore CS0618
        }

        public delegate IEnumerator<(VanillaAsset, GameObject)> ToSave(GameObject gameObject);
    }
}
