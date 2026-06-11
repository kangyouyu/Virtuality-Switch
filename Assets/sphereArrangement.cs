using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class sphereArrangement : MonoBehaviour
{

    public GameObject spherePrefab;
    public int numberOfSpheres = 12;
    public float nearDepth = 0.75f; // Near depth in meters
    public float farDepth = 4.0f; // Far depth in meters
    public float perceivedRadius = 1.5f; // Perceived radius of the circle in meters

    void Start()
    {
        initialization();
    }

    // Update is called once per frame
    void Update()
    {
        
    }



    void initialization(){
        float angleStep = 360f / numberOfSpheres;

        

        for (int i = 0; i < numberOfSpheres; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float depth = (i < numberOfSpheres / 2) ? nearDepth : farDepth; // Left half at near depth, right half at far depth

            // Calculate positions with depth correction to keep perceived radius consistent
            float actualRadius = perceivedRadius * (depth / depth); // Scale radius based on depth
            Vector3 position = new Vector3(Mathf.Cos(angle) * actualRadius, Mathf.Sin(angle) * actualRadius, depth);

            // Instantiate and scale the sphere
            GameObject sphere = Instantiate(spherePrefab, position, Quaternion.identity);
            //sphere.transform.localScale = CalculatePerceivedSize(depth);
            sphere.transform.parent = transform; // Set parent to maintain hierarchy
        }
    }

    Vector3 CalculatePerceivedSize(float depth)
    {   
        float offset=1.0f/0.04f;
        float angularSize = 4.0f; // Set a consistent angular size in degrees
        float sizeInMeters = 2 * depth * Mathf.Tan(angularSize * Mathf.Deg2Rad / 2);
        return new Vector3(sizeInMeters*offset, sizeInMeters*offset, sizeInMeters*offset);
    }

    
}
