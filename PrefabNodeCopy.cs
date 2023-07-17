using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

[Serializable]
public class GameObjectNode
{
    private TransformNode[] m_NodeDic;
    private int m_Count = 0;
    public GameObjectNode(GameObject obj, int count)
    {
        m_Count = 0;
        m_NodeDic = new TransformNode[count];
    }

    public void Add(TransformNode value)
    {
        m_NodeDic[m_Count++] = value;
    }
}

[Serializable]
public class TransformNode
{
    // transform path, from root parent
    public string path;
    List<ComponentNode> componentNodes;

    public TransformNode(Transform node, Transform root)
    {
        path = PrefabNodeCopy.GetGameObjectPath(node.gameObject, root);
        var components = node.GetComponents<Component>();
        componentNodes = new List<ComponentNode>(components.Length);
        foreach (var c in components)
        {
            if (c == null)
                continue;
            var cn = new ComponentNode(c);
            componentNodes.Add(cn);
        }
    }
}

[Serializable]
public class ComponentNode
{
    public Type componentType;
    public string componetName;
    public List<PropertyOrFieldNode> propertiesOrFields;

    public ComponentNode(Component component)
    {
        componentType = component.GetType();
        componetName = component.name;
        propertiesOrFields = PrefabNodeCopy.DeepCopy(component);
    }

    public object GetValue(string key)
    {
        foreach (var kv in propertiesOrFields)
        {
            if (kv.key == key)
                return kv.value;
        }
        return null;
    }
}

[Serializable]
public class PropertyOrFieldNode
{
    public string key;
    public object value;
}
public static class PrefabNodeCopy
{
    private static GameObjectNode m_GameObjectNode;
    /// <summary>
    /// key: transform path,value: Node
    /// </summary>
    public static void DeepCopy(GameObject obj)
    {
        var childMap = new List<Transform>();
        IterateThroughObjectTree(obj, ref childMap);
        m_GameObjectNode = new GameObjectNode(obj, childMap.Count);
        foreach (var child in childMap)
        {
            var tn = new TransformNode(child, obj.transform);
            m_GameObjectNode.Add(tn);
        }
    }

    public static List<PropertyOrFieldNode> DeepCopy(Component comp)
    {
        Type type = comp.GetType();
        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Instance | BindingFlags.Default;


        List<PropertyOrFieldNode> list = new List<PropertyOrFieldNode>();
        // Property Info
        PropertyInfo[] pinfos = type.GetProperties(flags);
        foreach (var pinfo in pinfos)
        {
            if (pinfo.CanWrite)
            {
                try
                {
                    var v = pinfo.GetValue(comp);
                    if (v != null)
                    {
                        list.Add(new PropertyOrFieldNode()
                        {
                            key = pinfo.Name,
                            value = v
                        });
                    }
                    //pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                }
                catch
                {
                    /*
                     * In case of NotImplementedException being thrown.
                     * For some reason specifying that exception didn't seem to catch it,
                     * so I didn't catch anything specific.
                     */
                }
            }
        }

        // Field Info
        FieldInfo[] finfos = type.GetFields(flags);
        foreach (var finfo in finfos)
        {
            //finfo.SetValue(comp, finfo.GetValue(other));
            var v = finfo.GetValue(comp);
            if (v != null)
            {
                list.Add(new PropertyOrFieldNode()
                {
                    key = finfo.Name,
                    value = v
                });
            }
        }
        return list;
    }

    private static void IterateThroughObjectTree(GameObject go, ref List<Transform> childMap)
    {
        // If not a prefab, go through all children
        var transform = go.transform;
        var children = transform.childCount;
        for (int i = 0; i < children; i++)
        {
            var childGo = transform.GetChild(i);
            childMap.Add(childGo);
            IterateThroughObjectTree(childGo.gameObject, ref childMap);
        }
    }

    public static string GetGameObjectPath(GameObject obj, Transform root)
    {
        string path = obj.name;
        while (obj.transform.parent != root)
        {
            obj = obj.transform.parent.gameObject;
            path = obj.name + "/" + path;
        }
        return path;
    }

    public static string ToJson()
    {
        return LitJson.JsonMapper.ToJson(m_GameObjectNode);
    }

    public static void SaveData2File(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        var data = ToJson();
        File.WriteAllText(filePath, data);
    }
}
