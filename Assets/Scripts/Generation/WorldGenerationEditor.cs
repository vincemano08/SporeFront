#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WorldGeneration))]
public class WorldGenerationEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        WorldGeneration worldGen = (WorldGeneration)target;

        // Only show button if the game is playing
        if (Application.isPlaying) {
            if (GUILayout.Button("Clear Map")) {
                InsectSpawner insectSpawner = FindFirstObjectByType<InsectSpawner>();
                if (insectSpawner != null) {
                    insectSpawner.RemoveAllInsects();
                }
                worldGen.ClearMap();
            }
            if (GUILayout.Button("Generate Map")) {
                worldGen.GenerateMap();
            }
        }
    }
}
#endif