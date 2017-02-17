using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Properties : MonoBehaviour {

    private static Properties _singleton;

    public int listenPort = 57743;
	public int sendInterval = 50;

    public int tcpPort = 8001;



    public int FrameDescription_Width = 512;
    public int FrameDescription_Height = 424;


    private Properties()
    {
        _singleton = this;
    }

    public static Properties Instance
    {
        get
        {
            return _singleton;
        }
    }

    void Start()
    {
        //_singleton = this;
    }
}
