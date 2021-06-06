using UnityEngine;

namespace UltimateReplay.Example
{
    public class AudioPlayer : MonoBehaviour
    {
        // Public
        public AudioSource targetAudio;

        // Methods
        public void OnGUI()
        {
            // Only allow play when recording
            if (ReplayManager.IsRecordingAny == false)
                return;

            if(GUILayout.Button("Play Audio") == true)
            {
                if (targetAudio != null)
                    targetAudio.Play();
            }
        }
    }
}