using UnityEngine;
using System.Linq;
using System.Collections.Generic;



public class MeshOrientation : MonoBehaviour
{
    public Vector3 normal;
    public Material originalMaterial;

    public MeshRenderer renderer;

    public void resetColor()
    {
        if (renderer == null)
        {
            renderer = this.gameObject.GetComponent<MeshRenderer>();
        }
        if (originalMaterial != null)
        {
            renderer.material = originalMaterial;
        }
    }
}