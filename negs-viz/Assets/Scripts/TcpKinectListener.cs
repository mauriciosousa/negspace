using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class KinectStream
{
    public bool Ready;
    private TcpClient _client;
    public DepthViewer depthViewer;
    internal int bytesReceived;
    internal string name;
    internal byte[] data;
    public int BUFFER = 4341760;

    public Color[] texture;
    public ushort[] depthData;
    internal bool dirty;
    internal int size;

    internal bool destroy;

    public KinectStream(TcpClient client)
    {

        int frameSize = Properties.Instance.FrameDescription_Width * Properties.Instance.FrameDescription_Height;

        Ready = false;
        texture = new Color[frameSize];
        depthData = new ushort[frameSize];

        name = "Unknown Kinect Stream";
        _client = client;
        depthViewer = null;
        bytesReceived = 0;

        data = new byte[BUFFER];
        dirty = false;
    }

    public void stopStream()
    {
        _client.Close();
    }

    internal void reload()
    {
        if (Ready)
        {

        }
    }
}

public class TcpKinectListener : MonoBehaviour {

    public int DEBUG;

    public int BUFFER = 4341760;

    public bool showNetworkDetails = true;

    private int TcpListeningPort;
    private TcpListener _server;

    private bool _running;

    private List<KinectStream> _kinectStreams;
    private List<KinectStream> _kinectStreamsToDestroy;

    public DepthViewer depthViewer;

    void Start () {

        //_threads = new List<Thread>();

        _kinectStreams = new List<KinectStream>();
        _kinectStreamsToDestroy = new List<KinectStream>();

        TcpListeningPort = Properties.Instance.tcpPort;
        _server = new TcpListener(IPAddress.Any, TcpListeningPort);

        _running = true;
        _server.Start();

        Thread acceptLoop = new Thread(new ParameterizedThreadStart(AcceptClients));
        //_threads.Add(acceptLoop);
        acceptLoop.Start();
    }

    void AcceptClients(object o)
    {

        while (_running)
        {
            TcpClient newclient = _server.AcceptTcpClient();
            Thread clientThread = new Thread(new ParameterizedThreadStart(clientHandler));
            //_threads.Add(clientThread);
            clientThread.Start(newclient);
        }
    }

    void clientHandler(object o)
    {
        int SIZEHELLO = 200;

        TcpClient client = (TcpClient)o;
        KinectStream kstream = new KinectStream(client);
        _kinectStreams.Add(kstream);

        bool login = false;

        using (NetworkStream ns = client.GetStream())
        {
            byte [] message = new byte[SIZEHELLO];
            int bytesRead = 0;
            byte[] buffer = new byte[BUFFER];

            try
            {
                bytesRead = ns.Read(message, 0, SIZEHELLO);
            }
            catch
            {
                Debug.Log("Connection Lost from " + kstream.name);
                client.Close();
                _kinectStreams.Remove(kstream);
            }

            if (bytesRead == 0)
            {
                Debug.Log("Connection Lost from " + kstream.name);
                client.Close();
                _kinectStreams.Remove(kstream);
            }

            // login
            string s = System.Text.Encoding.Default.GetString(message);
            string[] l = s.Split('/');
            if (l.Length == 3 && l[0] == "k")
            {
                kstream.name = l[1];
                Debug.Log("New stream from " + l[1]);
                login = true;
            }
            else
            {
                Debug.Log("Invalid Login data from: " + kstream.name);
                client.Close();
                _kinectStreams.Remove(kstream);
            }

            while (login && _running)
            {
                try
                {
                    bytesRead = ns.Read(message, 0, 4);
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                    login = false;
                    break;
                }
                if (bytesRead == 0)
                {
                    login = false;
                    break;
                }

                byte[] sizeb = { message[0], message[1], message[2] , message[3]};
                int size = BitConverter.ToInt32(sizeb, 0);
                kstream.size = size;

                kstream.bytesReceived = size;

                string ccc = "X";

                while (size > 0)
                {
                    try
                    {
                        bytesRead = ns.Read(buffer, 0, size);
                        ccc += "Y";
                    }
                    catch (Exception e)
                    {
                        Debug.Log(e.Message);
                        login = false;
                        break;
                    }
                    if (bytesRead == 0)
                    {
                        login = false;
                        break;
                    }

                    Array.Copy(buffer, 0, kstream.data, kstream.size - size, bytesRead);
                    size -= bytesRead;
                }

                kstream.Ready = false;


                Color background = new Color(0, 0, 0, 0);
                Color foreground = Color.white;
                int index;
                ushort depth;
                int ptr = 0;
                byte[] byteValues = new byte[2];

                int currentLine = 0;

                for (int i = 0; i < kstream.texture.Length; i++)
                {
                    byteValues[0] = kstream.data[ptr];
                    byteValues[1] = kstream.data[ptr + 1];
                    index = BitConverter.ToInt16(byteValues, 0);

                    if (index == 5000)
                    {
                        currentLine++;
                        ptr += 2;
                        continue;
                    }
                    else if (i - (currentLine * 512) == index)
                    {
                        foreground.b = ((float)kstream.data[ptr + 2]) / 255;
                        foreground.g = ((float)kstream.data[ptr + 3]) / 255;
                        foreground.r = ((float)kstream.data[ptr + 4]) / 255;
                        kstream.texture[i] = foreground;

                        byteValues[0] = kstream.data[ptr + 5];
                        byteValues[1] = kstream.data[ptr + 6];
                        depth = BitConverter.ToUInt16(byteValues, 0);
                        kstream.depthData[i] = depth;

                        ptr += 7;
                    }
                    else
                    {
                        kstream.texture[i] = background;
                        kstream.depthData[i] = 0;
                    }
                }

                kstream.Ready = true;
            }
            Debug.Log("Connection Lost from " + kstream.name);
            client.Close();
            _kinectStreamsToDestroy.Add(kstream);
            _kinectStreams.Remove(kstream);
        }

    }

    void clientHandler2(object o)
    {
        TcpClient client = (TcpClient)o;

        KinectStream kstream = new KinectStream(client);

        _kinectStreams.Add(kstream);

        using (NetworkStream ns = client.GetStream())
        {
            bool login = false;

            byte[] message = new byte[BUFFER];

            byte[] loginMessage = new byte[BUFFER];
            List<byte> received = new List<byte>();

            int bytesRead;

            byte[] rcvSizeInBytes = new byte[2];
            int rcvSize = 0;

            int rcvBuffer = 0;

            while (_running)
            {
                if (!login)
                {
                    try
                    {
                        bytesRead = ns.Read(loginMessage, 0, loginMessage.Length);
                        kstream.bytesReceived = bytesRead;
                    }
                    catch
                    {
                        break;
                    }

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    string s = System.Text.Encoding.Default.GetString(loginMessage);
                    string[] l = s.Split('/');
                    if (l.Length == 3 && l[0] == "k")
                    {
                        kstream.name = l[1];
                        login = true;
                        Debug.Log("New stream from " + l[1]);
                    }
                }
                else
                {

                    try
                    {
                        bytesRead = ns.Read(rcvSizeInBytes, 0, rcvSizeInBytes.Length);
                    }
                    catch
                    {
                        break;
                    }

                    if (bytesRead > 0)
                    {
                        rcvSize = BitConverter.ToInt16(rcvSizeInBytes, 0);

                        Debug.Log(rcvSize);

                        continue;
                        try
                        {
                            message = new byte[rcvSize];
                            bytesRead = ns.Read(message, 0, message.Length);
                            kstream.bytesReceived = bytesRead;

                            message = received.ToArray();

                            kstream.Ready = false;

                            Color background = new Color(0, 0, 0, 0);
                            Color foreground = Color.white;
                            int index;
                            ushort depth;
                            int ptr = 0;
                            byte[] byteValues = new byte[2];

                            //message
                            int currentLine = 0;

                            continue;

                            for (int i = 0; i < kstream.texture.Length; i++)
                            {
                                byteValues[0] = message[ptr];
                                byteValues[1] = message[ptr + 1];
                                index = BitConverter.ToInt16(byteValues, 0);

                                if (index == 5000)
                                {
                                    currentLine++;
                                    ptr += 2;
                                    continue;
                                }
                                else if (i - (currentLine * 512) == index)
                                {
                                    foreground.b = ((float)message[ptr + 2]) / 255;
                                    foreground.g = ((float)message[ptr + 3]) / 255;
                                    foreground.r = ((float)message[ptr + 4]) / 255;
                                    kstream.texture[i] = foreground;

                                    byteValues[0] = message[ptr + 5];
                                    byteValues[1] = message[ptr + 6];
                                    depth = BitConverter.ToUInt16(byteValues, 0);
                                    kstream.depthData[i] = depth;

                                    ptr += 7;
                                }
                                else
                                {
                                    kstream.texture[i] = background;
                                    kstream.depthData[i] = 0;
                                }
                            }

                            kstream.Ready = true;
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e.Message);
                            Debug.LogError(e.StackTrace);
                            break;
                        }
                    }
                    else break;
                }
            } 
        }
        Debug.Log("Connection Lost from " + kstream.name);
        client.Close();
        _kinectStreams.Remove(kstream);
    }

    private int convert2BytesToInt(byte b1, byte b2)
    {
        return (int)b1 + (int)(b2 * 256);
    }

    void Update ()
    {
        foreach(KinectStream ks in _kinectStreams)
        {
            if (ks.Ready)
            {
                ks.reload();

                if (ks.depthViewer == null)
                {
                    GameObject gameObject = new GameObject();
                    gameObject.name = ks.name;
                    gameObject.transform.position = Vector3.zero;
                    ks.depthViewer = gameObject.AddComponent<DepthViewer>();
                }

                ks.depthViewer.colors = ks.texture;
                ks.depthViewer.depth = ks.depthData;
                ks.Ready = false;
            }
        }

        if (_kinectStreamsToDestroy.Count > 0)
        {
            foreach (KinectStream ks in _kinectStreamsToDestroy)
            {
                Destroy(ks.depthViewer.gameObject);
            }
            _kinectStreamsToDestroy.Clear();
        }

    }

    void OnGUI()
    {
        if (showNetworkDetails)
        {
            int left = 10;
            int top = 10;

            if (_kinectStreams.Count > 0)
            {
                foreach (KinectStream ks in _kinectStreams)
                {
                    string s = "" + ks.name + "   " + ks.bytesReceived;
                    GUI.Label(new Rect(left, top, Screen.width, 30), s);
                    top += 30;
                }
            }
            else
            {
                GUI.Label(new Rect(left, top, 500, 30), "No connected Kinect Streams! ");
            }
        }

    }

    public void closeTcpConnections()
    {
        foreach (KinectStream ks in _kinectStreams)
        {
            ks.stopStream();
        }
        _kinectStreams = new List<KinectStream>();
    }

    void OnApplicationQuit()
    {
        _running = false;
        closeTcpConnections();
    }

    void OnQuit()
    {
        OnApplicationQuit();
    }
}
