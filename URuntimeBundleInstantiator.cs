using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class URuntimeBundleInstantiator : MonoBehaviour
{
    public Dictionary<Guid, Component> ComponentDeserializeDB = new Dictionary<Guid, Component>();
    public URuntimeBundle.TreeNode TreeNode;
    public void Awake()
    {
        instance = this;
    }

    public GameObject URuntimeBundleInstantiate(URuntimeBundle.TreeNode treeNode)
    {
        URuntimeBundle.bundler.ComponentDB.Clear();
        URuntimeBundle.bundler.KnownObjects.Clear();
        URuntimeBundle.bundler.GameobjectRefs.Clear();
        // Clear meshes and everything else too, currently cheating!
        return CreateInstanceOfURuntimeBundleObject(treeNode);
    }
    
    public GameObject URuntimeBundleInstantiate(string treeNode)
    {
        
        URuntimeBundle.bundler.ComponentDB.Clear();
        URuntimeBundle.bundler.KnownObjects.Clear();
        URuntimeBundle.bundler.GameobjectRefs.Clear();
        
        BinaryFormatter bf = new BinaryFormatter();
        Stream stream = new FileStream(treeNode, FileMode.Open, FileAccess.Read, FileShare.None);
        URuntimeBundle.TreeNode instantiatedTreeNode = (URuntimeBundle.TreeNode)bf.Deserialize(stream);
        TreeNode = instantiatedTreeNode;
        // Clear meshes and everything else too, currently cheating!
        return CreateInstanceOfURuntimeBundleObject(instantiatedTreeNode);
    }
    
    public GameObject CreateInstanceOfURuntimeBundleObject(URuntimeBundle.TreeNode treeNode, Transform parent = null)
    {
        
        Dictionary<Guid, Component> localLookup =
            new Dictionary<Guid, Component>();
        GameObject root = new GameObject(treeNode.Name);
        root.transform.parent = parent;
        URuntimeBundle.bundler.GameobjectRefs[treeNode.gameobjectId] = root;
        
        
        foreach (var c in treeNode.Children)
        {
            CreateInstanceOfURuntimeBundleObject(c, root.transform);
        }
        
        foreach (var comp in treeNode.Components)
        {
            Type t = Type.GetType(comp.name);
            if (t == null)
            {
                Debug.Log("Type is null :(");
                continue;
            }

            Component c = root.AddComponent(t);

            if (c == null)
                c = root.GetComponent(t);

            URuntimeBundle.bundler.ComponentDB[comp.Guid] = comp;
            ComponentDeserializeDB[comp.Guid] = c;
            URuntimeBundle.bundler.KnownObjects[c] = comp.Guid;
            localLookup[comp.Guid] = c;
        }


        foreach (var compNode in treeNode.Components)
        {
            Debug.Log(compNode.name);
//            Debug.Log("Adding component with type of " + compNode.name);
            if(localLookup.ContainsKey(compNode.Guid))
                FillFields(localLookup[compNode.Guid], compNode);
        }

        return root;
    }

    /*void RecursivelyCreateChildren(URuntimeBundle.TreeNode node, Transform parent)
    {
        GameObject thisNode = new GameObject(node.Name);
        URuntimeBundle.bundler.GameobjectRefs[node.gameobjectId] = thisNode;
        thisNode.transform.parent = parent;
        foreach (var c in node.Children)
        {
            RecursivelyCreateChildren(c, thisNode.transform);
        }
        foreach (var comp in node.Components)
        {
            Type t = Type.GetType(comp.name);
            Component c = thisNode.AddComponent(t);
            if (c == null)
                c = thisNode.GetComponent(t);
            Debug.Log("Adding component with type of " + t);
            FillFields(c, comp);
        }
    }*/
    
    void FillFields(Component c, URuntimeBundle.URuntimeComponentNode componentNode)
    {
        
        var p = c.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var f = c.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetField | BindingFlags.GetField);
        Dictionary<string, PropertyInfo> pinfo = new Dictionary<string, PropertyInfo>();
        foreach (var entry in p)
        {
            pinfo[entry.Name] = entry;
        }
        
        Dictionary<string, FieldInfo> finfo = new Dictionary<string, FieldInfo>();
        foreach (var entry in f)
        {
            finfo[entry.Name] = entry;
        }
        /*var f = c.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
        Dictionary<string, FieldInfo> finfo = new Dictionary<string, FieldInfo>();
        foreach (var entry in f)
        {
            finfo[entry.Name] = entry;
        }*/
        foreach (var field in componentNode.Fields)
        {
            if (pinfo.ContainsKey(field.Name))
            {
                if (!pinfo[field.Name].CanWrite) continue;
                
                
                Debug.Log("Attempting to set property " + c + "[" + field.Name + "] to " + field.Data.GetValue());
                object o = field.Data.GetValue();
                if (o == null) continue;
//                Debug.Log(field.Data.type);
                switch (field.Data.type)
                {
                    case "Component":
                        pinfo[field.Name].SetValue(c, ComponentDeserializeDB[(Guid)o]);
                        break;
                    case "Array":
                        //var objects2 = Array.ConvertAll(objects, obj => Convert.ChangeType(obj,obj.GetType()));
                        Debug.Log("TRYING TO DO ARRAY STUFF! " + JsonUtility.ToJson(o));
                        pinfo[field.Name].SetValue(c, o);
                        break;
                    default:
                        pinfo[field.Name].SetValue(c, o);
                        break;
                }
                /*else if (finfo.ContainsKey(field.Name))
                {
                    Debug.Log("Attempting to set field " + c + "[" + field.Name + "] to " + field.Data.GetValue());
                    object o = field.Data.GetValue();
                    if (o == null) continue;
                    finfo[field.Name].SetValue(c, o);
                }*/
            }
            else if (finfo.ContainsKey(field.Name))
            {
//                if (!finfo[field.Name]) continue;
                
                
                Debug.Log("Attempting to set field " + c + "[" + field.Name + "] to " + field.Data.GetValue());
                object o = field.Data.GetValue();
                if (o == null) continue;
//                Debug.Log(field.Data.type);
                switch (field.Data.type)
                {
                    case "Component":
                        finfo[field.Name].SetValue(c, ComponentDeserializeDB[(Guid)o]);
                        break;
                    case "Array":
                        //var objects2 = Array.ConvertAll(objects, obj => Convert.ChangeType(obj,obj.GetType()));
                        Debug.Log("TRYING TO DO ARRAY STUFF! " + JsonUtility.ToJson(o));
                        finfo[field.Name].SetValue(c, o);
                        break;
                    default:
                        finfo[field.Name].SetValue(c, o);
                        break;
                }
                /*else if (finfo.ContainsKey(field.Name))
                {
                    Debug.Log("Attempting to set field " + c + "[" + field.Name + "] to " + field.Data.GetValue());
                    object o = field.Data.GetValue();
                    if (o == null) continue;
                    finfo[field.Name].SetValue(c, o);
                }*/
            }
        }
    }
    
    public IList createList(Type myType)
    {
        Type genericListType = typeof(List<>).MakeGenericType(myType);
        return (IList)Activator.CreateInstance(genericListType);
    }
    public static URuntimeBundleInstantiator instance { get; set; }
}