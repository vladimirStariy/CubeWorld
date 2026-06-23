using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ItemTransformPreviewMenu
{
    private const string ScenePath = "Assets/Scenes/ItemTransformPreview.unity";

    [MenuItem("CubeWorld/Item Transform Preview")]
    private static void OpenPreviewScene()
    {
        if (!System.IO.File.Exists(ScenePath))
        {
            EditorUtility.DisplayDialog(
                "Item Transform Preview",
                $"Scene not found at {ScenePath}.",
                "OK");
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPlaying = true;
        }
    }
}
