using System.Collections;
using UnityEngine;

namespace UltimateReplay.Example
{
    public class Ex_RecordEvent : ReplayBehaviour
    {
        private enum MyEventType : ushort
        {
            Event1,
            Event2,
            Event3,
            // etc.
        }

        // Methods
        public IEnumerator Start()
        {
            while(true)
            {
                // Wait until we are recording
                if (IsRecording == false)
                    yield return null;

                // Record event type 1
                yield return new WaitForSeconds(1f);
                RecordEvent((ushort)MyEventType.Event1);

                // Record event type 2
                yield return new WaitForSeconds(1f);
                RecordEvent((ushort)MyEventType.Event2);

                // Record event type 3 with extra data
                ReplayState eventData = ReplayState.pool.GetReusable();
                eventData.Write("Hello World");

                yield return new WaitForSeconds(1f);
                RecordEvent((ushort)MyEventType.Event3, eventData);
            }
        }

        public override void OnReplayEvent(ushort eventID, ReplayState eventData)
        {
            switch((MyEventType)eventID)
            {
                case MyEventType.Event1:
                    {
                        Debug.Log("Event 1 was called");
                        break;
                    }

                case MyEventType.Event2:
                    {
                        Debug.Log("Event 2 was called");
                        break;
                    }

                case MyEventType.Event3:
                    {
                        Debug.Log("Event 3 was called with message: " + eventData.ReadString());
                        break;
                    }
            }
        }
    }
}
