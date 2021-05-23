using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class trackPoint : MonoBehaviour
{
    private float timeElapsed;
    private List<Vector3> positions = new List<Vector3>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timeElapsed += Time.deltaTime;
        if (timeElapsed >= 1) {
            positions.Add(transform.position);
            timeElapsed = 0;
        }
    }

    void OnDestroy()
    {
        string outputString = "";
        foreach(Vector3 position in positions){
            //three decimal places
            outputString += position.ToString("F3");
        }

        System.IO.File.WriteAllText(@"C:\Users\alber\Downloads\data.txt", outputString);
    }
}
