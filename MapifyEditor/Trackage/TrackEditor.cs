#if UNITY_EDITOR
using Mapify.Editor.BezierCurves;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
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

                //Vector2 p1 = new Vector2(1, 1);
                //Vector2 p2 = new Vector2(1, -1);
                //Vector2 p3 = new Vector2(-1, -1);
                //Vector2 p4 = new Vector2(-1, 1);
                //Vector2 toCheck = new Vector2(0, 0);

                //if(PointInRectangle(p1, p2, p3, p4, toCheck)) {
                //    Debug.Log("THE POINT IS IN");
                //} else {
                //    Debug.Log("something's fucked");
                //}

                // For each point on the curve
                for(int i = 0; i < points.Length; i++) {
                    Terrain terrain = GetTerrainBelowPos(points[i]);

                    terrainsBelowPoints[i] = terrain;
                    var treeInstancesList = new List<TreeInstance>(terrain.terrainData.treeInstances);

                    if(i < points.Length - 1) {
                        Vector2 lineVectorDirection = new Vector2(points[i].x - points[i + 1].x, points[i].z - points[i + 1].z).normalized;
                        Vector3 perpendicularVector = new Vector3(Vector2.Perpendicular(lineVectorDirection).x, 0, Vector2.Perpendicular(lineVectorDirection).y);

                        Vector3 point1 = new Vector3(points[i].x, points[i].y, points[i].z) + perpendicularVector * treeClearRadius;
                        Vector3 point2 = new Vector3(points[i].x, points[i].y, points[i].z) + perpendicularVector * -treeClearRadius;
                        Vector3 point3 = new Vector3(points[i + 1].x, points[i + 1].y, points[i + 1].z) + perpendicularVector * treeClearRadius;
                        Vector3 point4 = new Vector3(points[i + 1].x, points[i + 1].y, points[i + 1].z) + perpendicularVector * -treeClearRadius;

                        Vector2 point12D = new Vector2(point1.x, point1.z);
                        Vector2 point22D = new Vector2(point2.x, point2.z);
                        Vector2 point32D = new Vector2(point3.x, point3.z);
                        Vector2 point42D = new Vector2(point4.x, point4.z);

                        // Front/Back
                        //Debug.DrawLine(point1, point2, UnityEngine.Color.black, 2);
                        //Debug.DrawLine(point3, point4, UnityEngine.Color.black, 2);
                        // Sides
                        //Debug.DrawLine(point1, point3, UnityEngine.Color.red, 2);
                        //Debug.DrawLine(point4, point2, UnityEngine.Color.red, 2);
                        //Debug.DrawLine(points[i], points[i + 1], UnityEngine.Color.magenta, 20);

                        // For Each Tree
                        for(int j = 0; j < treeInstancesList.Count; j++) {
                            float terrainSize = terrain.terrainData.size.x;
                            Vector3 pos = (treeInstancesList[j].position * terrainSize) + terrain.transform.position + new Vector3(0, 40, 0);

                            Vector2 pointToCheck = new Vector2(pos.x, pos.z);
                            Vector3 posSameY = new Vector3(pointToCheck.x, 0, pointToCheck.y);

                            if(PointInRectangle(point12D, point22D, point42D, point32D, pointToCheck)) {
                                treeInstancesList.RemoveAt(j);
                            }
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

        // This check if the terrain is square isn't needed, but I thought it might be nice to put in here for now. I'll probably end up removing it later.
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

        bool PointInRectangle(Vector2 A, Vector2 B, Vector2 C, Vector2 D, Vector2 m) {
            Vector2 AB = vect2d(A, B); float C1 = -1 * (AB.y * A.x + AB.x * A.y);
            float D1 = (AB.y * m.x + AB.x * m.y) + C1;
            Vector2 AD = vect2d(A, D); float C2 = -1 * (AD.y * A.x + AD.x * A.y);
            float D2 = (AD.y * m.x + AD.x * m.y) + C2;
            Vector2 BC = vect2d(B, C); float C3 = -1 * (BC.y * B.x + BC.x * B.y);
            float D3 = (BC.y * m.x + BC.x * m.y) + C3;
            Vector2 CD = vect2d(C, D); float C4 = -1 * (CD.y * C.x + CD.x * C.y);
            float D4 = (CD.y * m.x + CD.x * m.y) + C4;

            return 0 >= D1 && 0 >= D4 && 0 <= D2 && 0 >= D3;
        }

        // This is a terrible, undescriptive name but it's what stackexchange named it :upside_down: I'll fix it later because I don't actually know what id does right now
        Vector2 vect2d(Vector2 p1, Vector2 p2) {
            Vector2 temp;
            temp.x = (p2.x - p1.x);
            temp.y = -1 * (p2.y - p1.y);
            return temp;
        }

        public Terrain GetTerrainBelowPos(Vector3 position) {
            RaycastHit hit;
            Physics.Raycast(position, Vector3.down, out hit, Mathf.Infinity);
            if(hit.collider == null) {
                Debug.LogError("Not all points found a collider below " + position + "! Move the points upward, and make sure none of them are intersecting with the ground.");
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
