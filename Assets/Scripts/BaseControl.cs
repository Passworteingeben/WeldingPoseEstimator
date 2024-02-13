using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEditor;
using System.Linq;

public class BaseControl : MonoBehaviour
{
    private Vector3 FORCE_RIGHT = new Vector3(1.0f, 0.0f, 0.0f);
    private Vector3 FORCE_UP = new Vector3(0.0f, 1.0f, 0.0f);

    private float TRANSLATION_FACTOR = 0.05f;
    // public float ROTATION_FACTOR = 0.25f*16;
    private float ROTATION_FACTOR = 0.5f;
    private Vector3 FORCE_FORWARD = new Vector3(0.0f, 0.0f, 1.0f);

    public void moveAgent(int[] actionArray)
    {
        // print input arr
        actionArray = actionArray.Select( x => x != 2 ? x : -1).ToArray(); 
        // Debug.Log(string.Join(" ", actionArray));
        Vector3 translationForce =TRANSLATION_FACTOR * (
                                    (actionArray[0])* this.transform.forward +
                                    (actionArray[1]) * this.transform.up +
                                    (actionArray[2]) * this.transform.right);
        Vector3 rotationAngles = new Vector3((actionArray[3]), 
                                            (actionArray[4]), 
                                            (actionArray[5]))* ROTATION_FACTOR;

        this.transform.localPosition += translationForce;
        rotationAngles += this.transform.localEulerAngles;
        rotationAngles.z = 0f;
        this.transform.localEulerAngles = rotationAngles;
        // legacy
        // body.AddForce(translation_force,ForceMode.VelocityChange);
        // gameObject.transform.Rotate(rotation_force[0],rotation_force[1],rotation_force[2], Space.Self);
    }


    // public void pause()
    // {
    //     EditorApplication.isPaused = true;
    // }

    void printArray(int[] arr)
    {
        Debug.Log(string.Join(" ", arr));
    }
}
