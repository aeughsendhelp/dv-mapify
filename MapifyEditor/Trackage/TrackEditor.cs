#if UNITY_EDITOR
using Mapify.Editor.BezierCurves;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
//using Mapify.Editor.BezierCurves;

namespace Mapify.Editor {
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Track))]
    public class TrackEditor : UnityEditor.Editor {
        private Track[] tracks;
        private Transform trackTransform;

        private float snappingOffsetHeight = 0.2f;
        private float treeClearRadius = 5;
        private int clearSubdivisions = 10;

        private void OnEnable() {
            tracks = target ? new[] { (Track) target } : targets.Cast<Track>().ToArray();

            MonoBehaviour monoBev = (MonoBehaviour) target;
            trackTransform = monoBev.GetComponent<Transform>();
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            GUILayout.Space(10);
            if(GUILayout.Button("Generate Track Name")) {
                foreach(Track track in tracks) {
                    Undo.RecordObject(track.gameObject, "Generate Track Name");
                    track.name = track.LogicName;
                }
            }

            GUILayout.Space(10);

            if(GUILayout.Button("Snap To Terrain")) {
                foreach(Transform child in trackTransform.transform) {
                    Terrain terrain = GetTerrainBelowPos(child.position);
                    if(terrain == null) { return; }

                    Vector3 pos = new Vector3(child.transform.position.x, child.transform.position.y, child.transform.position.z);
                    float terHeight = terrain.SampleHeight(pos);
                    child.position = new Vector3(child.position.x, terHeight + snappingOffsetHeight, child.position.z);
                }
                Debug.Log("Snapped To Terrain!");
            }
            snappingOffsetHeight = EditorGUILayout.FloatField("Offset Height", snappingOffsetHeight);

            GUILayout.Space(10);

            // This should be later updated to remove all objects along tracks, not just trees, but I don't feel like bothering with that right now.
            if(GUILayout.Button("Remove Trees Along Track")) {
                Vector3[] points = GetSubdividedPointsOnCurve(trackTransform.GetComponent<BezierCurve>(), clearSubdivisions);

                Terrain[] terrainsBelowPoints = new Terrain[points.Length];

                for(int i = 0; i < points.Length; i++) {
                    Terrain terrain = GetTerrainBelowPos(points[i]);

                    // This check if the terrain is square isn't needed, but I thought it might be nice to put in here for now. I'll probably end up removing it later.

                    terrainsBelowPoints[i] = terrain;
                    var treeInstancesList = new List<TreeInstance>(terrain.terrainData.treeInstances);

                    for(int j = 0; j < treeInstancesList.Count; j++) {
                        float terrainSize = terrain.terrainData.size.x;
                        Vector3 pos = (treeInstancesList[j].position * terrainSize) + terrain.transform.position + new Vector3(0, 40, 0);
                        Vector3 posSameY = new Vector3(pos.x, points[i].y, pos.z);

                        if(j < points.Length - 1) {
                            Debug.DrawLine(points[j], points[j + 1], Color.magenta, 20);
                        }

                        Debug.DrawRay(posSameY, Vector3.down * 30, Color.red, 20);

                        if(Vector3.Distance(points[i], posSameY) <= treeClearRadius) {
                            Debug.Log("h");
                            //Debug.DrawRay(posSameY, Vector3.down * 30, Color.red, 20);
                            //Debug.DrawRay(points[j], Vector3.down * 30, Color.red, 20);

                            // Removes the tree from the list
                            treeInstancesList.RemoveAt(i);
                            
                        }
                    }

                    terrain.terrainData.treeInstances = treeInstancesList.ToArray();
                }

                Debug.Log("Removed trees along track " + trackTransform.name);
            }
            treeClearRadius = EditorGUILayout.FloatField("Clear Radius", treeClearRadius);
            clearSubdivisions = EditorGUILayout.IntField("Subdivisions", clearSubdivisions);

            GUILayout.Space(10);

            // this is unused and bad, but i'd like to make it used and good later on
            //if(GUILayout.Button("Subdivide")) {
            //    foreach(Transform child in trackTransform.transform) {
            //        //GetTBetweenPoints()
            //        BezierCurve.GetLinearPoint;
            //    }
            //    Debug.Log("Subdivided");
            //}

            GUILayout.Space(10);
        }

        public bool IsTerrainSquare(Terrain terrain) {
            int terrainSize = 0;
            float terX = terrain.terrainData.size.x;
            float terZ = terrain.terrainData.size.z;
            if(terX == terZ) {
                terrainSize = (int) terX;
                return true;
            } else {
                Debug.LogError("Terrain size is not square! " + terX + " by " + terZ);
                return false;
            }
        }

        public bool PointInRectangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P) {
            // Compute vectors        
            Vector2 v0 = C - A;
            Vector2 v1 = B - A;
            Vector2 v2 = P - A;

            // Compute dot products
            float dot00 = Vector2.Dot(v0, v0);
            float dot01 = Vector2.Dot(v0, v1);
            float dot02 = Vector2.Dot(v0, v2);
            float dot11 = Vector2.Dot(v1, v1);
            float dot12 = Vector2.Dot(v1, v2);

            // Compute barycentric coordinates
            float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
            float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

            // Check if point is in rectangle
            if(u >= 0 && v >= 0 && u <= 1 && v <= 1) {
                return true;
            } else {
                return false;
            }
        }

        // This function allows for rotation of a bounding box and then checking if a point is in that box.
        public bool IsInBoundingBox(float xO, float yO, float xW, float yW, float xH, float yH, /**/ float x, float y) {
            float xU = xW - xO;
            float yU = yW - yO;
            float xV = xH - xO;
            float yV = yH - yO;
            float L = xU * yV - xV * yU;

            if(L < 0) {
                L = -L;
                xU = -xU;
                yV = -yV;
            } else {
                xV = -xV;
                yU = -yU;
            }

            float u = (x - xO) * yV + (y - yO) * xV;

            if(u < 0 || u > L) {
                return false;
            } else {
                float v = (x - xO) * yU + (y - yO) * xU;
                if(v < 0 || v > L) {
                    return false;
                } else {
                    return true;
                }
            }
        }

        public Terrain GetTerrainBelowPos(Vector3 position) {
            RaycastHit hit;
            Physics.Raycast(position, Vector3.down, out hit, Mathf.Infinity);
            if(hit.collider == null) {
                Debug.LogError("No collider found below " + position + "! Move the points upward.");
                return null;
            }
            Terrain terrainFound = hit.collider.transform.GetComponent<Terrain>();
            if(terrainFound == null) {
                Debug.LogError("No terrain component found on the collider below! Is there something in the way?");
                return null;
            }

            return terrainFound;
        }

        // Needs to subdivide, not just get closer and closer to 0
        public Vector3[] GetSubdividedPointsOnCurve(BezierCurve curve, int iterations) {
            int stepNum = (int) Mathf.Pow(2, iterations) + 1;
            float[] pointsOnLine = linspace(0, 1, stepNum);

            var toReturnList = new List<Vector3>();

            for (int i = 0; i < pointsOnLine.Length; i++) {
                Vector3 point = curve.GetPointAt(pointsOnLine[i]);
                toReturnList.Add(point);
            }

            return toReturnList.ToArray();
        }

        public static float[] linspace(float startval, float endval, int steps) {
            float interval = (endval / Mathf.Abs(endval)) * Mathf.Abs(endval - startval) / (steps - 1);
            return (from val in Enumerable.Range(0, steps)
                    select startval + (val * interval)).ToArray();
        }
    }
}
#endif
