using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MeshProcess;
using UnityEditor;
using Unity.MLAgents.Sensors;


public class ToolCreation : MonoBehaviour
{
    private Material defaultMaterial;
    public GameObject tool;
    public GameObject target;
    private GameObject agent;

    private Vector3 rotAngle = new Vector3(-90f,0f,180f);
    public bool addLidar = true;

    void Start()
    {
        if (tool.name == "TAND_GERAD_DD")
        {
            rotAngle = new Vector3(-90f,0f,180f);
        }
        if (tool.name == "MRW510_10GH")
        {
            rotAngle = new Vector3(-60f,0f,180f);
        }
        if (target == null)
        {
            target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            target.transform.SetParent(this.transform);
            target.transform.localPosition = Vector3.zero;
            target.transform.eulerAngles = Vector3.zero;
            target.transform.localScale =  new Vector3(0.3f, 0.3f, 0.3f);
            target.name = "target";
            target.tag = "target";
            target.layer = LayerMask.NameToLayer("Ignore Raycast");
        }
        agent = new GameObject("Agent");
        ToolConvexSplitter toolConvexSplitter = this.gameObject.AddComponent<ToolConvexSplitter>();
        defaultMaterial = Resources.Load("Materials/defaultMaterial", typeof(Material)) as Material;
        List<GameObject> colliderObjs = toolConvexSplitter.split(tool, defaultMaterial);
        tool.transform.SetParent(agent.transform);
        // GameObject mesh = tool.transform.Find("default");
        foreach (GameObject obj in colliderObjs)
        {
            obj.transform.SetParent(tool.transform);
        }
        tool.transform.localPosition = new Vector3(0f,0.2f,0f);
        tool.transform.localScale = new Vector3(0.006f, 0.006f, 0.006f);
        tool.transform.eulerAngles =rotAngle;
        StartCoroutine(frameAfterStart());
    }

    IEnumerator frameAfterStart()
    {
        yield return new WaitForSeconds(0);
        if (addLidar)
        {
            LidarSensorGenerator lidarSensorGenerator = this.gameObject.AddComponent<LidarSensorGenerator>();

            List<GameObject> lidarPoints = lidarSensorGenerator.generateToolRays(target);
            foreach (GameObject obj in lidarPoints)
            {
                obj.transform.SetParent(tool.transform);
            }
        }
        StartCoroutine(afterLidar());
    }
    IEnumerator afterLidar()
    {
        yield return new WaitForSeconds(1);
        // tool.transform.eulerAngles = new Vector3(0f,0f,180f);
        // tool.transform.localPosition = new Vector3(0f,0f,0.2f);
        foreach (Transform child in agent.transform) 
        {
            child.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            foreach (Transform grandchild in child) 
            {
                grandchild.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            }
        }
    }

}
