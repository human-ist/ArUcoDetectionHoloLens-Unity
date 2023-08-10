using Microsoft.MixedReality.Toolkit.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MQTTController : MonoBehaviour
{
    public string controllerName = "robot/sensor_feedback";
    public string MQTTReceiverTag = "mqtt";
    public MQTTReceiver _eventSender;

    // Start is called before the first frame update
    void Start()
    {
        _eventSender = GameObject.FindGameObjectsWithTag(MQTTReceiverTag)[0].gameObject.GetComponent<MQTTReceiver>();
        _eventSender.OnMessageArrived += OnMessageArrivedHandler;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnMessageArrivedHandler(string newMsg)
    {
        Debug.Log("Event Fired. The message, from Object " + controllerName + " is = " + newMsg);
    }
}
