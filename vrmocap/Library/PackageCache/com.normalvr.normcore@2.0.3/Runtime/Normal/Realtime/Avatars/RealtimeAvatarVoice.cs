using System;
using UnityEngine;
using Normal.Realtime.Native;
using Normal.Utility;
using UnityEngine.XR;

namespace Normal.Realtime {
    [ExecutionOrder(-1)] // Make sure our Update() runs before the default to ensure _microphoneDbLevel has been calculated for CalculateVoiceVolume()
    public class RealtimeAvatarVoice : RealtimeComponent<RealtimeAvatarVoiceModel> {
        public  float voiceVolume { get; private set; }

        private bool _mute = false;
        public  bool  mute { get { return _mute; } set { SetMute(value); } }
        
        private bool                   _hasMicrophone { get { return (_oculusMicrophoneDevice != null || _nativeMicrophoneDevice != null || _unityMicrophoneDevice != null); } }
        private OculusMicrophoneDevice _oculusMicrophoneDevice;
        private Native.Microphone      _nativeMicrophoneDevice;
        private MicrophoneDevice       _unityMicrophoneDevice;
        private AudioDeviceDataReader  _unityMicrophoneDeviceDataReader;
        private int                    _microphoneSampleRate;
        private int                    _microphoneChannels;
        private int                    _microphoneFrameSize;
        private float[]                _microphoneFrameData;
        private AudioInputStream       _microphoneStream;

        private AudioPreprocessor                 _audioPreprocessor;
        private AudioPreprocessorPlaybackListener _audioPreprocessorPlaybackListener;
        private float _microphoneDbLevel = -42.0f;

        private AudioOutput _audioOutput;

        /// <summary>
        /// True if we need to rebuild the audio stream because the model changed. This is cached as a bool so we can
        /// handle the audio stream connection during Update instead of the model change event handlers.
        /// </summary>
        private bool _rebuildAudioStream;
        
        void Update() {
            // Reconnect the audio stream if the client or stream ID changed.
            if (_rebuildAudioStream) ConnectAudioStream();
            
            // Send microphone data if needed
            SendMicrophoneData();

            // Calculate voice volume level
            CalculateVoiceVolume();
        }

        void OnDestroy() {
            if (model != null) {
                model.clientIDDidChange -= ClientIDUpdated;
                model.streamIDDidChange -= StreamIDUpdated;
            }
            
            DisconnectAudioStream();
        }

        private void CalculateVoiceVolume() {
            float averageDbSample = -42.0f;

            if (_hasMicrophone) {
                averageDbSample = _microphoneDbLevel;
            } else if (_audioOutput != null) {
                averageDbSample = _audioOutput.dbLevel;
            }

            // These are arbitrary values I picked from my own testing.
            float volumeMinDb = -42.0f;
            float volumeMaxDb = -5.0f;
            float volumeRange = volumeMaxDb - volumeMinDb;

            float normalizedVolume = (averageDbSample - volumeMinDb) / volumeRange;
            if (normalizedVolume < 0.0f)
                normalizedVolume = 0.0f;
            if (normalizedVolume > 1.0f)
                normalizedVolume = 1.0f;

            voiceVolume = normalizedVolume;
        }

        protected override void OnRealtimeModelReplaced(RealtimeAvatarVoiceModel previousModel, RealtimeAvatarVoiceModel currentModel) {
            if (previousModel != null) {
                previousModel.clientIDDidChange -= ClientIDUpdated;
                previousModel.streamIDDidChange -= StreamIDUpdated;
            }
            
            if (currentModel != null) {
                currentModel.clientIDDidChange += ClientIDUpdated;
                currentModel.streamIDDidChange += StreamIDUpdated;
            }
            
            _rebuildAudioStream = true;
        }

        private void ClientIDUpdated(RealtimeAvatarVoiceModel model, int clientID) {
            if (isOwnedLocallyInHierarchy) return;
            _rebuildAudioStream = true;
        }

        private void StreamIDUpdated(RealtimeAvatarVoiceModel model, int streamID) {
            if (isOwnedLocallyInHierarchy) return;
            _rebuildAudioStream = true;
        }

        #region Connect Audio Stream
        
        private void ConnectAudioStream() {
            DisconnectAudioStream();

            if (model == null) return;
            
            if (isOwnedLocallyInHierarchy) {
                ConnectLocalAudioStream();
            } else {
                ConnectRemoteAudioStream();
            }

            _rebuildAudioStream = false; // prevent rebuilding the audio stream until the model changes
        }

        private void ConnectLocalAudioStream() {
            // Local player, create microphone stream

            // First check if this platform supports our native microphone wrapper (lower latency + native echo cancellation if available)
            _microphoneSampleRate = 48000;
            _microphoneChannels   = 1;

            // Check for Oculus native microphone device API
            bool foundOculusMicrophoneDevice = false;
            if (OculusMicrophoneDevice.IsOculusPlatformAvailable()) {
                foundOculusMicrophoneDevice = OculusMicrophoneDevice.IsOculusPlatformInitialized();
                if (!foundOculusMicrophoneDevice && Application.platform == RuntimePlatform.Android)
                    Debug.LogWarning("Normcore: Oculus Platform SDK found, but it's not initialized. Oculus Quest native echo cancellation will be unavailable.");
            }

            if (foundOculusMicrophoneDevice) {
                // Create Oculus microphone device
                _oculusMicrophoneDevice = new OculusMicrophoneDevice();
                _oculusMicrophoneDevice.Start();
                _microphoneSampleRate = 48000;
                _microphoneChannels   = 1;
            } else if (Native.Microphone.PlatformSupported()) {
                _nativeMicrophoneDevice = new Native.Microphone();

                // If we failed to connect to the local microphone, bail.
                if (!_nativeMicrophoneDevice.Start()) {
                    Debug.LogError("Failed to connect to default microphone device. Make sure it is plugged in and functioning properly.");
                    _nativeMicrophoneDevice.Dispose();
                    _nativeMicrophoneDevice = null;
                    return;
                }

                _microphoneSampleRate = _nativeMicrophoneDevice.SampleRate();
                _microphoneChannels   = _nativeMicrophoneDevice.Channels();
            } else {
                // Create a microphone device
                _unityMicrophoneDevice = MicrophoneDevice.Start("");
                
                // If we failed to connect to the local microphone, bail.
                if (_unityMicrophoneDevice == null) {
                    Debug.LogError("Failed to connect to default microphone device. Make sure it is plugged in and functioning properly.");
                    return;
                }
                
                _unityMicrophoneDeviceDataReader = new AudioDeviceDataReader(_unityMicrophoneDevice);
                _microphoneSampleRate = _unityMicrophoneDevice.sampleRate;
                _microphoneChannels   = _unityMicrophoneDevice.numberOfChannels;
            }

            // Compute frame size with the sample rate of the microphone we received
            _microphoneFrameSize = _microphoneSampleRate / 100;

            // Preallocate the microphone frame buffer used in SendMicrophoneData
            _microphoneFrameData = new float[_microphoneFrameSize];

            // Create microphone stream with this sample rate (stream will automatically resample to 48000 before encoding with OPUS)
            _microphoneStream = room.CreateAudioInputStream(true, _microphoneSampleRate, _microphoneChannels);

            // Audio Preprocessor
            bool createAudioPreprocessor = Application.platform != RuntimePlatform.IPhonePlayer; // Create it for all platforms except iOS. iOS provides a nice built-in one.
            
            if (createAudioPreprocessor) {
                // Turn on echo cancellation for mobile devices;
                bool echoCancellation = Application.isMobilePlatform && Application.platform != RuntimePlatform.IPhonePlayer && XRSettings.loadedDeviceName != "Oculus";
                _audioPreprocessor = new AudioPreprocessor(_microphoneSampleRate,_microphoneFrameSize,                  // Input stream
                                                           true,                                                        // Automatic gain control
                                                           true,                                                        // Noise suppression
                                                           true,                                                        // Reverb suppression
                                                           echoCancellation, AudioSettings.outputSampleRate, 2, 0.28f); // Echo cancellation
                if (echoCancellation) {
                    // Find the audio listener in the scene so we can perform echo cancellation with it
                    AudioListener[] audioListeners = FindObjectsOfType<AudioListener>();
                    if (audioListeners.Length <= 0) {
                        Debug.LogWarning("RealtimeAvatarVoice: Unable to find any AudioListeners in the scene. RealtimeAvatarVoice will not be able to perform echo cancellation.");
                    } else {
                        AudioListener audioListener = audioListeners[0];
                        if (audioListeners.Length > 1)
                            Debug.LogWarning("RealtimeAvatarVoice: Multiple AudioListeners found in the scene. Performing echo cancellation with the first one: " + audioListener.gameObject.name);

                        _audioPreprocessorPlaybackListener = audioListener.gameObject.AddComponent<AudioPreprocessorPlaybackListener>();
                        _audioPreprocessorPlaybackListener.audioPreprocessor = _audioPreprocessor;
                    }
                }
            }
        }

        private void ConnectRemoteAudioStream() {
            // Remote player, lookup audio stream and create audio output
            int clientID = model.clientID;
            int streamID = model.streamID;

            // Ignore invalid state model state
            if (clientID < 0 || streamID < 0) return;
            
            // Find AudioOutputStream
            AudioOutputStream audioOutputStream = room.GetAudioOutputStream(clientID, streamID);
            if (audioOutputStream != null) {
                _audioOutput = gameObject.AddComponent<AudioOutput>();
                _audioOutput.mute = mute;
                _audioOutput.StartWithAudioOutputStream(audioOutputStream);
            } else {
                Debug.LogError($"RealtimeAvatarVoice: Unable to find matching audio stream for avatar (clientID: {clientID}, streamID: {streamID}).");
            }
        }
        
        #endregion

        private void DisconnectAudioStream() {
            if (_microphoneStream != null) {
                // Destroy AudioPreprocessorPlaybackListener
                if (_audioPreprocessorPlaybackListener != null) {
                    Destroy(_audioPreprocessorPlaybackListener);
                    _audioPreprocessorPlaybackListener = null;
                }

                // Dispose of audio preprocessor
                if (_audioPreprocessor != null) {
                    _audioPreprocessor.Dispose();
                    _audioPreprocessor = null;
                }

                // Close microphone stream
                _microphoneStream.Close();

                // Dispose microphone device
                if (_oculusMicrophoneDevice != null) {
                    _oculusMicrophoneDevice.Stop();
                    _oculusMicrophoneDevice.Dispose();
                    _oculusMicrophoneDevice = null;
                }
                if (_nativeMicrophoneDevice != null) {
                    _nativeMicrophoneDevice.Stop();
                    _nativeMicrophoneDevice.Dispose();
                    _nativeMicrophoneDevice = null;
                }
                if (_unityMicrophoneDevice != null) {
                    _unityMicrophoneDevice.Dispose();
                    _unityMicrophoneDevice = null;
                }

                // Clean up
                _unityMicrophoneDeviceDataReader = null;
                _microphoneStream = null;
            }

            // Remove audio output
            if (_audioOutput != null) {
                _audioOutput.Stop();
                Destroy(_audioOutput);
                _audioOutput = null;
            }
        }

        void SendMicrophoneData() {
            if (_microphoneStream == null)
                return;

            // Store the client ID / stream ID so remote clients can find the corresponding AudioOutputStream
            model.clientID = _microphoneStream.ClientID();
            model.streamID = _microphoneStream.StreamID();

            // Check if AudioInputStream is valid.
            if (model.clientID < 0 || model.streamID < 0)
                return;
            
            
            // Clear the previous microphone frame data
            Array.Clear(_microphoneFrameData, 0, _microphoneFrameData.Length);

            // Send audio data in _microphoneFrameSize chunks until we're out of microphone data to send
            bool didGetAudioData = false;
            while (GetMicrophoneAudioData(_microphoneFrameData)) {
                // If we have an _audioPreprocessor, preprocess the microphone data to remove noise and echo
                if (_audioPreprocessor != null)
                    _audioPreprocessor.ProcessRecordSamples(_microphoneFrameData);

                // Note that even when muted audio still needs to run through the audio processor to make sure echo cancellation works properly when mute is turned back off.
                if (_mute)
                    Array.Clear(_microphoneFrameData, 0, _microphoneFrameData.Length);

                // Send out microphone data
                _microphoneStream.SendRawAudioData(_microphoneFrameData);

                didGetAudioData = true;
            }

            // Normcore queues audio packets locally so they can go out in a single packet. This sends them off.
            if (didGetAudioData) {
                _microphoneStream.SendQueuedMessages();
            }

            // If we got audio data, update the current microphone level.
            // Note: I moved this here so that we do our volume level calculations on microphone audio that has run through the AudioPreprocessor.
            if (didGetAudioData) {
                int firstFrame = _microphoneFrameData.Length - 256;
                if (firstFrame < 0)
                    firstFrame = 0;
                int firstSample = firstFrame * _microphoneChannels;
                _microphoneDbLevel = StaticFunctions.CalculateAverageDbForAudioBuffer(_microphoneFrameData, firstSample);
            }
        }

        private bool GetMicrophoneAudioData(float[] audioData) {
            if (_oculusMicrophoneDevice != null)
                return _oculusMicrophoneDevice.GetAudioData(audioData);
            else if (_nativeMicrophoneDevice != null)
                return _nativeMicrophoneDevice.GetAudioData(audioData);
            else if (_unityMicrophoneDeviceDataReader != null)
                return _unityMicrophoneDeviceDataReader.GetData(audioData);
            
            return false;
        }

        void SetMute(bool mute) {
            if (mute == _mute)
                return;

            if (_audioOutput != null)
                _audioOutput.mute = mute;

            _mute = mute;
        }
    }
}
