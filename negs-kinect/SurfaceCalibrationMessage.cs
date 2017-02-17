using System;
using Microsoft.Kinect;
using System.Text;

namespace Microsoft.Samples.Kinect.ColorBasics
{
    internal class SurfaceCalibrationMessage
    {
        private CameraSpacePoint bL;
        private CameraSpacePoint bR;
        private string machineName;
        private CameraSpacePoint tL;
        private CameraSpacePoint tR;

        public string Message { get { return _composeMessage(); } }

        public SurfaceCalibrationMessage(string machineName, CameraSpacePoint bL, CameraSpacePoint bR, CameraSpacePoint tL, CameraSpacePoint tR)
        {
            this.machineName = machineName;
            this.bL = bL;
            this.bR = bR;
            this.tL = tL;
            this.tR = tR;
        }

        private string _composeMessage()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("srf").Append("/");
            sb.Append(machineName).Append("/");
            sb.Append("bl=").Append(_cameraSpacePointToString(bL)).Append("/");
            sb.Append("br=").Append(_cameraSpacePointToString(bR)).Append("/");
            sb.Append("tl=").Append(_cameraSpacePointToString(tL)).Append("/");
            sb.Append("tr=").Append(_cameraSpacePointToString(tR));
            return sb.ToString();
        }

        private string _cameraSpacePointToString(CameraSpacePoint p)
        {
            return new StringBuilder().Append(p.X).Append(":").Append(p.Y).Append(":").Append(p.Z).ToString();
        }
    }
}