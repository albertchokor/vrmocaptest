using UnityEngine;
using UltimateReplay;
using UltimateReplay.Storage;
using System.Collections;

namespace UltimateReplay.Example
{
    public class Ex03_ResumeRecording : MonoBehaviour
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

            // Wait for a little bit of time to pass
            yield return new WaitForSeconds(1f);

            // Resume recording
            ReplayManager.ResumeRecording(handle);
        }
    }
}
