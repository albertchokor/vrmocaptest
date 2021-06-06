using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UltimateReplay.Storage;
using UnityEngine;

namespace UltimateReplay.Demo
{
    public class SimpleKillcamPlayback : ReplayBehaviour
    {
        // Private
        private ReplayMemoryTarget killcamStorage = null;
        private ReplayHandle killcamHandle = ReplayHandle.invalid;
        private bool isReplaying = false;

        // Public
        public Camera killcamViewCamera;
        public Canvas killcamViewCanvas;
        public float recordKillcamSeconds = 10;

        // Properties
        public bool IsReplayingKillcam
        {
            get { return isReplaying; }
        }

        // Methods
        public override void Awake()
        {
            base.Awake();

            // Create a rolling memory buffer of 10 seconds
            killcamStorage = ReplayMemoryTarget.CreateTimeLimitedRolling(recordKillcamSeconds);

            // Start recording
            killcamHandle = ReplayManager.BeginRecording(killcamStorage);
        }

        public void PlayKillcam()
        {
            // Stop recording
            if (ReplayManager.IsRecording(killcamHandle) == true)
                ReplayManager.StopRecording(ref killcamHandle);

            // Start replaying
            killcamHandle = ReplayManager.BeginPlayback(killcamStorage);
            isReplaying = true;

            // Activate camera
            killcamViewCamera.enabled = true;
            killcamViewCanvas.gameObject.SetActive(true);
            killcamViewCamera.GetComponent<AudioListener>().enabled = true;

            ReplayManager.AddPlaybackEndListener(killcamHandle, OnKillcamEnd);
        }

        private void OnKillcamEnd()
        {
            killcamViewCamera.enabled = false;
            killcamViewCanvas.gameObject.SetActive(false);
            isReplaying = false;

            // Reset recorder storage to avoid issues
            //killcamStorage.Clear();

            StartCoroutine(RestartKillcamRecording());
        }

        private IEnumerator RestartKillcamRecording()
        {
            // Wait a frame
            yield return null;

            // Start recording again
            killcamStorage.Clear();

            killcamHandle = ReplayManager.BeginRecording(killcamStorage);
        }

        public void ResetKillcam()
        {
            // Stop recording
            if (ReplayManager.IsRecording(killcamHandle) == true)
                ReplayManager.StopRecording(ref killcamHandle);

            killcamStorage.Clear();

            killcamHandle = ReplayManager.BeginRecording(killcamStorage);
        }
    }
}
