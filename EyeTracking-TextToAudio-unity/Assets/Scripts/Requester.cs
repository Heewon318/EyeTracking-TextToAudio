using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class Requester : MonoBehaviour
{
    private const string SERVER_IP = "127.0.0.1";
    private const int SERVER_PORT = 65432;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    private void Update()
    {

    }

    public async Task<string> RequestPathAsync(string request)
    {
        try
        {
            using (TcpClient client = new TcpClient(SERVER_IP, SERVER_PORT))
            using (NetworkStream stream = client.GetStream())
            {
                byte[] requestData = Encoding.UTF8.GetBytes(request);
                Debug.Log($"[Requester] Sending request: {request}");
                await stream.WriteAsync(requestData, 0, requestData.Length);
                Debug.Log("[Requester] Request sent successfully.");

                byte[] responseData = new byte[4096];
                Debug.Log("[Requester] Waiting for response from server...");
                int bytesRead = await stream.ReadAsync(responseData, 0, responseData.Length);
                Debug.Log("[Requester] Response received successfully.");

                string response = Encoding.UTF8.GetString(responseData, 0, bytesRead);
                Debug.Log($"[Requester] Server Response Received\nRequest: {request}\nResponse: {response}\nLength: {response.Length} characters\nStatus: Successfully processed");

                return response;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Requester] An error occurred while sending request: {ex.Message}");
            return null;
        }
    }

    public async Task SendGazeDataAsync(string gazeData)
    {
        try
        {
            using (TcpClient client = new TcpClient(SERVER_IP, SERVER_PORT))
            using (NetworkStream stream = client.GetStream())
            {
                string request = "GAZE:" + gazeData;
                byte[] requestData = Encoding.UTF8.GetBytes(gazeData);
                await stream.WriteAsync(requestData, 0, requestData.Length);
                Debug.Log($"[Requester] Sent Gaze Data: {gazeData}");

                byte[] responseData = new byte[256];
                int bytesRead = await stream.ReadAsync(responseData, 0, responseData.Length);
                string response = Encoding.UTF8.GetString(responseData, 0, bytesRead);

                if (response.StartsWith("AUDIO:"))
                {
                    string audioPath = response.Substring(6);
                    Debug.Log($"[Requester] Received Audio Path: {audioPath}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Requester] Error sending gaze data: {ex.Message}");
        }
    }
}
