using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIFollowCamera : MonoBehaviour
{
    public Camera mainCamera;  // Assign your main camera here
    //public Vector3 offset;     // Set offset in the Inspector or calculate it dynamically in Start()

    private Vector3 initialOffset;

    void Start()
    {
        // Calculate the initial offset between the InputField and the camera
        if (mainCamera == null)
        {
            mainCamera = Camera.main;  // Use the main camera if none is assigned
        }

        initialOffset = transform.position - mainCamera.transform.position;
    }

    void LateUpdate()
    {
        // Update the position of the InputField based on the camera's position and rotation
        Vector3 targetPosition = mainCamera.transform.position + mainCamera.transform.rotation * initialOffset;
        transform.position = targetPosition;

        // Optionally, make the InputField face the camera
        transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
    }
}
