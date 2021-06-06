using UnityEngine;
using UltimateReplay;
using UltimateReplay.Storage;

namespace UltimateReplay.Example
{
    // record handle is assigned but never used
#pragma warning disable 0414

    public class Ex01_SimpleRecording : MonoBehaviour
    {
        // Private
        // A replay handle is used to keep track or record or playback operations
        private ReplayHandle recordHandle = ReplayHandle.invalid;

        // Methods
        public void Start()
        {
            // Create a memory storage target to store the replay
            ReplayStorageTarget storage = new ReplayMemoryTarget();

            // Begin recording to the specified storage target.
            // We should store the returned replay handle as all other replay operations will expect this value as a parameter to identify the recording
            // By default, all replay objects in the active scene will be recorded.
            recordHandle = ReplayManager.BeginRecording(storage);
        }
    }

#pragma warning restore 0414
}
