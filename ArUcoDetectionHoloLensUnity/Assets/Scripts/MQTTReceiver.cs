using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using M2MqttUnity;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using UnityEngine.UI;
using System;
using UnityEngine.SocialPlatforms;

public class MQTTReceiver : M2MqttUnityClient
{
    [Header("MQTT topics")]
    [Tooltip("Set the topic(s) to subscribe. !!!ATTENTION!!! multi-level wildcard # subscribes to all topics")]
    public string topicSubscribe = "#"; // topic to subscribe. !!! The multi-level wildcard # is used to subscribe to all the topics. Attention i if #, subscribe to all topics. Attention if MQTT is on data plan
    [Tooltip("Topics")]
    public string topicPublish = "robot/opendays"; // topic to publish
    public string destinationTopicPublish = "robot/destination";

    [Tooltip("Set this to true to perform a testing cycle automatically on startup")]
    public bool autoTest = false;

    [Header("Gameobjects")]
    public GameObject connectionMessage;
    public GameObject sensorInformation;
    public GameObject headingInformation;
    public GameObject distanceInformation;
    public GameObject markerReference;
    public GameObject robotObject;
    public GameObject goalObject;
    
    [Header("Sensors")]
    public GameObject[] sensorFeedback;
    public float[] sensor_positions;
    private const int SENSOR_THRESHOLD = 1000;
    private const float MAX_RADIUS = .08f; //.13f;

    // navigation properties
    private bool isDestinationSet = false;
    private float time = 0.0f;
    private const float MARKER_VALIDITY_THRESH = 0.05f;
    private const float PUBLISH_TIME = 0.2f;

    //using C# Property GET/SET and event listener to reduce Update overhead in the controlled objects
    private string m_msg;

    public string msg
    {
        get
        {
            return m_msg;
        }
        set
        {
            if (m_msg == value) return;
            m_msg = value;
            if (OnMessageArrived != null)
            {
                OnMessageArrived(m_msg);
            }
        }
    }

    public event OnMessageArrivedDelegate OnMessageArrived;
    public delegate void OnMessageArrivedDelegate(string newMsg);

    //using C# Property GET/SET and event listener to expose the connection status
    private bool m_isConnected;

    public bool isConnected
    {
        get
        {
            return m_isConnected;
        }
        set
        {
            if (m_isConnected == value) return;
            m_isConnected = value;
            if (OnConnectionSucceeded != null)
            {
                OnConnectionSucceeded(isConnected);
            }
        }
    }
    public event OnConnectionSucceededDelegate OnConnectionSucceeded;
    public delegate void OnConnectionSucceededDelegate(bool isConnected);

    // a list to store the messages
    private List<string> eventMessages = new List<string>();


    public void Publish()
    {
        string messagePublish = "test";
        client.Publish(topicPublish, System.Text.Encoding.UTF8.GetBytes(messagePublish), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
        Debug.Log("Published topic " +topicPublish + " with message " +messagePublish);
    }

    public void PublishDestination(string message)
    {
        client.Publish(destinationTopicPublish, System.Text.Encoding.UTF8.GetBytes(message), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
        Debug.Log("Published topic " + destinationTopicPublish + " with message " + message);
    }

    public void PublishOnButtonClick(string msg = "")
    {
        client.Publish(topicPublish, System.Text.Encoding.UTF8.GetBytes(msg), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
        Debug.Log("Test message published");
    }
    public void SetEncrypted(bool isEncrypted)
    {
        this.isEncrypted = isEncrypted;
    }

    protected override void OnConnecting()
    {
        base.OnConnecting();
        connectionMessage.GetComponent<Text>().text = "Connection state: CONNECTING";
    }

    protected override void OnConnected()
    {
        base.OnConnected();
        isConnected = true;

        if (autoTest)
        {
            Publish();
        }

        connectionMessage.GetComponent<Text>().text = "Connection state: CONNECTED";
    }

    protected override void OnConnectionFailed(string errorMessage)
    {
        Debug.Log("CONNECTION FAILED! " + errorMessage);
        connectionMessage.GetComponent<Text>().text = "Connection state: FAILED";
    }

    protected override void OnDisconnected()
    {
        Debug.Log("Disconnected.");
        isConnected = false;
        connectionMessage.GetComponent<Text>().text = "Connection state: DISCONNECTED";
    }

    protected override void OnConnectionLost()
    {
        Debug.Log("CONNECTION LOST!");
        connectionMessage.GetComponent<Text>().text = "Connection state: CONNECTION LOST";
    }

    protected override void SubscribeTopics()
    {
        client.Subscribe(new string[] { topicSubscribe }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
    }

    protected override void UnsubscribeTopics()
    {
        client.Unsubscribe(new string[] { topicSubscribe });
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void DecodeMessage(string topic, byte[] message)
    {
        //The message is decoded
        msg = System.Text.Encoding.UTF8.GetString(message);
        Debug.Log("Received: " + msg + " from topic: " + topic);

        // store and handle message
        StoreMessage(msg);
        TopicHandler(topic, msg);
    }

    private void StoreMessage(string eventMsg)
    {
        if (eventMessages.Count > 50)
        {
            eventMessages.Clear();
        }
        eventMessages.Add(eventMsg);
    }

    protected override void Update()
    {
        base.Update(); // call ProcessMqttEvents()

        if (!isConnected)
            return;

        // publish every 0.2 secs to avoid spamming the robot
        if (isDestinationSet && time >= PUBLISH_TIME)
        {
            Vector3 projectedRobot = new Vector3(robotObject.transform.position.x, 0.0f, robotObject.transform.position.z);
            Vector3 projectedGoal = new Vector3(goalObject.transform.position.x, 0.0f, goalObject.transform.position.z);

            Vector3 direction = projectedGoal - projectedRobot;
            Vector3 robotForward = robotObject.transform.up;
            robotForward.y = 0.0f;

            PublishDestination("{'ValidDestination': 1, " +
                "'Heading': " + Vector3.SignedAngle(robotForward, direction, Vector3.up).ToString() + 
                ", 'Distance': " + Vector3.Magnitude(direction).ToString() + "}");

            headingInformation.GetComponent<Text>().text = "Heading: " + Vector3.SignedAngle(robotForward, direction, Vector3.up).ToString("F2") + " deg";
            float magnitude_cm = (Vector3.Magnitude(direction) * 100) -2f;
            distanceInformation.GetComponent<Text>().text = "Distance: " + magnitude_cm.ToString("F2") + " cm";

            time = 0.0f;
        }

        time += Time.deltaTime;
    }

    private void OnDestroy()
    {
        Disconnect();
    }

    private void OnValidate()
    {
        if (autoTest)
        {
            autoConnect = true;
        }
    }

    private void TopicHandler(string topic, string msg)
    {
        if (topic == "robot/sensor_feedback")
        {
            ProcessSensorFeedback(msg);
        }
        else if (topic == "robot/goal_reached")
        {
            RemoveDestinationMarker();
        }
    }

    private void ProcessSensorFeedback(string msg)
    {
        Debug.Log("sensor values is: " + msg);
        float[] sensor_values = Array.ConvertAll(msg.Split(' '), float.Parse);
        Color color = new Color();
        float radius = MAX_RADIUS;
        sensorInformation.GetComponent<Text>().text = "Prox sensor: " + sensor_values[0].ToString();

        for (int i = 0; i < sensor_values.Length; i++)
        {
            /*
             * Due to the imprecision of the proximity sensors, the thresholds are hard coded
             */
            int sensor_value = (int)sensor_values[i];
            
            if (sensor_value < 50)
            {
                // don't render
                sensorFeedback[i].gameObject.SetActive(false);
                continue;
            }
            else if ((sensor_value >= 50) && (sensor_value < 200))
            {
                // set color to green
                color = Color.green;
                sensorFeedback[i].gameObject.SetActive(true);
                radius = 0.08f;
            }
            else if ((sensor_value >= 200) && (sensor_value < 500))
            {
                // set color to yellow
                color = Color.yellow;
                sensorFeedback[i].gameObject.SetActive(true);
                radius = 0.07f;
            }
            else if ((sensor_value >= 500) && (sensor_value < 1000))
            {
                // set color to orange
                color = new Color(0.92f, 0.584f, 0.196f, 1f);
                sensorFeedback[i].gameObject.SetActive(true);
                radius = 0.06f;
            }
            else
            {
                // set color to red
                color = Color.red;
                sensorFeedback[i].gameObject.SetActive(true);
                radius = 0.05f;
            }
            
            RenderSensorValue(sensor_positions[i], radius, color, sensorFeedback[i].gameObject.GetComponent<LineRenderer>());

            /*
             * Due to the imprecision of the proximity sensors, the following is no longer used
             */
            /*int threshold = (int) sensor_values[i] / SENSOR_THRESHOLD;
            radius = MAX_RADIUS - (threshold-1)*.01f;

            switch (threshold)
            {
                case 0:
                    // don't render
                    sensorFeedback[i].gameObject.SetActive(false);
                    break;
                case 1:
                    // set color to green
                    color = Color.green;
                    sensorFeedback[i].gameObject.SetActive(true);
                    break; 
                case 2:
                    // set color to yellow
                    color = Color.yellow;
                    sensorFeedback[i].gameObject.SetActive(true);
                    break;
                case 3:
                    // set color to orange
                    color = new Color(0.92f, 0.584f, 0.196f, 1f);
                    sensorFeedback[i].gameObject.SetActive(true);
                    break;
                case 4:
                    // set color to red
                    color = Color.red;
                    sensorFeedback[i].gameObject.SetActive(true);
                    break;
            }

            if (threshold > 0)
                RenderSensorValue(sensor_positions[i], radius, color, sensorFeedback[i].gameObject.GetComponent<LineRenderer>());*/
        }
    }

    private void RenderSensorValue(float sensorAngle, float radius, Color color, LineRenderer sensorRenderer, float fov = 30f, int nbPos = 150)
    {
        float frontAngle = 90f;
        float sensorAbsoluteAngle = frontAngle + sensorAngle;
        float startAngle = sensorAbsoluteAngle - fov/2;

        // positions for the linerenderer
        Vector3[] positions;
        positions = new Vector3[nbPos];
        Vector3 axisX = markerReference.transform.right;
        Vector3 axisY = markerReference.transform.up;
        
        // origin references
        Vector3 origin = markerReference.transform.position;

        for (int i = 0; i < nbPos; i++)
        {
            positions[i] = origin + radius * Mathf.Cos((startAngle + fov * i / (nbPos - 1)) * Mathf.Deg2Rad) * axisX 
                + radius * Mathf.Sin((startAngle + fov * i / (nbPos - 1)) * Mathf.Deg2Rad) * axisY;
        }

        sensorRenderer.positionCount = nbPos;
        sensorRenderer.SetPositions(positions);
        sensorRenderer.startWidth = 0.01f;
        sensorRenderer.endWidth = 0.01f;
        sensorRenderer.startColor = color;
        sensorRenderer.endColor = color;
    }

    public void SetDestinationMarker()
    {
        if (CheckDestinationMarkerValidity())
            isDestinationSet = true;
    }

    public void RemoveDestinationMarker()
    {
        isDestinationSet = false;
        PublishDestination("{'ValidDestination': 0, " + "'Heading': 0, " + "'Distance': 0}");
    }

    private bool CheckDestinationMarkerValidity()
    {
        if (Mathf.Abs(robotObject.transform.position.y - goalObject.transform.position.y) <= MARKER_VALIDITY_THRESH)
            return true;

        return false;
    }
}
