using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class Rotate : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //READ KEYBOARD INPUT IF ANY
        Keyboard kboard = Keyboard.current;

        if (kboard.anyKey.wasPressedThisFrame)
        {
            char key = 'q';
            foreach (KeyControl k in kboard.allKeys)
            {
                if (k.wasPressedThisFrame)
                {
                    Debug.Log((int)k.keyCode + " " + k.path);
                    key = k.path[k.path.Length - 1];
                    Debug.Log(key);
                    break;
                }
            }
            if(key == '9')
            {
                Debug.Log("increase");
                transform.rotation *= Quaternion.AngleAxis(0.1f, Vector3.right);
            }
            if (key == '0')
            {
                Debug.Log("decrease");
                transform.rotation *= Quaternion.AngleAxis(-0.1f, Vector3.right);
            }
            if (key == '7')
            {
                Debug.Log("increase");
                transform.rotation *= Quaternion.AngleAxis(0.1f, Vector3.forward);
            }
            if (key == '8')
            {
                Debug.Log("decrease");
                transform.rotation *= Quaternion.AngleAxis(-0.1f, Vector3.forward);
            }
        }
    }
}
