using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameLogic))]
public class GameLogicEditor : Editor
{
    public override void OnInspectorGUI()
    {

        GameLogic gameLogic = (GameLogic) target;

        GUILayout.Label("Insects", EditorStyles.boldLabel);

        if (GUILayout.Button("Spawn Insects"))
        {
            InsectSpawner insectSpawner = FindFirstObjectByType<InsectSpawner>();
            if (insectSpawner != null)
            {
                insectSpawner.SpawnInsects();
            }
            else
            {
                Debug.LogWarning("No InsectSpawner found in the scene");
            }
        }

        if (GUILayout.Button("Clear Insects"))
        {
            InsectSpawner insectSpawner = FindFirstObjectByType<InsectSpawner>();
            if (insectSpawner != null)
            {
                insectSpawner.RemoveAllInsects();
            }
            else
            {
                Debug.LogWarning("No InsectSpawner found in the scene");
            }
        }
    }
}
