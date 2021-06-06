using UnityEngine;
using UltimateReplay;
using UltimateReplay.Storage;
using System.Collections;

namespace UltimateReplay.Example
{
    public class Ex02_PauseRecording : MonoBehaviour
    {
        // Methods
        public IEnumerator Start()
        {
            // Record as normal
            ReplayHandle handle = ReplayManager.BeginRecording(new ReplayMemoryTarget());

            // Allow recording to run for 1 second
            yield return new WaitForSeconds(1f);

            // Pause recording
            // This will supend recording and we can resume at a later date so long as we have the replay handle object
            ReplayManager.PauseRecording(handle);
        }
    }
}
