using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Xml;
using UnityEngine;

[Serializable]
public class GameObjectNode
{
    public TransformNode[] m_NodeDic;
    public int m_Count = 0;
    public GameObjectNode(GameObject obj, int count)
    {
        m_Count = 0;
        m_NodeDic = new TransformNode[count];
    }

    public void Add(TransformNode value)
    {
        m_NodeDic[m_Count++] = value;
    }

    public SecurityElement Save2Xml()
    {
        SecurityElement node = new SecurityElement("GameObjectNode");
        foreach (var pf in m_NodeDic)
        {
            node.AddChild(pf.Save2Xml());
        }
        return node;
    }
}

[Serializable]
public class TransformNode
{
    // transform path, from root parent
    public string path;
    public List<ComponentNode> componentNodes;

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

    public SecurityElement Save2Xml()
    {
        SecurityElement node = new SecurityElement("TransformNode");
        node.AddAttribute("path", path);
        foreach (var pf in componentNodes)
        {
            node.AddChild(pf.Save2Xml());
        }
        return node;
    }
}

[Serializable]
public class ComponentNode
{
    public Type componentType;
    public string componetName;
    public List<PropertyNode> properties;
    public List<FieldNode> fields;

    public ComponentNode(Component component)
    {
        componentType = component.GetType();
        componetName = component.name;
        PrefabNodeCopy.DeepCopy(component, out properties, out fields);
    }

    public object GetValue(string key)
    {
        foreach (var kv in properties)
        {
            if (kv.key == key)
                return kv.value;
        }

        foreach (var kv in fields)
        {
            if (kv.key == key)
                return kv.value;
        }
        return null;
    }

    public SecurityElement Save2Xml()
    {
        SecurityElement node = new SecurityElement("ComponentNode");
        node.AddAttribute("type", componentType.ToString());
        node.AddAttribute("name", componetName);
        foreach (var pf in properties)
        {
            var childNode = pf.Save2Xml();
            if (childNode != null)
                node.AddChild(childNode);
        }

        foreach (var f in fields)
        {
            var childNode = f.Save2Xml();
            if (childNode != null)
                node.AddChild(childNode);
        }
        return node;
    }
}

[Serializable]
public class PropertyNode
{
    [DataMember]
    public string key;
    [DataMember]
    public object value;
    public PropertyNode() { }

    public SecurityElement Save2Xml()
    {
        //Debug.LogError("PropertyOrFieldNode" + LitJson.JsonMapper.ToJson(this));
        //return @"{ ""key"":" + key + @" ""value"": " + value + "}";

        try
        {
            SecurityElement node = new SecurityElement("PropertyNode");
            node.AddAttribute("PropertyKey", key.ToString());
            node.AddAttribute("PropertyValue", value.ToString());
            return node;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        return null;
    }
}

public class FieldNode
{
    [DataMember]
    public string key;
    [DataMember]
    public object value;
    public FieldNode() { }

    public SecurityElement Save2Xml()
    {
        try
        {
            if (key.Contains("k__BackingField"))
                return null;
            SecurityElement node = new SecurityElement("FieldNode");
            node.AddAttribute("FieldKey", key.ToString());
            //node.AddAttribute("FieldValue", value.ToString());
            return node;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        return null;
    }
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

    public static void DeepCopy(Component comp, out List<PropertyNode> properties, out List<FieldNode> fields)
    {
        Type type = comp.GetType();
        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Instance | BindingFlags.Default;


        properties = new List<PropertyNode>();
        fields = new List<FieldNode>();
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
                        properties.Add(new PropertyNode()
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
                fields.Add(new FieldNode()
                {
                    key = finfo.Name,
                    value = v
                });
            }
        }
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


    public static void SaveData2File(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        var doc = new XmlDocument();
        var docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
        doc.AppendChild(docNode);
        var root = m_GameObjectNode.Save2Xml();

        DumpSecurityElement(doc, null, root);
        doc.Save(filePath);
    }

    public static void DumpSecurityElement(XmlDocument InDocument, XmlNode InParent, SecurityElement InElement)
    {
        var node = InDocument.CreateElement(InElement.Tag);
        node.InnerText = InElement.Text;
        if (InParent != null)
        {
            InParent.AppendChild(node);
        }
        else
        {
            InDocument.AppendChild(node);
        }

        if (InElement.Attributes != null)
        {
            var iter = InElement.Attributes.GetEnumerator();
            while (iter.MoveNext())
            {
                var xmlAttr = InDocument.CreateAttribute(iter.Key as string);
                xmlAttr.Value = iter.Value as string;

                node.Attributes.Append(xmlAttr);
            }
        }

        if (InElement.Children != null)
        {
            foreach (SecurityElement Element in InElement.Children)
            {
                DumpSecurityElement(InDocument, node, Element);
            }
        }
    }
}
