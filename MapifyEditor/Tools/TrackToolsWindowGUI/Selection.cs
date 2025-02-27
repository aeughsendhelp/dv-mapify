using Mapify.Editor.Utils;
using UnityEditor;
using UnityEngine;
using static Mapify.Editor.Tools.ToolEnums;

#if UNITY_EDITOR
namespace Mapify.Editor.Tools
{
    public partial class TrackToolsWindow
    {
        private bool _showSelection = true;
        private SelectionType _selectionType = SelectionType.None;
        private Track[] _selectedTracks = new Track[0];
        private BezierPoint[] _selectedPoints = new BezierPoint[0];
        private bool _showTracks = true;
        private bool _showPoints = true;

        // Foldout with info about what we're working with currently.
        private void DrawSelectionFoldout()
        {
            GUI.backgroundColor *= 1.1f;

            _showSelection = EditorGUILayout.BeginFoldoutHeaderGroup(_showSelection,
                new GUIContent("Selection", "Properties of the current selection"),
                null, null);

            GUI.backgroundColor = Color.white;

            if (_showSelection)
            {
                EditorGUI.indentLevel++;

                switch (_selectionType)
                {
                    case SelectionType.Track:
                        DrawTrackSelection();
                        break;
                    case SelectionType.BezierPoint:
                        DrawPointSelection();
                        break;
                    case SelectionType.Switch:
                        DrawSwitchSelection();
                        break;
                    case SelectionType.Turntable:
                        DrawTurntableSelection();
                        break;
                    default:
                        EditorGUILayout.Space();
                        EditorGUILayout.HelpBox("No compatible objects selected! These tools work with the following:\n" +
                            "\u2022 Tracks\n\u2022 BezierPoints\n\u2022 Switches\n\u2022 Turntables", MessageType.Warning);
                        EditorGUILayout.Space();
                        break;
                }

                EditorGUILayout.Space();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawTrackSelection()
        {
            EditorGUILayout.ObjectField(
                new GUIContent($"Current track"),
                CurrentTrack, typeof(Track), true);

            EditorGUILayout.LabelField("Total selected", _selectedTracks.Length.ToString());

            bool isSwitch = CurrentTrack.IsSwitch;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Part of switch", isSwitch.ToString());

            // If it's a switch enable a button to swap between the 2 tracks.
            GUI.enabled = isSwitch;

            if (GUILayout.Button(new GUIContent("Swap tracks", "Swaps the selected track between the through and diverging tracks")) && isSwitch)
            {
                Switch s = CurrentTrack.GetComponentInParent<Switch>();

                if (CurrentTrack == s.ThroughTrack)
                {
                    Selection.activeGameObject = s.DivergingTrack.gameObject;
                    SelectTrack(s.DivergingTrack);
                }
                else
                {
                    Selection.activeGameObject = s.ThroughTrack.gameObject;
                    SelectTrack(s.ThroughTrack);
                }
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Part of turntable", CurrentTrack.IsTurntable.ToString());

            EditorGUILayout.LabelField(new GUIContent("Length", "Length of the track"),
                new GUIContent($"{CurrentTrack.Curve.length:F3}m"));
            EditorGUILayout.LabelField(new GUIContent("Horizontal length", "Length of the track with no vertical changes"),
                new GUIContent($"{CurrentTrack.GetHorizontalLength():F3}m"));

            EditorGUILayout.LabelField("At start");
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField(new GUIContent("Grade", "Grade at the start of the track"),
                new GUIContent($"{CurrentTrack.GetGradeAtStart() * 100.0f:F2}%"));
            EditorGUILayout.LabelField(new GUIContent("North angle", "Angle in relation to the North at start of the track"),
                new GUIContent($"{MathHelper.AngleToNorth(CurrentTrack.Curve[0].globalHandle2 - CurrentTrack.Curve[0].position):F2}°"));

            EditorGUI.indentLevel--;
            EditorGUILayout.LabelField("At end");
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField(new GUIContent("Grade", "Grade at the end of the track"),
                new GUIContent($"{CurrentTrack.GetGradeAtEnd() * 100.0f:F2}%"));
            EditorGUILayout.LabelField(new GUIContent("North angle", "Angle in relation to the North at end of the track"),
                new GUIContent($"{MathHelper.AngleToNorth(CurrentTrack.Curve.Last().position - CurrentTrack.Curve.Last().globalHandle1):F2}°"));

            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField(new GUIContent("Height difference", "Total change in height along the track"),
                new GUIContent($"{CurrentTrack.GetHeightChange():F3}m"));
            EditorGUILayout.LabelField(new GUIContent("Average grade", "Average grade of the track"),
                new GUIContent($"{CurrentTrack.GetAverageGrade() * 100.0f:F2}%"));
        }

        private void DrawPointSelection()
        {
            EditorGUILayout.ObjectField(
                new GUIContent("Current point"),
                CurrentPoint, typeof(BezierPoint), true);

            EditorGUILayout.LabelField("Handle 1");
            EditorGUI.indentLevel++;

            if (CurrentPoint.handle1.sqrMagnitude > 0)
            {
                EditorGUILayout.LabelField(new GUIContent("Grade", "Grade through handle 1"),
                    new GUIContent($"{MathHelper.GetGrade(CurrentPoint.position, CurrentPoint.globalHandle1) * 100.0f:F2}%"));
                EditorGUILayout.LabelField(new GUIContent("North angle", "Angle in relation to the North through handle 1"),
                    new GUIContent($"{MathHelper.AngleToNorth(CurrentPoint.position - CurrentPoint.globalHandle1):F2}°"));
            }
            else
            {
                EditorGUILayout.LabelField("Handle has 0 length!");
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField("Handle 2");
            EditorGUI.indentLevel++;

            if (CurrentPoint.handle2.sqrMagnitude > 0)
            {
                EditorGUILayout.LabelField(new GUIContent("Grade", "Grade through handle 2"),
                    new GUIContent($"{MathHelper.GetGrade(CurrentPoint.position, CurrentPoint.globalHandle2) * 100.0f:F2}%"));
                EditorGUILayout.LabelField(new GUIContent("North angle", "Angle in relation to the North through handle 2"),
                    new GUIContent($"{MathHelper.AngleToNorth(CurrentPoint.position - CurrentPoint.globalHandle2):F2}°"));
            }
            else
            {
                EditorGUILayout.LabelField("Handle has 0 length!");
            }
            EditorGUI.indentLevel--;

            _showPoints = EditorHelper.MultipleSelectionFoldout("Selected points", "BezierPoint", _showPoints, _selectedPoints);
        }

        private void DrawSwitchSelection()
        {
            EditorGUILayout.ObjectField(
                new GUIContent("Current switch"),
                CurrentSwitch, typeof(Switch), true);
        }

        private void DrawTurntableSelection()
        {
            EditorGUILayout.ObjectField(
                new GUIContent("Current turntable"),
                CurrentTurntable, typeof(Turntable), true);
        }
    }
}
#endif
