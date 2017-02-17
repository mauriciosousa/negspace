using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Text;

public class UdpListener : MonoBehaviour {


    private UdpClient _udpClient = null;
    private IPEndPoint _anyIP;
    private List<string> _stringsToParse;

    void Start()
    {
        _stringsToParse = new List<string>();
    }

    public void udpRestart()
    {
        if (_udpClient != null)
        {
            _udpClient.Close();
        }


        _stringsToParse = new List<string>();
        
		_anyIP = new IPEndPoint(IPAddress.Any, Properties.Instance.listenPort);
        
        _udpClient = new UdpClient(_anyIP);

        _udpClient.BeginReceive(new AsyncCallback(this.ReceiveCallback), null);

        Debug.Log("[UDP] listening to " + Properties.Instance.listenPort);
    }

    public void ReceiveCallback(IAsyncResult ar)
    {

        Byte[] receiveBytes = _udpClient.EndReceive(ar, ref _anyIP);
        Debug.Log("receivedBytes: " + receiveBytes.Length);


        //_stringsToParse.Add(Encoding.ASCII.GetString(receiveBytes));

        

		_udpClient.BeginReceive(new AsyncCallback(this.ReceiveCallback), null);
    }

    void Update()
    {
        while (_stringsToParse.Count > 0)
        {
            string stringToParse = _stringsToParse.First();
            _stringsToParse.RemoveAt(0);
			if(stringToParse != null){
                
			}
        }
    }

    void OnApplicationQuit()
    {
        if (_udpClient != null) _udpClient.Close();
    }

    void OnQuit()
    {
        OnApplicationQuit();
    }
}
