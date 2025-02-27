using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace Mapify.Editor.Utils
{
    public static class EditorHelper
    {
        #region COLOURS

        // Turns out colour isn't limited to [0..1] range so larger values can make it brighter.
        public static readonly Color Accept = new Color(0.50f, 1.80f, 0.75f);
        public static readonly Color Warning = new Color(2.00f, 1.50f, 0.25f);
        public static readonly Color Cancel = new Color(2.00f, 0.75f, 0.75f);

        #endregion

        // Unity doesn't have a GUI MinMaxSlider for ints, so this is a workaround.
        public static (int Min, int Max) MinMaxSliderInt(GUIContent label, int minValue, int maxValue, int minLimit, int maxLimit, params GUILayoutOption[] options)
        {
            float minTemp = minValue;
            float maxTemp = maxValue;

            EditorGUILayout.MinMaxSlider(label, ref minTemp, ref maxTemp, minLimit, maxLimit, options);

            return (Mathf.RoundToInt(minTemp), Mathf.RoundToInt(maxTemp));
        }

        public static char CharField(GUIContent label, char c, params GUILayoutOption[] options)
        {
            string text = EditorGUILayout.TextField(label, c.ToString(), options);

            if (string.IsNullOrEmpty(text))
            {
                return '\0';
            }

            return text[0];
        }

        public static void DrawBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int samples = 8)
        {
            Handles.DrawPolyLine(MathHelper.SampleBezier(p0, p1, p2, p3, samples));
        }

        public static bool MultipleSelectionFoldout<T>(string foldoutName, string identifier, bool show, T[] objects, int max = -1) where T : UnityEngine.Object
        {
            if (max > objects.Length || max == -1)
            {
                max = objects.Length;
            }

            show = EditorGUILayout.Foldout(show, $"{foldoutName} ({(max != objects.Length ? $"{max}/" : "")}{objects.Length})");

            if (!show)
            {
                return false;
            }

            EditorGUI.indentLevel++;

            for (int i = 0; i < max; i++)
            {
                EditorGUILayout.ObjectField(
                    new GUIContent($"{identifier} {i}"),
                    objects[i], typeof(T), true);
            }

            EditorGUI.indentLevel--;

            return true;
        }
    }
}
#endif
