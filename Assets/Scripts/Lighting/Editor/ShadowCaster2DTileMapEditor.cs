using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ShadowCaster2DTileMap))]
public class ShadowCaster2DTileMapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector fields
        DrawDefaultInspector();

        // Get a reference to the script instance
        ShadowCaster2DTileMap generator = (ShadowCaster2DTileMap)target;

        // Add some space
        EditorGUILayout.Space();

        // Add the Generate button
        if (GUILayout.Button("Generate Shadows"))
        {
            generator.Generate();
            // Optional: Mark scene as dirty to ensure changes are saved
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
            }
        }

        // Add the Destroy button
        if (GUILayout.Button("Destroy Shadows"))
        {
            generator.DestroyAllChildren();
             // Optional: Mark scene as dirty to ensure changes are saved
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
            }
        }
    }
} 