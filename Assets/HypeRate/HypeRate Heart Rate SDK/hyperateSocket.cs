using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using NativeWebSocket;

public class hyperateSocket : MonoBehaviour
{

	// Put your websocket Token ID here
    public string websocketToken = "qGaQGofbHDuXUb77XuxyVEV0eexjD1KmUmJtP6U3LV80aU1F78b4rwvnQb4wO5y8"; 
    public string hyperateID = "ZFNBRC0";
    // ⭐ 新增: 用于存储当前心率的公共静态变量
    public static int CurrentHeartRate = 0;
    // Textbox to display your heart rate in
    Text textBox;
	// Websocket for connection with Hyperate
    WebSocket websocket;
    async void Start()
    {
        textBox = GetComponent<Text>();

        websocket = new WebSocket("wss://app.hyperate.io/socket/websocket?token=" + websocketToken);
        Debug.Log("Connect!");

        websocket.OnOpen += () =>
        {
            Debug.Log("Connection open!");
            SendWebSocketMessage();
        };

        websocket.OnError += (e) =>
        {
            Debug.Log("Error! " + e);
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log("Connection closed!");
        };

        websocket.OnMessage += (bytes) =>
        {
        // getting the message as a string
            var message = System.Text.Encoding.UTF8.GetString(bytes);
            var msg = JObject.Parse(message);

            if (msg["event"].ToString() == "hr_update")
            {
                // Change textbox text into the newly received Heart Rate (integer like "86" which represents beats per minute)
                //textBox.text = (string) msg["payload"]["hr"]; //改变
                string hrString = (string)msg["payload"]["hr"];
                textBox.text = hrString;
                // ⭐ 新增: 更新静态变量
                if (int.TryParse(hrString, out int hrValue))
                {
                    CurrentHeartRate = hrValue;
                }
            }
        };

        // Send heartbeat message every 25seconds in order to not suspended the connection
        InvokeRepeating("SendHeartbeat", 1.0f, 25.0f);

        // waiting for messages
        await websocket.Connect();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket.DispatchMessageQueue();
#endif
    }

    async void SendWebSocketMessage()
    {
        if (websocket.State == WebSocketState.Open)
        {
            // Log into the "internal-testing" channel
            await websocket.SendText("{\"topic\": \"hr:"+hyperateID+"\", \"event\": \"phx_join\", \"payload\": {}, \"ref\": 0}");
        }
    }
    async void SendHeartbeat()
    {
        if (websocket.State == WebSocketState.Open)
        {
            // Send heartbeat message in order to not be suspended from the connection
            await websocket.SendText("{\"topic\": \"phoenix\",\"event\": \"heartbeat\",\"payload\": {},\"ref\": 0}");

        }
    }

    private async void OnApplicationQuit()
    {
        await websocket.Close();
    }


}

public class HyperateResponse
{
    public string Event { get; set; }
    public string Payload { get; set; }
    public string Ref { get; set; }
    public string Topic { get; set; }
}
