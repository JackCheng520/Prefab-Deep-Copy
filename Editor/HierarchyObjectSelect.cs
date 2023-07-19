using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class HierarchyObjectSelect : MonoBehaviour
{
    [MenuItem("Tools/FBX2Clip")]
    public static void Fbx2AnimationClip()
    {
        var guids = AssetDatabase.FindAssets("t:Model", new string[] { "Assets/UIAnimation/Animator" });
        foreach(var guid in guids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Debug.Log(assetPath);
            var _asset = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
            foreach (var assetRepresentation in _asset)
            {
                var animationClip = assetRepresentation as AnimationClip;

                if (animationClip != null)
                {
                    Debug.Log("Found animation clip");
                    AssetDatabase.CreateAsset(Object.Instantiate(animationClip), $"Assets/UIAnimation/{animationClip.name}.anim");
                    AssetDatabase.SaveAssets();
                }
            }
        }
    }

    private static float _lastMenuCallTimestamp;
    [MenuItem("GameObject/Group selected GameObjects", false, 0)]
    public static void GroupSelected(MenuCommand menuCommand)
    {
        //Prevent executing multiple times when right-clicking.
        if (Time.unscaledTime.Equals(_lastMenuCallTimestamp))
        {
            return;
        }

        var parent = new GameObject();
        var selected = Selection.gameObjects;

        foreach (var gameObject in selected)
        {
            gameObject.transform.SetParent(parent.transform);
        }

        Selection.activeGameObject = parent;
        _lastMenuCallTimestamp = Time.unscaledTime;
    }

    [MenuItem("GameObject/Deep Copy", false, 0)]
    public static void DeepCopySelected(MenuCommand menuCommand)
    {
        //Prevent executing multiple times when right-clicking.
        if (Time.unscaledTime.Equals(_lastMenuCallTimestamp))
        {
            return;
        }

        var selected = Selection.gameObjects;

        foreach (var gameObject in selected)
        {

            PrefabNodeCopy.DeepCopy(gameObject);
        }



        PrefabNodeCopy.SaveData2File(Path.Combine(Application.dataPath, "../DeepCopy/children.xml"));
        Debug.Log("Deep Copy Ok");
    }
}
