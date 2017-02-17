using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.Samples.Kinect.ColorBasics
{
    class TcpSender
    {
        private bool _connected;
        public bool Connected { get { return _connected; } }

        private TcpClient _client;
        private Stream _stream;

        private string _address;
        private int _port;

        private ASCIIEncoding _encoder;

        public TcpSender()
        {
            _connected = false;
        }

        public void connect(string address, int port)
        {
            _address = address;
            _port = port;

            _encoder = new ASCIIEncoding();

            _client = new TcpClient();
            try
            {
                _client.Connect(address, port);

                _stream = _client.GetStream();

                _connected = true;

                this.write("k/" + Environment.MachineName + "/");
            }
            catch (Exception e)
            {
                _connected = false;
                Console.WriteLine("Unable to connect");
            }
        }

        public void write(string line)
        {
            byte[] ba = _encoder.GetBytes(line);
            this.write(ba);
        }

        public void write(byte[] frame)
        {
            if (_connected)
            {
                try
                {
                    _stream.Write(frame, 0, frame.Length);
                }
                catch
                {
                    close();
                    _connected = false;
                }
            }
        }

        public void close()
        {
            _client.Close();
        }
    }
}
