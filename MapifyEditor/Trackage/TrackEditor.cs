#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
//using Mapify.Editor.BezierCurves;

namespace Mapify.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Track))]
    public class TrackEditor : UnityEditor.Editor
    {
        private Track[] tracks;
        private Transform trackTransform;

        private float snappingOffsetHeight = 0.2f;

        private void OnEnable()
        {
            tracks = target ? new[] { (Track)target } : targets.Cast<Track>().ToArray();

            MonoBehaviour monoBev = (MonoBehaviour) target;
            trackTransform = monoBev.GetComponent<Transform>();
        }

        public override void OnInspectorGUI()
        {
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
                    RaycastHit hit;
                    Physics.Raycast(child.position, Vector3.down, out hit, Mathf.Infinity);
                    if(hit.collider == null) {
                        Debug.LogError("No collider found below point \"" + child.name + "\"! Move the points upward.");
                        return;
                    }
                    Terrain terrainUnderneath = hit.collider.transform.GetComponent<Terrain>();
                    if(terrainUnderneath == null) {
                        Debug.LogError("No terrain component found on the collider below! Is there something in the way?");
                        return;
                    }

                    Vector3 pos = new Vector3(child.transform.position.x, child.transform.position.y, child.transform.position.z);
                    float terHeight = terrainUnderneath.SampleHeight(pos);

                    child.position = new Vector3(child.position.x, terHeight + snappingOffsetHeight, child.position.z);
                }
                Debug.Log("Snapped To Terrain!");
            }

            snappingOffsetHeight = EditorGUILayout.FloatField("Offset Height", snappingOffsetHeight);

            GUILayout.Space(10);

            // this is unused and bad, but i want to make it used and good later on
            //if(GUILayout.Button("Subdivide")) {
            //    foreach(Transform child in trackTransform.transform) {
            //        //GetTBetweenPoints()
            //        BezierCurve.GetLinearPoint;
            //    }
            //    Debug.Log("Subdivided");
            //}

            GUILayout.Space(10);
        }
    }
}
#endif
