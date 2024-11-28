using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.SocialPlatforms;
using TMPro;
using M2MqttUnity;
using System;
using System.Linq;

public class PlayerPrefsManager : MonoBehaviour
{
    public GameObject tmpIpAddress;
    public TouchScreenKeyboard keyboard;
    private string ipAddressInit = "192.168.2.185";
    private M2MqttUnityClient mqttClient;

    /// <summary>
    /// A Unity event function that is called on the frame when a script is enabled just 
    /// before any of the update methods are called the first time.
    /// Get the references of the textfield and the MQTT client.
    /// </summary> 
    void Start()
    {
        // % userprofile %\AppData\Local\Packages\[ProductPackageId]\LocalState\playerprefs.dat
        tmpIpAddress.GetComponent<TMP_Text>().text = LoadIpAddress();
        mqttClient = FindObjectOfType<M2MqttUnityClient>();
    }

    /// <summary>
    /// A Unity event function that is called once per frame when the script is enabled.
    /// Listen to inputs of the virtual keyboard.
    /// </summary> 
    void Update()
    {
        if (keyboard != null)
        {
            tmpIpAddress.GetComponent<TMP_Text>().text = keyboard.text;
        }
    }

    /// <summary>
    /// Open the virtual keyboard.
    /// </summary>
    public void OpenSystemKeyboard()
    {
        keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default, false, false, false, false);
    }

    /// <summary>
    /// Save the current ip address witten in the textfiled.
    /// </summary>
    public void SaveIpAddress()
    {
        string partialIpAddress = tmpIpAddress.GetComponent<TMP_Text>().text;
        string initIpAddress = LoadIpAddress();
        string baseIpAddress = initIpAddress.Split(char.Parse("."))[0] + "." + initIpAddress.Split(char.Parse("."))[1] + "." + initIpAddress.Split(char.Parse("."))[2] + ".";
        if (partialIpAddress.Trim().Length > 3)
            PlayerPrefs.SetString("ipAddress", partialIpAddress.Trim());
        else
            PlayerPrefs.SetString("ipAddress", baseIpAddress + partialIpAddress.Trim());

        tmpIpAddress.GetComponent<TMP_Text>().text = LoadIpAddress();
    }

    /// <summary>
    /// Load the ip address present in the player preferences or create a new one.
    /// </summary>
    public string LoadIpAddress()
    {
        string ipAddress;

        if (PlayerPrefs.HasKey("ipAddress"))
            ipAddress = PlayerPrefs.GetString("ipAddress");
        else
        {
            PlayerPrefs.SetString("ipAddress", ipAddressInit);
            ipAddress = ipAddressInit;
        }

        return ipAddress;
    }

    /// <summary>
    /// Connect MQTT client and update ip address if needed.
    /// </summary>
    public void ConnectMqttClient()
    {
        keyboard = null;

        if (tmpIpAddress.GetComponent<TMP_Text>().text.Trim().Length > 3)
        {
            if (tmpIpAddress.GetComponent<TMP_Text>().text.Trim() != LoadIpAddress())
                SaveIpAddress();
        }
        else 
        {
            if (tmpIpAddress.GetComponent<TMP_Text>().text.Trim() != LoadIpAddress().Split(char.Parse(".")).Last())
                SaveIpAddress();
        }
        
        mqttClient.Connect();
    }

    /// <summary>
    /// Disconnect the MQTT client.
    /// </summary>
    public void DisconnectMqttClient()
    { 
        mqttClient.Disconnect();
    }

    /// <summary>
    /// Shut down the application.
    /// </summary>
    public void SetQuit()
    {
        Application.Quit();
    }
}
