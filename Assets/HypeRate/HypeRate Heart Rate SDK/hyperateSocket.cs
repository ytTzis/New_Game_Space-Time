using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using NativeWebSocket;

[DisallowMultipleComponent]
public class hyperateSocket : MonoBehaviour
{

	// Put your websocket Token ID here
    public string websocketToken = "WqUFS31Br1CochGoJQLtAahFBkMmvVfAXKUPJXlF"; 
    public string hyperateID = "1H8Q6F6";
    [Header("Lifecycle")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool autoConnectOnStart = true;
    [Header("Runtime Debug")]
    [SerializeField] private string connectionStatus = "Not started";
    [SerializeField] private bool joinedHeartRateChannel;
    [SerializeField] private bool receivedHeartRate;
    [SerializeField] private int heartbeatSentCount;
    [SerializeField] private string lastSocketEvent = "None";
    [SerializeField] private string lastError = string.Empty;
    [SerializeField] private string lastRawMessage = string.Empty;
    [SerializeField] private string lastHeartRateText = "0";
    [Header("UI Debug (Optional)")]
    [SerializeField] private Text textBox;

    public static int CurrentHeartRate = 0;
    WebSocket websocket;
    private static hyperateSocket instance;

    public string ConnectionStatus => connectionStatus;
    public bool JoinedHeartRateChannel => joinedHeartRateChannel;
    public bool ReceivedHeartRate => receivedHeartRate;
    public string LastSocketEvent => lastSocketEvent;
    public string LastError => lastError;
    public string LastRawMessage => lastRawMessage;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        if (dontDestroyOnLoad)
        {
            if (transform.parent != null)
            {
                transform.SetParent(null, true);
            }
            DontDestroyOnLoad(gameObject);
        }
    }

    async void Start()
    {
        if (!autoConnectOnStart)
        {
            return;
        }

        await ConnectSocket();
    }

    public async void Connect()
    {
        await ConnectSocket();
    }

    private async System.Threading.Tasks.Task ConnectSocket()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            return;
        }

        connectionStatus = "Connecting";
        lastSocketEvent = "Start";
        lastError = string.Empty;
        lastRawMessage = string.Empty;
        joinedHeartRateChannel = false;
        receivedHeartRate = false;
        heartbeatSentCount = 0;
        lastHeartRateText = CurrentHeartRate.ToString();

        websocket = new WebSocket("wss://app.hyperate.io/ws/" + hyperateID + "?token=" + websocketToken);
        Debug.Log("Connect!");

        websocket.OnOpen += () =>
        {
            connectionStatus = "Open";
            lastSocketEvent = "OnOpen";
            Debug.Log("Connection open!");
            SendWebSocketMessage();
        };

        websocket.OnError += (e) =>
        {
            connectionStatus = "Error";
            lastSocketEvent = "OnError";
            lastError = e;
            Debug.Log("Error! " + e);
        };

        websocket.OnClose += (e) =>
        {
            connectionStatus = "Closed";
            lastSocketEvent = "OnClose";
            Debug.Log("Connection closed!");
        };

        websocket.OnMessage += (bytes) =>
        {
            var message = System.Text.Encoding.UTF8.GetString(bytes);
            lastRawMessage = message;
            Debug.Log("[Hyperate] Message: " + message);

            JObject msg;
            try
            {
                msg = JObject.Parse(message);
            }
            catch (Exception ex)
            {
                lastSocketEvent = "ParseError";
                lastError = ex.Message;
                Debug.LogError("[Hyperate] Failed to parse message: " + ex.Message);
                return;
            }

            string eventName = msg["event"]?.ToString() ?? "unknown";
            string topicName = msg["topic"]?.ToString() ?? "unknown";
            lastSocketEvent = eventName;

            if (eventName == "phx_reply" &&
                topicName == "hr:" + hyperateID &&
                msg["payload"]?["status"]?.ToString() == "ok")
            {
                joinedHeartRateChannel = true;
                connectionStatus = "Joined channel";
                Debug.Log("[Hyperate] Joined heart rate channel successfully.");
                return;
            }

            if (eventName == "hr_update")
            {
                string hrString = (string)msg["payload"]?["hr"];
                lastHeartRateText = hrString;
                receivedHeartRate = true;
                connectionStatus = "Receiving heart rate";

                if (textBox != null)
                {
                    textBox.text = hrString;
                }

                if (int.TryParse(hrString, out int hrValue))
                {
                    CurrentHeartRate = hrValue;
                }

                Debug.Log("[Hyperate] Heart rate updated: " + hrString);
            }
        };

        InvokeRepeating("SendHeartbeat", 1.0f, 15.0f);

        await websocket.Connect();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (websocket != null)
        {
            websocket.DispatchMessageQueue();
        }
#endif
    }

    async void SendWebSocketMessage()
    {
        if (websocket.State == WebSocketState.Open)
        {
            lastSocketEvent = "SendJoin";
            await websocket.SendText("{\"topic\": \"hr:"+hyperateID+"\", \"event\": \"phx_join\", \"payload\": {}, \"ref\": 0}");
            Debug.Log("[Hyperate] Join request sent for hr:" + hyperateID);
        }
    }

    async void SendHeartbeat()
    {
        if (websocket.State == WebSocketState.Open)
        {
            heartbeatSentCount++;
            lastSocketEvent = "SendHeartbeat";
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await websocket.SendText("{\"event\": \"ping\", \"payload\": {\"timestamp\": " + timestamp + "}}");
            Debug.Log("[Hyperate] Heartbeat sent. Count: " + heartbeatSentCount);
        }
    }

    private async void OnApplicationQuit()
    {
        if (websocket != null)
        {
            await websocket.Close();
        }
    }

    private async void OnDestroy()
    {
        if (websocket != null)
        {
            await websocket.Close();
        }
    }

}

public class HyperateResponse
{
    public string Event { get; set; }
    public string Payload { get; set; }
    public string Ref { get; set; }
    public string Topic { get; set; }
}
