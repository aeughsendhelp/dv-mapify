﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mapify.Editor.Utils
{
    public static class Extensions
    {
        public static void Select(this GameObject gameObject)
        {
            Selection.objects = new Object[] { gameObject };
        }

        public static List<T> ToList<T>(this IEnumerator<T> e)
        {
            List<T> list = new List<T>();
            while (e.MoveNext()) list.Add(e.Current);
            return list;
        }

        public static float CalculateWorldSize(this IEnumerable<Terrain> terrains)
        {
            float maxX = 0f;
            float maxZ = 0f;

            foreach (Terrain terrain in terrains)
            {
                Vector3 terrainSize = terrain.terrainData.size;
                Vector3 position = terrain.transform.position;
                float terrainMaxX = position.x + terrainSize.x;
                float terrainMaxZ = position.z + terrainSize.z;
                if (terrainMaxX > maxX) maxX = terrainMaxX;
                if (terrainMaxZ > maxZ) maxZ = terrainMaxZ;
            }

            return Mathf.Max(maxX, maxZ);
        }

        public static T GetComponentInSelfOrParent<T>(this Component component)
        {
            return component.gameObject.GetComponentInSelfOrParent<T>();
        }

        public static T GetComponentInSelfOrParent<T>(this GameObject gameObject)
        {
            T self = gameObject.GetComponent<T>();
            return self != null ? self : gameObject.GetComponentInParent<T>();
        }

        public static Dictionary<Station, List<T>> MapToClosestStation<T>(this IEnumerable<T> arr) where T : Component
        {
            return arr
                .GroupBy(spawner => spawner.gameObject.GetClosestComponent<Station>())
                .Where(group => group.Key != null)
                .ToDictionary(group => group.Key, group => group.ToList());
        }

        public static T GetClosestComponent<T>(this GameObject gameObject) where T : Component
        {
            return Object.FindObjectsOfType<T>()
                .OrderBy(c => (gameObject.transform.position - c.transform.position).sqrMagnitude)
                .FirstOrDefault();
        }

        public static T RecordObjectChanges<T>(this List<Object> objects, Func<T> func)
        {
            Undo.IncrementCurrentGroup();
            Undo.RecordObjects(objects.ToArray(), "Map Validation");

            T result = func.Invoke();

            foreach (Object o in objects.Where(PrefabUtility.IsPartOfPrefabInstance))
                PrefabUtility.RecordPrefabInstancePropertyModifications(o);

            EditorSceneManager.SaveOpenScenes();
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            return result;
        }

        public static BezierCurve Curve(this BezierPoint point)
        {
            if (point._curve != null) return point._curve;
            BezierCurve curve = point.GetComponentInParent<BezierCurve>();
            point._curve = curve;
            return curve;
        }
    }
}
