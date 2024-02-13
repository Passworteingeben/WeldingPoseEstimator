using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

public class MeshLoader : MonoBehaviour
{
    public string materialPath = "Materials/defaultMaterial";
    public string inputMeshPath = "ToolData/MRW510_10GH";
    // public string meshName = "MeshData/202110184743_R3";

    // public string PATH_TORCH_1 = "ToolData/MRW510_10GH";
    // public string PATH_TORCH_2 = "ToolData/TAND_GERAD_DD";

    void Start()
    {
        string name = inputMeshPath.Split('/').Last();
        GameObject obj = load_mesh(inputMeshPath, name);
    }

    GameObject load_mesh(string meshPath, string name)
    {
        /// <summary>
        /// Loads mesh and creates a gameobject with a given name
        /// The gameObject then has to be pulled into Assets to be created as an prefab in order to be persistent beyond this session
        /// </summary>
        GameObject obj = new GameObject(name);
        MeshFilter filter = obj.AddComponent<MeshFilter>();
        MeshRenderer renderer = obj.AddComponent<MeshRenderer>();

        // set mesh
        Mesh meshObj = Resources.Load<GameObject>(meshPath).transform.GetChild(0).gameObject.GetComponent<MeshFilter>().sharedMesh;
        meshObj.name = name;
        filter.mesh = meshObj;
        renderer.material = Resources.Load(materialPath, typeof(Material)) as Material;

        return obj;
    }
}

