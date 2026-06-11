using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.MagicLeap;
using InputDevice = UnityEngine.XR.InputDevice;


public class StudyManager : MonoBehaviour
{
    public GameObject mainCamera;
        public Transform fixationPoint;

        // Used to get ml inputs.
        private MagicLeapInputs mlInputs;

        // Used to get eyes action data.
        private MagicLeapInputs.EyesActions eyesActions;

        // Used to get other eye data
        private InputDevice eyesDevice;

        // Was EyeTracking permission granted by user
        private bool permissionGranted = false;
        private readonly MLPermissions.Callbacks permissionCallbacks = new MLPermissions.Callbacks();

        private void Awake()
        {
            permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
            permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
            permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;
        }

        private void Start()
        {
            mlInputs = new MagicLeapInputs();
            mlInputs.Enable();

            MLPermissions.RequestPermission(MLPermission.EyeTracking, permissionCallbacks);
        }

        private void OnDestroy()
        {
            permissionCallbacks.OnPermissionGranted -= OnPermissionGranted;
            permissionCallbacks.OnPermissionDenied -= OnPermissionDenied;
            permissionCallbacks.OnPermissionDeniedAndDontAskAgain -= OnPermissionDenied;

            mlInputs.Disable();
            mlInputs.Dispose();

            InputSubsystem.Extensions.MLEyes.StopTracking();
        }

        private void Update()
        {
            if (!permissionGranted)
            {
                return;
            }

            if (!eyesDevice.isValid)
            {
                this.eyesDevice = InputSubsystem.Utils.FindMagicLeapDevice(InputDeviceCharacteristics.EyeTracking | InputDeviceCharacteristics.TrackedDevice);
                return;
            }

            // Eye data provided by the engine for all XR devices.
            // Used here only to update the status text. The 
            // left/right eye centers are moved to their respective positions &
            // orientations using InputSystem's TrackedPoseDriver component.
            var eyes = eyesActions.Data.ReadValue<UnityEngine.InputSystem.XR.Eyes>();

            // Manually set fixation point marker so we can apply rotation, since UnityXREyes
            // does not provide it
            fixationPoint.position = eyes.fixationPoint;
            fixationPoint.rotation = Quaternion.LookRotation(eyes.fixationPoint - Camera.main.transform.position);

            // Eye data specific to Magic Leap
            InputSubsystem.Extensions.TryGetEyeTrackingState(eyesDevice, out var trackingState);

            string bothEyesText =
                $"Fixation Point:\n({eyes.fixationPoint.x:F2}, {eyes.fixationPoint.y:F2}, {eyes.fixationPoint.z:F2})\n" +
                $"Confidence:\n{trackingState.FixationConfidence:F2}";


            if (trackingState.RightBlink || trackingState.LeftBlink)
            {
                Debug.Log($"Eye Tracking Blink Registered Right Eye Blink: {trackingState.RightBlink} Left Eye Blink: {trackingState.LeftBlink}");
            }
        }

        private void OnPermissionDenied(string permission)
        {
            MLPluginLog.Error($"{permission} denied, example won't function.");
        }

        private void OnPermissionGranted(string permission)
        {
            InputSubsystem.Extensions.MLEyes.StartTracking();
            eyesActions = new MagicLeapInputs.EyesActions(mlInputs);
            permissionGranted = true;
        }
    
}
