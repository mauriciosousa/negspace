using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Microsoft.Samples.Kinect.ColorBasics
{

    public class UdpSend
    {
        private string _address;
        private int _port;

        private IPEndPoint _remoteEndPoint;
        private UdpClient _udp;
        private int _sendRate;

        private DateTime _lastSent;

        private bool _streaming = false;

        public UdpSend(int port, string address = null, int sendRate = 100)
        {
            _lastSent = DateTime.Now;
            reset(port, address, sendRate);
        }

        public void reset(int port, string address, int sendRate = 100)
        {
            Console.WriteLine("streaming to " + address + " " + port);

            _address = address;
            _sendRate = sendRate;
            try
            {
                _port = port;

                _remoteEndPoint = new IPEndPoint(_address == null ? IPAddress.Broadcast : IPAddress.Parse(_address), _port);
                _udp = new UdpClient();
                _streaming = true;
            }
            catch (Exception e) { }
        }

        public void send(string line)
        {

            this.send(Encoding.UTF8.GetBytes(line));
        }

        public void send(byte[] data)
        {
            if (_streaming)
            {
                try
                {
                    _udp.Send(data, data.Length, _remoteEndPoint);
                    _lastSent = DateTime.Now;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            }
        }
    }
}