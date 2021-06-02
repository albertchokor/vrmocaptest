//========= Copyright 2018, HTC Corporation. All rights reserved. ===========
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Random = System.Random;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tobii.XR;
using Valve.VR;

namespace ViveSR.anipal.Eye
{
    public class eyescript : MonoBehaviour
    {
        [SerializeField] private Transform[] EyesModels = new Transform[0];

        public bool NeededToGetData = true;
        private GameObject[] EyeAnchors;
        private const int NUM_OF_EYES = 2;
        private static EyeData_v2 eyeData = new EyeData_v2();
        private bool eye_callback_registered = false;
        private void Start()
        {
            if (!SRanipal_Eye_Framework.Instance.EnableEye)
            {
                enabled = false;
                return;
            }

            SetEyesModels(EyesModels[0], EyesModels[1]);
        }

        private void Update()
        {
            if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING &&
                SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.NOT_SUPPORT) return;

            if (NeededToGetData)
            {
                if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == true && eye_callback_registered == false)
                {
                    SRanipal_Eye_v2.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
                    eye_callback_registered = true;
                }
                else if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == false && eye_callback_registered == true)
                {
                    SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
                    eye_callback_registered = false;
                }
                else if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == false)
                    SRanipal_Eye_API.GetEyeData_v2(ref eyeData);

                bool isLeftEyeActive = false;
                bool isRightEyeAcitve = false;
                if (SRanipal_Eye_Framework.Status == SRanipal_Eye_Framework.FrameworkStatus.WORKING)
                {
                    isLeftEyeActive = eyeData.no_user;
                    isRightEyeAcitve = eyeData.no_user;
                }
                else if (SRanipal_Eye_Framework.Status == SRanipal_Eye_Framework.FrameworkStatus.NOT_SUPPORT)
                {
                    isLeftEyeActive = true;
                    isRightEyeAcitve = true;
                }

                Vector3 GazeOriginCombinedLocal, GazeDirectionCombinedLocal = Vector3.zero;
                if (eye_callback_registered == true)
                {
                    if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.COMBINE, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal, eyeData)) { }
                    else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.LEFT, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal, eyeData)) { }
                    else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.RIGHT, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal, eyeData)) { }
                }
                else
                {
                    if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.COMBINE, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal)) { }
                    else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.LEFT, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal)) { }
                    else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.RIGHT, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal)) { }

                }
                UpdateGazeRay(GazeDirectionCombinedLocal);
            }
        }
        private void Release()
        {
            if (eye_callback_registered == true)
            {
                SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
                eye_callback_registered = false;
            }
        }
        private void OnDestroy()
        {
            DestroyEyeAnchors();
        }

        public void SetEyesModels(Transform leftEye, Transform rightEye)
        {
            if (leftEye != null && rightEye != null)
            {
                EyesModels = new Transform[NUM_OF_EYES] { leftEye, rightEye };
                DestroyEyeAnchors();
                CreateEyeAnchors();
            }
        }

        public void UpdateGazeRay(Vector3 gazeDirectionCombinedLocal)
        {
            for (int i = 0; i < EyesModels.Length; ++i)
            {
                Vector3 target = EyeAnchors[i].transform.TransformPoint(gazeDirectionCombinedLocal);
                EyesModels[i].LookAt(target);
            }
        }

        private void CreateEyeAnchors()
        {
            EyeAnchors = new GameObject[NUM_OF_EYES];
            for (int i = 0; i < NUM_OF_EYES; ++i)
            {
                EyeAnchors[i] = new GameObject();
                EyeAnchors[i].name = "EyeAnchor_" + i;
                EyeAnchors[i].transform.SetParent(gameObject.transform);
                EyeAnchors[i].transform.localPosition = EyesModels[i].localPosition;
                EyeAnchors[i].transform.localRotation = EyesModels[i].localRotation;
                EyeAnchors[i].transform.localScale = EyesModels[i].localScale;
            }
        }

        private void DestroyEyeAnchors()
        {
            if (EyeAnchors != null)
            {
                foreach (var obj in EyeAnchors)
                    if (obj != null) Destroy(obj);
            }
        }
        private static void EyeCallback(ref EyeData_v2 eye_data)
        {
            eyeData = eye_data;
        }
    }
}