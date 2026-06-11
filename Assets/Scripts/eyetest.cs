using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR;
using UnityEngine.XR.MagicLeap;
using UnityEngine.InputSystem;
using UnityEngine.Video;
using UnityEngine.InputSystem.Controls;
using Unity.Mathematics;
using InputDevice = UnityEngine.XR.InputDevice;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Data.Common;


public class eyetest: MonoBehaviour
{
    

    private MagicLeapInputs mlInputs;
    private MagicLeapInputs.ControllerActions _controller;

    private MagicLeapInputs.EyesActions eyesActions;
    
    public GameObject eyetrackingVisualizer;

    private InputDevice eyeTrackingDevice;
    public bool EyeTrackingPermissionGranted { get; private set; }
    
    private GameObject fixationSphere;

   

  
    private bool isEyetrackingEnabled = true;

    

    public TMP_Text eyetracking_Text;

    private UnityEngine.InputSystem.XR.Eyes eyes;
    private readonly MLPermissions.Callbacks permissionCallbacks = new MLPermissions.Callbacks();

    void Start()
    {
        
        mlInputs = new MagicLeapInputs();
        mlInputs.Enable();
        _controller = new MagicLeapInputs.ControllerActions(mlInputs);
        

      

        MLPermissions.RequestPermission(MLPermission.EyeTracking, permissionCallbacks);

        if(MLPermissions.CheckPermission(MLPermission.EyeTracking).IsOk){
            eyetracking_Text.text="Eye Tracking permission denied";
            Debug.Log("eye tracking did not set up");
        }else{
            eyetracking_Text.text="eye tracking successfully set up";
            Debug.Log("eye tracking successfully set up");
        }

        fixationSphere = Instantiate(eyetrackingVisualizer, new Vector3(0, 1.3f, 0.4f), Quaternion.identity);
        
        fixationSphere.GetComponent<Renderer>().material.color = Color.green;

        
        //eyetracking_Text.text="if eye gaze permission granted: "+EyeTrackingPermissionGranted;

        
    }
    private void Awake()
    {
        permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
        permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
        permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;
        
    }
    void Update()
    {
       

        

        // Eye tracking

        if (!EyeTrackingPermissionGranted)
        {
            return;
        }

        if (!eyeTrackingDevice.isValid)
        {
            this.eyeTrackingDevice = InputSubsystem.Utils.FindMagicLeapDevice(InputDeviceCharacteristics.EyeTracking | InputDeviceCharacteristics.TrackedDevice);
            return;
        }

        
        eyetracking_Text.text="eyetrakcingdevice.isvalid is"+eyeTrackingDevice.isValid;

        //EYE TRACKING
        // Eye data provided by the engine for all XR devices.
        // Used here only to update the status text. The 
        // left/right eye centers are moved to their respective positions &
        // orientations using InputSystem's TrackedPoseDriver component.
        eyes = eyesActions.Data.ReadValue<UnityEngine.InputSystem.XR.Eyes>();

        // Eye data specific to Magic Leap
        InputSubsystem.Extensions.TryGetEyeTrackingState(eyeTrackingDevice, out var trackingState);
        
        //eyetracking_Text.text="gaze point pos: "+eyes.fixationPoint;

        if (trackingState.FixationConfidence > 0.1f)
        {
            Vector3 fixationPos = eyes.fixationPoint;
            //taskData.fixationPoints.Add(fixationPos);

            if (isEyetrackingEnabled)
            {
                fixationSphere.transform.position = fixationPos;
            }
        }

        

        Debug.Log("Gaze Fixation Point"+eyes.fixationPoint+"confidence: "+trackingState.FixationConfidence);


        
        
    }

    

    // Method to modify the size of the circles



    

    private void IsTrackedOnPerformed(InputAction.CallbackContext obj)
    {
        Debug.Log("The Controller Is tracking");
    }


    private void OnPermissionDenied(string permission)
    {
        Debug.Log($"Eye tracking permission denied.");
    }

    void OnPermissionGranted(string permission)
    {
        InputSubsystem.Extensions.MLEyes.StartTracking();
        eyesActions = new MagicLeapInputs.EyesActions(mlInputs);
        EyeTrackingPermissionGranted = true;
    }
}

