using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ParallelCircuitAnimator))]
[CanEditMultipleObjects]
public class ParallelCircuitAnimatorEditor : Editor
{
    SerializedProperty pathPointsProp;

    void OnEnable()
    {
        pathPointsProp = serializedObject.FindProperty("pathPoints");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw all properties except pathPoints with default layout
        DrawPropertiesExcluding(serializedObject, "pathPoints");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Animation Path Points", EditorStyles.boldLabel);

        if (pathPointsProp == null)
        {
            EditorGUILayout.HelpBox("Path points property not found.", MessageType.Error);
        }
        else
        {
            EditorGUI.indentLevel++;
            // allow resizing the list
            EditorGUILayout.PropertyField(pathPointsProp.FindPropertyRelative("Array.size"), new GUIContent("Size"));
            for (int i = 0; i < pathPointsProp.arraySize; i++)
            {
                var element = pathPointsProp.GetArrayElementAtIndex(i);
                if (element == null) continue;

                // Header for each PathPoint
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Point [{i}]", EditorStyles.boldLabel);

                var pointProp = element.FindPropertyRelative("point");
                var typeProp = element.FindPropertyRelative("type");
                var branchPathPointsProp = element.FindPropertyRelative("branchPathPoints");
                var branchResistorIndicesProp = element.FindPropertyRelative("branchResistorIndices");

                EditorGUILayout.PropertyField(pointProp, new GUIContent("Waypoint"));
                EditorGUILayout.PropertyField(typeProp, new GUIContent("Type"));

                // If this PathPoint is a Split, show exactly 3 branch slots
                if (typeProp.enumValueIndex == (int)PathPointType.Split)
                {
                    // enforce 3 entries for editor convenience
                    if (branchPathPointsProp != null && branchPathPointsProp.arraySize != 3)
                        branchPathPointsProp.arraySize = 3;
                    if (branchResistorIndicesProp != null && branchResistorIndicesProp.arraySize != 3)
                        branchResistorIndicesProp.arraySize = 3;

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Branches (3)", EditorStyles.miniBoldLabel);

                    for (int b = 0; b < 3; b++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        if (branchPathPointsProp != null)
                        {
                            var bp = branchPathPointsProp.GetArrayElementAtIndex(b);
                            EditorGUILayout.PropertyField(bp, new GUIContent($"Branch {b + 1} Root"));
                        }
                        if (branchResistorIndicesProp != null)
                        {
                            var ri = branchResistorIndicesProp.GetArrayElementAtIndex(b);
                            EditorGUILayout.PropertyField(ri, new GUIContent($"Resistor Index"));
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    // For non-split points show branch arrays collapsed (optional)
                    if (branchPathPointsProp != null)
                        EditorGUILayout.PropertyField(branchPathPointsProp, new GUIContent("Branch Path Points"), true);
                    if (branchResistorIndicesProp != null)
                        EditorGUILayout.PropertyField(branchResistorIndicesProp, new GUIContent("Branch Resistor Indices"), true);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}