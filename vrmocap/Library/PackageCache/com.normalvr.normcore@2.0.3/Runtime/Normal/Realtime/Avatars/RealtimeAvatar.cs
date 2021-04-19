using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Normal.Utility;

namespace Normal.Realtime {
    [ExecutionOrder(-95)] // Make sure our Update() runs before the default to so that the avatar positions are as up to date as possible when everyone else's Update() runs.
    public class RealtimeAvatar : RealtimeComponent<RealtimeAvatarModel> {
        // Local Player
        [Serializable]
        public class LocalPlayer {
            public Transform root;
            public Transform head;
            public Transform leftHand;
            public Transform rightHand;
        }
        public  LocalPlayer  localPlayer { get { return _localPlayer; } set { SetLocalPlayer(value); } }
#pragma warning disable 0649 // Disable variable is never assigned to warning.
        private LocalPlayer _localPlayer;
#pragma warning restore 0649

        // Device Type
        public enum DeviceType : uint {
            Unknown = 0,
            OpenVR  = 1,
            Oculus  = 2,
        }

        /// <summary>
        /// The XR device type of the client that owns this avatar. See RealtimeAvatar#DeviceType for values.
        /// </summary>
        public DeviceType deviceType {
            get => model.deviceType;
            set => model.deviceType  = value;
        }

        /// <summary>
        /// The XRDevice.model of the client that owns this avatar.
        /// </summary>
        public string deviceModel {
            get => model.deviceModel;
            set => model.deviceModel = value;
        }

        // Prefab
        public Transform head      { get { return _head;      } }
        public Transform leftHand  { get { return _leftHand;  } }
        public Transform rightHand { get { return _rightHand; } }
#pragma warning disable 0649 // Disable variable is never assigned to warning.
        [SerializeField] private Transform _head;
        [SerializeField] private Transform _leftHand;
        [SerializeField] private Transform _rightHand;
#pragma warning restore 0649

        private RealtimeAvatarManager _realtimeAvatarManager;

        private static List<XRNodeState> _nodeStates = new List<XRNodeState>();

        void Start() {
            // Register with RealtimeAvatarManager
            try {
                _realtimeAvatarManager = realtime.GetComponent<RealtimeAvatarManager>();
                _realtimeAvatarManager._RegisterAvatar(realtimeView.ownerIDSelf, this);
            } catch (NullReferenceException) {
                Debug.LogError("RealtimeAvatar failed to register with RealtimeAvatarManager component. Was this avatar prefab instantiated by RealtimeAvatarManager?");
            }
        }

        void OnDestroy() {
            // Unregister with RealtimeAvatarManager
            if (_realtimeAvatarManager != null)
                _realtimeAvatarManager._UnregisterAvatar(this);

            // Unregister for events
            localPlayer = null;
        }
    
        void FixedUpdate() {
            UpdateAvatarTransformsForLocalPlayer();
        }
    
        void Update() {
            UpdateAvatarTransformsForLocalPlayer();
        }
    
        void LateUpdate() {
            UpdateAvatarTransformsForLocalPlayer();
        }

        protected override void OnRealtimeModelReplaced(RealtimeAvatarModel previousModel, RealtimeAvatarModel currentModel) {
            if (previousModel != null) {
                previousModel.headActiveDidChange      -= ActiveStateChanged;
                previousModel.leftHandActiveDidChange  -= ActiveStateChanged;
                previousModel.rightHandActiveDidChange -= ActiveStateChanged;
            }

            if (currentModel != null) {
                currentModel.headActiveDidChange      += ActiveStateChanged;
                currentModel.leftHandActiveDidChange  += ActiveStateChanged;
                currentModel.rightHandActiveDidChange += ActiveStateChanged;
            }
        }

        void SetLocalPlayer(LocalPlayer localPlayer) {
            if (localPlayer == _localPlayer)
                return;

            _localPlayer = localPlayer;
    
            if (_localPlayer != null) {
                RealtimeTransform      rootRealtimeTransform =                                 GetComponent<RealtimeTransform>();
                RealtimeTransform      headRealtimeTransform =      _head != null ?      _head.GetComponent<RealtimeTransform>() : null;
                RealtimeTransform  leftHandRealtimeTransform =  _leftHand != null ?  _leftHand.GetComponent<RealtimeTransform>() : null;
                RealtimeTransform rightHandRealtimeTransform = _rightHand != null ? _rightHand.GetComponent<RealtimeTransform>() : null;
                if (     rootRealtimeTransform != null)      rootRealtimeTransform.RequestOwnership();
                if (     headRealtimeTransform != null)      headRealtimeTransform.RequestOwnership();
                if ( leftHandRealtimeTransform != null)  leftHandRealtimeTransform.RequestOwnership();
                if (rightHandRealtimeTransform != null) rightHandRealtimeTransform.RequestOwnership();
            }
        }

        void ActiveStateChanged(RealtimeAvatarModel model, bool nodeIsActive) {
            // Leave the head active so RealtimeAvatarVoice runs even when the head isn't tracking.
            if (_leftHand != null)  _leftHand.gameObject.SetActive(model.leftHandActive);
            if (_rightHand != null) _rightHand.gameObject.SetActive(model.rightHandActive);
        }

        void UpdateAvatarTransformsForLocalPlayer() {
            // Make sure this avatar is a local player
            if (_localPlayer == null)
                return;

            // Flags to fetch XRNode position/rotation state
            bool updateHeadWithXRNode      = false;
            bool updateLeftHandWithXRNode  = false;
            bool updateRightHandWithXRNode = false;

            // Root
            if (_localPlayer.root != null) {
                transform.position   = _localPlayer.root.position;
                transform.rotation   = _localPlayer.root.rotation;
                transform.localScale = _localPlayer.root.localScale;
            }

            // Head
            if (_localPlayer.head != null) {
                model.headActive = _localPlayer.head.gameObject.activeSelf;

                _head.position = _localPlayer.head.position;
                _head.rotation = _localPlayer.head.rotation;
            } else {
                updateHeadWithXRNode = true;
            }

            // Left Hand
            if (_leftHand != null) {
                if (_localPlayer.leftHand != null) {
                    model.leftHandActive = _localPlayer.leftHand.gameObject.activeSelf;
                
                    _leftHand.position = _localPlayer.leftHand.position;
                    _leftHand.rotation = _localPlayer.leftHand.rotation;
                } else {
                    updateLeftHandWithXRNode = true;
                }
            }

            // Right Hand
            if (_rightHand != null) {
                if (_localPlayer.rightHand != null) {
                    model.rightHandActive = _localPlayer.rightHand.gameObject.activeSelf;
                
                    _rightHand.position = _localPlayer.rightHand.position;
                    _rightHand.rotation = _localPlayer.rightHand.rotation;
                } else {
                    updateRightHandWithXRNode = true;
                }
            }

            // Update head/hands using XRNode APIs if needed
            if (updateHeadWithXRNode || updateLeftHandWithXRNode || updateRightHandWithXRNode) {
                InputTracking.GetNodeStates(_nodeStates); // the list is cleared by GetNodeStates

                bool      headActive = false;
                bool  leftHandActive = false;
                bool rightHandActive = false;

                foreach (XRNodeState nodeState in _nodeStates) {
                    if (nodeState.nodeType == XRNode.Head && updateHeadWithXRNode) {
                        headActive = nodeState.tracked;
                        UpdateTransformWithNodeState(_head, nodeState);
                    } else if (nodeState.nodeType == XRNode.LeftHand && updateLeftHandWithXRNode) {
                        leftHandActive = nodeState.tracked;
                        UpdateTransformWithNodeState(_leftHand, nodeState);
                    } else if (nodeState.nodeType == XRNode.RightHand && updateRightHandWithXRNode) {
                        rightHandActive = nodeState.tracked;
                        UpdateTransformWithNodeState(_rightHand, nodeState);
                    }
                }

                if (     updateHeadWithXRNode) model.headActive      =      headActive;
                if ( updateLeftHandWithXRNode) model.leftHandActive  =  leftHandActive;
                if (updateRightHandWithXRNode) model.rightHandActive = rightHandActive;
            }
        }

        private static void UpdateTransformWithNodeState(Transform transform, XRNodeState state) {
            if (state.TryGetPosition(out Vector3 position)) {
                transform.localPosition = position;
            }

            if (state.TryGetRotation(out Quaternion rotation)) {
                transform.localRotation = rotation;
            }
        }
    }
}
