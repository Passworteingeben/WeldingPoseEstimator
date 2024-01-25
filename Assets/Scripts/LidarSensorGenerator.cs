using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LidarSensorGenerator : MonoBehaviour
{
    List<GameObject> list_of_positions = new List<GameObject>();
    GameObject cage_ray_start_points;
    GameObject cage_ray_end_points;
    int point_count = 0;

    void generate_cage_ray_array_points(Vector3 target_point, float height, float radius, int num_angles)
    {
        RaycastHit hit;
        Vector3 start_point;
        Vector3 dir = Vector3.up;
        bool obstructed;
        float angle;
        float dist;
        for (int i = 0; i < num_angles; i++)
        {
            angle = 360f / num_angles * i * Mathf.Deg2Rad;
            start_point = new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
            dir = target_point - start_point;
            dist = dir.magnitude;
            dir = Vector3.Normalize(dir);
            obstructed = Physics.Raycast(start_point, dir, out hit, dist);
            if (!obstructed)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                sphere.transform.position = start_point;
                sphere.transform.parent = cage_ray_start_points.transform;
                sphere.name = point_count.ToString();
                sphere.tag = "ray_point";
                point_count += 1;
                sphere.layer = LayerMask.NameToLayer("Ignore Raycast");
            }
            draw_ray(start_point, dir, dist, hit);
        }

        if (cage_ray_end_points.transform.childCount == 0)
        {
            GameObject target_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            target_sphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            target_sphere.transform.position = target_point;
            target_sphere.name = "1";
            target_sphere.tag = "ray_point";
            target_sphere.transform.parent = cage_ray_end_points.transform;
            target_sphere.layer = LayerMask.NameToLayer("Ignore Raycast");
        }
    }

    void draw_ray(Vector3 start_point, Vector3 dir, float dist, RaycastHit hit, int time = 100)
    {
        if (ray_is_hit(hit))
        {
            Debug.DrawRay(start_point, dir * hit.distance, Color.red, time);
            // Debug.Log("hit");
            // Debug.Log(hit.distance);
        }
        else
        {
            Debug.DrawRay(start_point, dir * dist, Color.green, time);
            // Debug.Log("no hit");
        }
    }

    bool ray_is_hit(RaycastHit hit)
    {
        if (hit.collider != null)
        {
            if (hit.collider.tag == "target")
            {
                return false;
            }
        }
        return hit.collider != null;
    }



    public List<GameObject> generateToolRays(GameObject target_gameobj)
    {
        cage_ray_start_points = new GameObject("cage_ray_start_points");
        cage_ray_end_points = new GameObject("cage_ray_end_points");
        List<GameObject> output = new List<GameObject>{cage_ray_start_points, cage_ray_end_points};
        float height = 5.2f;

        List<float> circle_radii = new List<float>();
        circle_radii.Add(0.55f);
        circle_radii.Add(1f);
        circle_radii.Add(1.5f);
        circle_radii.Add(2.0f);
        circle_radii.Add(2.5f);
        circle_radii.Add(3f);
        circle_radii.Add(3.5f);

        List<int> angle_variants = new List<int>();
        angle_variants.Add(8);
        angle_variants.Add(16);
        angle_variants.Add(32);
        angle_variants.Add(32);
        angle_variants.Add(64);
        angle_variants.Add(64);
        angle_variants.Add(64);

        point_count = 0;
        for (int i = 0; i < circle_radii.Count; i++)
        {
            float radius = circle_radii[i];
            int num_angles = angle_variants[i];
            generate_cage_ray_array_points(target_gameobj.transform.position, height, radius, num_angles);
        }
        return output;
    }
}
