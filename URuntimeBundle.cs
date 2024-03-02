using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Rendering;

public class URuntimeBundle : MonoBehaviour
{
    public static URuntimeBundle bundler;
    public GameObject collectOnStart;
    [SerializeReference] public TreeNode _treeNode;
    public Dictionary<object, Guid> KnownObjects = new Dictionary<object, Guid>();
    public Dictionary<Guid, URuntimeComponentNode> ComponentDB = new Dictionary<Guid, URuntimeComponentNode>();
    public Dictionary<Guid, GameObject> GameobjectRefs = new Dictionary<Guid, GameObject>();
    public Dictionary<Guid, Texture> TextureDB = new Dictionary<Guid, Texture>();
    public static Dictionary<Guid, Mesh> MeshDb = new Dictionary<Guid, Mesh>();
    public USerializableMaterial Material;
    public URuntimeSerializableBundle ursb;
    public string importExportPath = "BUNDLE1.urb";
    public bool import = false;
    public bool export = false;
    void Start()
    {
        if (bundler != null && bundler != this)
        {
            Destroy(this);
            return;
        }

        bundler = this;
        Debug.Log("Starting loading");
        if(import)
            InstantiateURB(importExportPath);
        //
        if (export)
        {
            if (collectOnStart != null)
                _treeNode = Collect(collectOnStart);
            ExportURB(importExportPath);
        }
//        URuntimeBundleInstantiator.instance.CreateInstanceOfURuntimeBundleObject(_treeNode);
//        URuntimeBundleInstantiator.instance.URuntimeBundleInstantiate("Object.uab");
    }

    void InstantiateURB(string path)
    {
        URuntimeSerializableBundle newBundle = URuntimeSerializableBundle.LoadFromFile(path);
        ursb = newBundle;
        Debug.Log("LOADED BUNDLE FROM PATH!");
        newBundle.PrepareForCreation();
    {
        URuntimeSerializableBundle newBundle = new URuntimeSerializableBundle(_treeNode);
        newBundle.LoadDataFromURuntimeBundle();
        newBundle.ExportBundle(path);
    }

    [Serializable]
    public class URuntimeSerializableBundle
    {
        public TreeNode node;
        public List<USerializableMesh> serializableMeshes;
        public List<USerializableTexture> texture2Ds;
        public URuntimeSerializableBundle(TreeNode rootNode)
        {
            this.node = rootNode;
        }

        private URuntimeSerializableBundle()
        {
            Debug.Log("Instantiating on instance now!");
            if (URuntimeBundleInstantiator.instance == null)
            {
                Debug.Log("No instantiator present!"); 
                return;
            }
            URuntimeBundleInstantiator.instance.URuntimeBundleInstantiate(node);
        }

        public static URuntimeSerializableBundle LoadFromFile(string path)
        {
            URuntimeSerializableBundle bundle;
            BinaryFormatter bf = new BinaryFormatter();
            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
            Debug.Log("DESERIALIZING NOW?");
            bundle = (URuntimeSerializableBundle)bf.Deserialize(stream);
            Debug.Log("SUCCESSFULLY LOADED BUNDLE FROM FILE");
            return bundle;
        }

        public void PrepareForCreation()
        {
            URuntimeBundle.bundler._treeNode = node;
            foreach (var texture in texture2Ds)
            {
                URuntimeBundle.bundler.TextureDB[texture.assetGuid] = (Texture)texture.GetValue();
            }
            foreach (var mesh in serializableMeshes)
            {
                Mesh m = (Mesh)mesh.GetMesh();
                
                Debug.Log("Placing " + m + " : " + mesh.guid.ToString() + " into Mesh db");
                MeshDb[mesh.guid] = m;
            }
        }

        public void Create()
        {
            URuntimeBundleInstantiator.instance.URuntimeBundleInstantiate(node);
        }

        public void LoadDataFromURuntimeBundle()
        {
            serializableMeshes = new List<USerializableMesh>();
            foreach (var mesh in MeshDb)
            {
                try
                {
        newBundle.Create();
    }
    
    void ExportURB(string path)
                    USerializableMesh m = new USerializableMesh(mesh.Value);
                    m.guid = mesh.Key;
                    serializableMeshes.Add(m);
                }
                catch
                {
                    Debug.Log("Could not export " + mesh);
                }
            }

            texture2Ds = new List<USerializableTexture>();
            foreach (var texture in bundler.TextureDB)
            {
                try
                {
                    USerializableTexture t = new USerializableTexture(texture.Value as Texture2D);
                    t.assetGuid = texture.Key;
                    texture2Ds.Add(t);
                }
                catch
                {
                    Debug.Log("Could not export an asset. " + texture.Value.name );
                }
            }
        }
        
        public void ExportBundle(string path)
        {
            BinaryFormatter bf = new BinaryFormatter();
            Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            bf.Serialize(stream, this);
            Debug.Log("SUCCESSFULLY EXPORTED!");
        }
    }

    TreeNode Collect(GameObject g)
    {
        TreeNode node = new TreeNode();
        node.IsActive = g.activeSelf;
        node.IsStatic = g.isStatic;
        node.Name = g.name;
//        Debug.Log("COLLECTING " + g);
        KnownObjects[g] = node.gameobjectId;
        foreach (Transform child in g.transform)
        {
            node.Children.Add(Collect(child.gameObject));
        }

        foreach (Component component in g.GetComponents(typeof(Component)))
        {
            if(component == null) continue;
            URuntimeComponentNode c = Collect(component);
            node.Components.Add(c);
        }
        return node;
    }
    public static Mesh MakeReadableMeshCopy(Mesh nonReadableMesh)
    {
        Mesh meshCopy = new Mesh();
        meshCopy.indexFormat = nonReadableMesh.indexFormat;
        #if UNITY_2019
        // Handle vertices
        GraphicsBuffer verticesBuffer = nonReadableMesh.GetVertexBuffer(0);
        int totalSize = verticesBuffer.stride * verticesBuffer.count;
        byte[] data = new byte[totalSize];
        verticesBuffer.GetData(data);
        meshCopy.SetVertexBufferParams(nonReadableMesh.vertexCount, nonReadableMesh.GetVertexAttributes());
        meshCopy.SetVertexBufferData(data, 0, 0, totalSize);
        verticesBuffer.Release();
 
        // Handle triangles
        meshCopy.subMeshCount = nonReadableMesh.subMeshCount;
        GraphicsBuffer indexesBuffer = nonReadableMesh.GetNativeIndexBufferPtr().GetIndexBuffer();
        int tot = indexesBuffer.stride * indexesBuffer.count;
        byte[] indexesData = new byte[tot];
        indexesBuffer.GetData(indexesData);
        meshCopy.SetIndexBufferParams(indexesBuffer.count, nonReadableMesh.indexFormat);
        meshCopy.SetIndexBufferData(indexesData, 0, 0, tot);
        indexesBuffer.Release();
 
        // Restore submesh structure
        uint currentIndexOffset = 0;
        for (int i = 0; i < meshCopy.subMeshCount; i++)
        {
            uint subMeshIndexCount = nonReadableMesh.GetIndexCount(i);
            meshCopy.SetSubMesh(i, new SubMeshDescriptor((int)currentIndexOffset, (int)subMeshIndexCount));
            currentIndexOffset += subMeshIndexCount;
        }
 
        // Recalculate normals and bounds
        meshCopy.RecalculateNormals();
        meshCopy.RecalculateBounds();
        #endif
        return meshCopy;
    }
    URuntimeComponentNode Collect(Component component)
    {
        if (KnownObjects.ContainsKey(component))
        {
            return ComponentDB[KnownObjects[component]];
        }

        Debug.Log("STARTED COLLECTING COMPONENT 1");
        URuntimeComponentNode componentNode = new URuntimeComponentNode();
        componentNode.Guid = System.Guid.NewGuid();
        componentNode.name = component.GetType().AssemblyQualifiedName;
        Debug.Log("STARTED COLLECTING COMPONENT 2");
        if (component == null) return componentNode;
        KnownObjects[component] = componentNode.Guid;
        ComponentDB[componentNode.Guid] = componentNode;
        
        Debug.Log("STARTED COLLECTING COMPONENT 3");

        foreach (var property in component.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.GetProperty))
        {
            if(!property.CanRead || !property.CanWrite) continue;
            //Attribute.IsDefined(property, typeof(SerializableAttribute));
            try
            {
                USerializedField serializedField = new USerializedField();
                serializedField.Name = property.Name;
                serializedField.Data = ConvertToSerializable(property.GetValue(component), property.PropertyType);
                serializedField.ValueStringRep = serializedField.Data.status;

                componentNode.Fields.Add(serializedField);
            }
            catch (Exception e)
            {
             //   Debug.Log("COULD NOT USE PROPERTY " + property.Name + "  " + e);
                continue;
            }
        }
        
        Debug.Log("STARTED COLLECTING COMPONENT 4");
        foreach (var field in component.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetField | BindingFlags.SetField))
        {
            //Attribute.IsDefined(property, typeof(SerializableAttribute));
            try
            {
                USerializedField serializedField = new USerializedField();
                serializedField.Name = field.Name;
                serializedField.Data = ConvertToSerializable(field.GetValue(component), field.FieldType);
                serializedField.ValueStringRep = serializedField.Data.status;

                componentNode.Fields.Add(serializedField);
            }
            catch (Exception e)
            {
                //   Debug.Log("COULD NOT USE PROPERTY " + property.Name + "  " + e);
                continue;
            }
        }
        
        
        /* INJECTION
         *
         *
         *
         * 
		if(Input.GetKeyDown(KeyCode.K))
			{
			Debug.Log("K PRESSED! SHOULD EXPORT NOW!");
				GameObject gameObject = new GameObject();
			URuntimeBundle b = gameObject.AddComponent<URuntimeBundle>();
			b.export = true;
			b.collectOnStart = this.transform.root.gameObject;
		
			}
         */
        /*foreach (var field in component.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            Debug.Log("FOUND FIELD");
            USerializedField serializedField = new USerializedField();
            serializedField.Name = field.Name;
            componentNode.Fields.Add(serializedField);
        }*/
        return componentNode;
    }

    [System.Serializable]
    public class TreeNode
    {
        public string Name;
        public bool IsActive;
        public bool IsStatic;
        public Guid gameobjectId = Guid.NewGuid();
        [NonSerialized]
        public readonly Dictionary<Guid, Mesh> Meshes = new Dictionary<Guid, Mesh>();
        public List<TreeNode> Children = new List<TreeNode>();
        [SerializeReference]
        public List<URuntimeComponentNode> Components = new List<URuntimeComponentNode>();
        public URuntimeComponentNode mainComp;
    }

    [System.Serializable]
    public class URuntimeComponentNode
    {
        public Guid Guid;
        public bool IsUnityBuiltin;
        public Assembly Assembly;
        public string name;
        
        public List<USerializedField> Fields = new List<USerializedField>();
    }

    [Serializable]
    public class USerializedField
    {
        public string Name;
        public DatatypeContainer Data;
        public string ValueStringRep;
    }
    [System.Serializable]
    public abstract class DatatypeContainer
    {
        public Guid assetGuid = System.Guid.NewGuid();
        public string status;
        public string type;
        
        public DatatypeContainer()
        {
            this.status = "SUCCESSFULLY CONVERTED DATATYPE";
        }

        public abstract object GetValue();
    }

    [Serializable]
    public class LargeAsset : DatatypeContainer
    {
        public string name;
        public string type;

        public override object GetValue()
        {
            // Add Asset DB lookup
            return null;
        }
    }

    [Serializable]
    public class USerializableTexture : DatatypeContainer
    {
        public USerializableArray pixels;
        public USerializableInt32 width;
        public USerializableInt32 height;
        [NonSerialized] private Texture2D _texture2D;
        public USerializableTexture(Texture2D t)
        {
            if (t == null)
            {
                this.status = "NULL TEXTURE";
                return;
            }
            Color[] colors = t.GetPixels();
            USerializableVector4[] pixelsF = new USerializableVector4[colors.Length];
            int i = 0;
            foreach (var color in colors)
            {
                pixelsF[i++] = new USerializableColor(color);
            }

            width = new USerializableInt32(t.width);
            
            height = new USerializableInt32(t.height);

            pixels = new USerializableArray(pixelsF);
        }
        public override object GetValue()
        {
            if (_texture2D != null) return _texture2D;
            if (status == "NULL TEXTURE") return null;
            _texture2D = new Texture2D((int)width.GetValue(), (int)height.GetValue());
            _texture2D.SetPixels((Color[])pixels.GetValue());
            return _texture2D;
        }
    }
    [Serializable]
    public class USerializableMeshRef : LargeAsset
    {
        public USerializableMeshRef(Mesh m)
        {
            this.type = "Mesh";
            MeshDb[this.assetGuid] = m;
            bundler.KnownObjects[m] = this.assetGuid;
        }

        public override object GetValue()
        {
            string guids = "";
            foreach (var kvpgm in MeshDb)
            {
                guids += kvpgm.Key + " (" + kvpgm.Value.name + ")\t";
            }
            Debug.Log("MESHDB: \n" + guids);
            Debug.Log("LOOKING UP IN MESHDB! " + this.assetGuid);
            return MeshDb[this.assetGuid];
        }
    }
    
    
    [Serializable]
    public class USerializableTextureRef : LargeAsset
    {
        public USerializableTextureRef(Texture t)
        {
            this.type = "Texture";
            bundler.TextureDB[this.assetGuid] = t;
        }

        public override object GetValue()
        {
            
            return bundler.TextureDB[this.assetGuid];
        }
    }

    [Serializable]
    public class String : DatatypeContainer
    {
        public string theString;

        public String(string theString)
        {
            this.theString = theString;
        }
        public override object GetValue()
        {
            // Add Asset DB lookup
            return theString;
        }
    }

    [Serializable]
    public class USerializableVector2: DatatypeContainer
    {
        public float x, y;

        public USerializableVector2(Vector2 v)
        {
            x = v.x;
            y = v.y;
        }

        public USerializableVector2()
        { }
        public override object GetValue()
        {
            return new Vector2(x,y);
        }
    }
    [Serializable]
    public class USerializableVector3 : USerializableVector2
    {
        public float z;

        public USerializableVector3(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }
        public USerializableVector3()
        { }
        public override object GetValue()
        {
            return new Vector3(x,y,z);
        }
    }
    
    [Serializable]
    public class USerializableVector4 : USerializableVector3
    {
        public float w;

        public USerializableVector4(Vector4 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
            w = v.w;
        }

        protected USerializableVector4()
        {
            
        }

        public override object GetValue()
        {
            return new Vector4(x,y,z,w);
        }
    }
    
    [Serializable]
    public class USerializableMatrix4x4 : USerializableVector3
    {
        public float w;
        public USerializableVector4 c1;
        public USerializableVector4 c2;
        public USerializableVector4 c3;
        public USerializableVector4 c4;
        public USerializableMatrix4x4(Matrix4x4 v)
        {
            c1 = new USerializableVector4(v.GetColumn(0));
            c2 = new USerializableVector4(v.GetColumn(1));
            c3 = new USerializableVector4(v.GetColumn(2));
            c4 = new USerializableVector4(v.GetColumn(3));
        }

        protected USerializableMatrix4x4() : this(Matrix4x4.identity) {}

        public override object GetValue()
        {
            return new Matrix4x4((Vector4)c1.GetValue(), (Vector4)c2.GetValue(), (Vector4)c3.GetValue(), (Vector4)c4.GetValue());
        }
    }
    [Serializable]
    public class USerializableColor : USerializableVector4
    {
        public float w;

        public USerializableColor(Color v)
        {
            x = v.r;
            y = v.g;
            z = v.b;
            w = v.a;
        }
        public override object GetValue()
        {
            return new Color(x,y,z,w);
        }
    }
    [Serializable]
    public class USerializableQuaternion : DatatypeContainer
    {
        public float x, y, z, w;

        public USerializableQuaternion(Quaternion v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
            w = v.w;
        }
        public override object GetValue()
        {
            return new Quaternion(x,y,z,w);
        }
    }
    
    [Serializable]
    public class UFailedToSerializeMissingType : DatatypeContainer
    {
        public UFailedToSerializeMissingType(string name)
        {
            this.status = "FAILED TO SERIALIZE TYPE " + name;
        }
        
        public override object GetValue()
        {
            return null;
        }
    }

    [Serializable]
    public class UMissingExternalComponentType : DatatypeContainer
    {
        public UMissingExternalComponentType(string name)
        {
            this.status = "COMPONENT APPEARS TO BE ON A DIFFERENT GAME OBJECT: " + name;
        }
        
        public override object GetValue()
        {
            return null;
        }
    }
    [Serializable]
    public class USerializableInt64 : DatatypeContainer
    {
        public long value;

        public USerializableInt64(long v)
        {
            value = v;
        }
        public override object GetValue()
        {
            return value;
        }
    }

    [Serializable]
    public class USerializableInt32 : DatatypeContainer
    {
        public int value;

        public USerializableInt32(int v)
        {
            value = v;
        }
        public override object GetValue()
        {
            return value;
        }
    }

    [Serializable]
    public class USerializableInt16 : DatatypeContainer
    {
        public short value;

        public USerializableInt16(short v)
        {
            value = v;
        }
        public override object GetValue()
        {
            return value;
        }
    }

    [Serializable]
    public class USerializableInt8 : DatatypeContainer
    {
        public sbyte value;

        public USerializableInt8(sbyte v)
        {
            value = v;
        }
        public override object GetValue()
        {
            return value;
        }
    }
    
    [Serializable]
    public class USerializableUInt32 : DatatypeContainer
    {
        public uint value;

        public USerializableUInt32(uint v)
        {
            value = v;
        }
        public override object GetValue()
        {
            return value;
        }
    }

    [Serializable]
    public class USerializableUInt16 : DatatypeContainer
    {
        public ushort value;

        public USerializableUInt16(ushort v)
        {
            value = v;
        }
        public override object GetValue()
        {
            return value;
        }
    }


    [Serializable]
    public class USerializableBoolean : DatatypeContainer
    {
        public bool value;

        public USerializableBoolean(bool v)
        {
            value = v;
        }
        public override object GetValue()
        {
            return value;
        }
    }
    
    

    [Serializable]
    public class USerializableFloat : DatatypeContainer
    {
        public float value;

        public USerializableFloat(float v)
        {
            value = v;
        }
        public override object GetValue()
        {
            return value;
        }
    }
    
    

    [Serializable]
    public class USerializableDouble : DatatypeContainer
    {
        public double value;

        public USerializableDouble(double v)
        {
            value = v;
        }
        public override object GetValue()
        {
            return value;
        }
    }
    
    [Serializable]
    public abstract class USerializableRef : DatatypeContainer
    {
        public Guid referenceId;
        
    }
    
    [Serializable]
    public class USerializableComponentRef : USerializableRef
    {
        public USerializableComponentRef(Guid compGuid)
        {
            this.type = "Component";
            this.referenceId = compGuid;
        }
        public override object GetValue()
        {
            return this.referenceId;
        }
    }
    
    
    
    [Serializable]
    public class USerializableGameobjectRef : USerializableRef
    {
        public USerializableGameobjectRef(Guid goGuid)
        {
            this.type = "GameObject";
            this.referenceId = goGuid;
            Debug.Log("Reference to gameobject " + this.referenceId.ToString() + " was created");
        }
        public override object GetValue()
        {
            // Lookup in ComponentDB
            return bundler.GameobjectRefs[this.referenceId];
        }
    }
    
    [Serializable]
    public class USerializableEnumValue : DatatypeContainer
    {
        public int enumValue;
        public USerializableEnumValue(int value)
        {
            this.type = "Enum";
            enumValue = value;
            
        }
        public override object GetValue()
        {
            // Lookup in ComponentDB
            return enumValue;
        }
    }
    
    [Serializable]
    public class USerializableMaterial : DatatypeContainer
    {
        public string shaderName;
        public List<USerializedField> matProps = new List<USerializedField>();
        public string[] keywords;
        public USerializableMaterial(Material material)
        {
            this.type = "Material";
            shaderName = material.shader.name;
            keywords = material.shaderKeywords;
            
            for (int i = 0; i < material.shader.GetPropertyCount(); i++)
            {
                ShaderPropertyType spt = material.shader.GetPropertyType(i);
                int propNameID = material.shader.GetPropertyNameId(i);
                if(!material.HasProperty(propNameID)) continue;
                USerializedField sf = new USerializedField();
                sf.Name = material.shader.GetPropertyName(i);
                //Debug.Log("Material get property: " + sf.Name);
                switch (spt)
                {
                    case ShaderPropertyType.Color:
                        sf.Data = new USerializableColor(material.GetColor(propNameID));
                        break;
                    case ShaderPropertyType.Vector:
                        sf.Data = new USerializableVector4(material.GetVector(propNameID));
                        break;
                    case ShaderPropertyType.Range:
                    case ShaderPropertyType.Float:
                        sf.Data = new USerializableFloat(material.GetFloat(propNameID));
                        break;
                    case ShaderPropertyType.Texture:
                        sf.Data = new USerializableTextureRef(material.GetTexture(propNameID));
                        break;
                }
                
            }
            Debug.Log("Material " + this.shaderName + " serialized into " + JsonUtility.ToJson(this) + " Value: " + JsonUtility.ToJson(this));

        }
        
        public override object GetValue()
        {
            Shader s = Shader.Find(shaderName);
            if (s == null) return null;
            int propCount = s.GetPropertyCount();
            List<ShaderPropertyType> propTypes;
            Material m = new Material(s);

            foreach (var kw in keywords)
            {
                m.EnableKeyword(kw);
            }
            
            foreach (var matProp in matProps)
            {
                ///ShaderPropertyType spt = s.GetPropertyType(i);
                //int nameId = s.GetPropertyNameId(i);
                DatatypeContainer value = matProp.Data;
                string name = matProp.Name;
                switch (value.GetType().Name)
                {
                    case "Color":
                        m.SetColor(name, (Color)value.GetValue());
                        break;
                    case "Vector4":
                        m.SetColor(name, (Vector4)value.GetValue());
                        break;
                    case "Float":
                        m.SetFloat(name, (float)value.GetValue());
                        break;
                    case "Texture":
                        m.SetTexture(name, (Texture)value.GetValue());
                        break;
                }

            }
//            Debug.Log("Created material " + m + " from " + JsonUtility.ToJson(this));
            return m;
        }
    }
    
    
        [Serializable]
        public class USerializableMesh
        {
            public string name;
            public Guid guid = System.Guid.NewGuid();
            public USerializableVector3[] verts;
            public USerializableInt32[] tris;
            //public USerializableArray serializableVerts;
            //public USerializableArray uSerializableArrayTris;
            public USerializableArray userializableSubmeshInfo;
            public USerializableInt32 submeshCount;
            [NonSerialized]
            private Mesh _mesh;
            public USerializableMesh(Mesh m)
            {
                if (!m.isReadable)
                {
                    m = MakeReadableMeshCopy(m);
                }
                if (bundler.KnownObjects.ContainsKey(m))
                {
                    guid = bundler.KnownObjects[m];
                }
                name = m.name;
                verts = new USerializableVector3[m.vertexCount];
                int i = 0;
                foreach (var vert in m.vertices)
                {
                    verts[i++] = new USerializableVector3(vert);
                }
                //serializableVerts = new USerializableArray(verts);
                tris = new USerializableInt32[m.triangles.Length];
                i = 0;
                foreach (var ind in m.triangles)
                {
                    tris[i++] = new USerializableInt32(ind);
                }
                ///uSerializableArrayTris = new USerializableArray(tris); 
                submeshCount = new USerializableInt32(m.subMeshCount);
                DatatypeContainer[] submeshes = new DatatypeContainer[m.subMeshCount*2];
                i = 0;
                for (int j = 0; j < m.subMeshCount; j++)
                {
                    SubMeshDescriptor smdesc = m.GetSubMesh(j);
                    submeshes[i++] = new USerializableInt32(smdesc.firstVertex);
                    submeshes[i++] = new USerializableInt32(smdesc.vertexCount);
                }
                userializableSubmeshInfo = new USerializableArray(submeshes);
                Debug.Log("GetMeshVertlen: " + tris.Length);
            }

            public Mesh GetMesh()
            {
                if (_mesh != null) return _mesh;
                _mesh = new Mesh();
                _mesh.name = name;
                _mesh.subMeshCount = (int)submeshCount.GetValue();
                //Debug.Log("casting from " + new USerializableArray(verts).GetValue().GetType() + " Vector3[]");
                Vector3[] verts = new Vector3[this.verts.Length];
                for (int j = 0; j < verts.Length; j++)
                {
                    verts[j] = (Vector3)this.verts[j].GetValue();
                }
                _mesh.vertices = verts;
                Debug.Log("MESH VERTS DESERIALIZED: " + _mesh.vertices.Length + " FROM " + verts.Length);
                int[] inds = new int[this.tris.Length];
                for (int j = 0; j < tris.Length; j++)
                {
                    inds[j] = (int)this.tris[j].GetValue();
                }

                _mesh.triangles = inds;
                //USerializableInt32[] submeshInfo = (USerializableInt32[])userializableSubmeshInfo.GetValue();
                //int i = 0;
                Debug.Log("GetMeshVertlen: " + _mesh.triangles.Length);
                MeshDb[this.guid] = _mesh;
                /*for (int j = 0; j < _mesh.subMeshCount; j++)
                {
                    SubMeshDescriptor smdesc = new SubMeshDescriptor();
                    
                    smdesc.firstVertex = (int)submeshInfo[i++].GetValue();
                    smdesc.vertexCount = (int)submeshInfo[i++].GetValue();
                    _mesh.SetSubMesh(j, smdesc);
                }*/

                return _mesh;
            }
        }
    [Serializable]
    public class USerializableClass : DatatypeContainer
    {
        public List<USerializedField> fields;
        public USerializableClass(List<USerializedField> fields, string type)
        {
            this.type = type;
            this.fields = fields;
        }

        public override object GetValue()
        {
            Type t = Type.GetType(this.type);
            if (t == null)
            {
                Debug.LogError("Could not find type " + this.type);
                return null;
            }

            object createdObject;
            try
            {
                //createdObject = Activator.CreateInstance(t);
            }
            catch (Exception e)
            {
                Debug.LogError("Could not Activate instance of type " + this.type);
                return null;
            }


            var p = t.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            Dictionary<string, PropertyInfo> pinfo = new Dictionary<string, PropertyInfo>();
            foreach (var entry in p)
            {
                pinfo[entry.Name] = entry;
            }
    
            /*
            foreach (var field in fields)
            {
                try
                {
                    if (pinfo.ContainsKey(field.Name))
                    {
                        Debug.Log("Attempting to set field " + createdObject + "[" + field.Name + "] to " + field.Data.GetValue());
                        object o = field.Data.GetValue();
                        if (o == null) continue;
                        pinfo[field.Name].SetValue(createdObject, o);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("COULD NOT DESERIALIZE FIELD INTO COMPONENT BECAUSE " + e);
                }
            }*/

            return null;
        }
    }
    
    [Serializable]
    public class USerializableArray : DatatypeContainer
    {
        public DatatypeContainer[] Content;
        public USerializableArray(DatatypeContainer[] content)
        {
            this.type = "Array";
            this.Content = content;
            
        }
        public override object GetValue()
        {
//            Debug.Log("ARRAY GET VALUE! " + Content + " " );
            if (Content.Length < 1)
                return null;
            Type arrayType = typeof(int);
            bool typeSet = false;
            for (int i = 0; i < Content.Length; i++)
            {
                object o = Content[i].GetValue();
                if(o == null) continue;
                arrayType = o.GetType();
                typeSet = true;
                break;
            }

            if (!typeSet) return null;
            Debug.Log("ARRAY TYPE: " + arrayType);
            /*if (arrayType )
                return null;
            */
            Array objects = Array.CreateInstance(arrayType,Content.Length);
            for(int i = 0; i < Content.Length; i++)
            {
                //Debug.Log("TYPE: " + arrayType + " Object: " + Content[i].GetValue());
                //objects[i] = Convert.ChangeType(Content[i].GetValue(), arrayType);
                objects.SetValue(Content[i].GetValue(),i);
            }

            return objects;
        }
    }



    public DatatypeContainer ConvertToSerializable(object o, Type t)
    {
        DatatypeContainer container = null;
        if (t == null)
        {
            Debug.Log("HUH? null type???");
            
            return new UFailedToSerializeMissingType("null type");
        }

        switch (t.Name)
        {
            case "Vector2":
                return new USerializableVector2((Vector2)o);
            case "Vector3":
                return new USerializableVector3((Vector3)o);
            case "Color":
                return new USerializableColor((Color)o);
            case "Vector4":
                return new USerializableVector4((Vector4)o);
            case "Matrix4x4":
                return new USerializableMatrix4x4((Matrix4x4)o);
            case "Quaternion":
                return new USerializableQuaternion((Quaternion)o);
            case "String":
                return new String((string)o);
            case "Int64":
                return new USerializableInt64((long)o);
            case "Int32":
                return new USerializableInt32((int)o);
            case "Int16":
                return new USerializableInt16((short)o);
            case "Int8":
                return new USerializableInt8((sbyte)o);
            case "UInt32":
                return new USerializableUInt32((uint)o);
            case "UInt16":
                return new USerializableUInt16((ushort)o);
            case "Boolean":
                return new USerializableBoolean((bool)o);
            case "Single":
                return new USerializableFloat((float)o);
            case "Double":
                return new USerializableDouble((double)o);
            case "Mesh":
                return new USerializableMeshRef((Mesh)o);
            case "Material":
                this.Material = new USerializableMaterial((Material)o);
                return new USerializableMaterial((Material)o);
            default:
                if (t.IsSubclassOf(typeof(Component)))
                {
                    if (o == null)
                    {
                        return new UMissingExternalComponentType("COMPONENT IS NULL");
                    }

                    if (KnownObjects.ContainsKey(o))
                    {
                        return new USerializableComponentRef(KnownObjects[o]);
                    }

                    return new UMissingExternalComponentType("MISSING EXTERNAL COMPONENT [" + o + "]");
                }
                else if (t.IsSubclassOf(typeof(GameObject)))
                {
                    if (o == null)
                        return new UMissingExternalComponentType("Gameobject is null");
                    if (KnownObjects.ContainsKey(o))
                    {
                        return new USerializableGameobjectRef(KnownObjects[o]);
                    }
                }
                else if (t.IsEnum)
                {
                    return new USerializableEnumValue((int)o);
                }
                else if (t.IsArray)
                {
                    if (o == null)
                        return new USerializableArray(Array.Empty<DatatypeContainer>());
                    Type elementType = t.GetElementType();
                    if (elementType.IsPrimitive)
                    {
//                        Debug.Log("CASTING " + o + " TO ARRAY");
                        Array objects = (Array)o;
                        
                        DatatypeContainer[] containers = new DatatypeContainer[objects.Length];
                        for (int i = 0; i < objects.Length; i++)
                        {
                            containers[i] = ConvertToSerializable(objects.GetValue(i), objects.GetValue(i).GetType());
                        }

                        return new USerializableArray(containers);
                    }
                    else
                    {

//                        Debug.Log("CASTING " + o + " TO ARRAY");
                        object[] objects = (object[])o;
                        DatatypeContainer[] containers = new DatatypeContainer[objects.Length];
                        for (int i = 0; i < objects.Length; i++)
                        {
                            containers[i] = ConvertToSerializable(objects[i], objects[i].GetType());
                        }

                        return new USerializableArray(containers);
                    }
                }
                else if (t.IsClass)
                {
                    USerializableClass uSerializableClass;
                    List<USerializedField> fields = new List<USerializedField>();
                    if (o != null)
                    {
                        foreach (var property in t.GetProperties(BindingFlags.Instance | BindingFlags.Public |
                                                                 BindingFlags.SetField))
                        {
                            if (!property.CanWrite || !property.CanRead) continue;
                            USerializedField serializedField = new USerializedField();
                            serializedField.Name = property.Name;
                            serializedField.Data = ConvertToSerializable(property.GetValue(o), property.PropertyType);
                            serializedField.ValueStringRep = serializedField.Data.status;

                            fields.Add(serializedField);
                        }
                    }

                    uSerializableClass = new USerializableClass(fields, t.AssemblyQualifiedName);
                    return uSerializableClass;
                }
                return new UFailedToSerializeMissingType(t.Name);
            }
        }
}
