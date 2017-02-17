using System;
using System.IO;
using Microsoft.Kinect;
using System.Collections.Generic;

namespace Microsoft.Samples.Kinect.ColorBasics
{
    internal class ConfigFile
    {
        // Netwoek
        public int TcpPort { get; private set; }
        public string TcpAddress { get; private set; }

        public ConfigFile()
        {
            TcpPort = 0;
            TcpAddress = "localhost";
        }

        public bool Load(string filename)
        {
            if (File.Exists(filename))
            {
                foreach (string line in File.ReadAllLines(filename))
                {
                    if (line.Length != 0 && line[0] != '%')
                    {
                        string[] s = line.Split('=');
                        if (s.Length == 2)
                        {
                            if (s[0] == "tcp.port") this.TcpPort = int.Parse(s[1]);
                            else if (s[0] == "tcp.address") this.TcpAddress = s[1];
                        }
                        else return false;
                    }
                }

                return true;
            }
            else return false;
        }

        public void Save(string filename)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filename, false))
            {
                file.WriteLine("% " + DateTime.Now.ToShortDateString());
                file.WriteLine("tcp.port=" + TcpPort);
                file.WriteLine("tcp.address=" + TcpAddress);

                file.WriteLine("%");
            }
        }

        private string _pointToString(CameraSpacePoint p)
        {
            return "" + p.X + ":" + p.Y + ":" + p.Z;
        }

        private bool _parseSurface(string str, out CameraSpacePoint csPoint)
        {
            
            csPoint = new CameraSpacePoint();
            string[] line = str.Split(':');
            if (line.Length == 3)
            {
                try
                {
                    csPoint.X = float.Parse(line[0]);
                    csPoint.X = float.Parse(line[1]);
                    csPoint.X = float.Parse(line[2]);
                }
                catch
                {
                    return false;
                }
            }
            else return false;

            return true;
        }
    }
}