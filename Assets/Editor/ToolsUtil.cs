using UnityEditor;
using UnityEngine;

public class ToolsUtil
{
    private const string AnimationDataPath = "Assets/Resources/Data/AnimationData";
    
    [MenuItem("Tools/处理选中的FBX")]
    public static void HandleSelected()
    {
        var guids = Selection.assetGUIDs;
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Debug.Log("通过GUID获取的路径: " + assetPath);
            if (assetPath.EndsWith(".FBX") || assetPath.EndsWith(".fbx"))
            {
                AnimationUtil.ImportAnimationDataAndClip(assetPath, AnimationDataPath);
            }
        }
    }

    [MenuItem("Tools/打开PersistentDataPath")]
    public static void OpenPersistentDataPath()
    {
        Application.OpenURL(Application.persistentDataPath);
    }
    
    [MenuItem("Tools/打开StreamingAssetsPath")]
    public static void OpenStreamingAssetsPath()
    {
        Application.OpenURL(Application.streamingAssetsPath);
    }
    
}