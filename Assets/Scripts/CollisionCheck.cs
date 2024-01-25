// using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using MeshProcess;

public class CollisionCheck : MonoBehaviour
{
    
    public List<Material> mats;
    public Material collisionMaterial;
    public Material defaultMaterial;
    public bool showColor = false;
    public bool collisionState = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    
    // for convex hull decomposition
    // https://github.com/Unity-Technologies/VHACD
    // https://docs.unity3d.com/ScriptReference/Collider.OnCollisionEnter.html
    void OnTriggerEnter(Collider other)
    {
        // Debug.Log($"hit something enter {other.gameObject.tag}");
        if (other.gameObject.tag == "obstacle")
        {
            collisionState = true;
        }
    }

    void OnTriggerStay(Collider other)
    {
        // Debug.Log($"hit something stay {other.gameObject.tag}");
        if (other.gameObject.tag == "obstacle")
        {
            collisionState = true;
            if (showColor)
            {
                other.GetComponent<MeshRenderer>().material = collisionMaterial;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        collisionState = false;
        if (showColor)
        {
            // other.GetComponent<MeshRenderer>().material = defMaterial;
            if (showColor)
            {
                other.GetComponent<MeshRenderer>().material = mats[UnityEngine.Random.Range(0,mats.Count)];
            }
        }
    }

    public void resetCollisionState()
    {
        collisionState = false;
    }
}
