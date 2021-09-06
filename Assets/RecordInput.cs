using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using OVRTouchSample;

public class RecordInput : MonoBehaviour
{
    public UnityEvent onButtonUp;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(OVRInput.GetUp(OVRInput.Button.Start) || Input.GetKeyUp(KeyCode.A))
        {
            onButtonUp.Invoke();
        }
    }
}
