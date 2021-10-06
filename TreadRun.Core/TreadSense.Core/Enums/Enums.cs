using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreadSense.Enums
{
    #region Exchange

    public enum EnResponseCode
    {
        OK = 200,
        OUTDATED = 426,
        NOTCALIBRATED = 424,
        TIMEOUT = 408,
        BADREQUEST = 400
    }

    public enum EnActions
    {
        Welcome,
        Calibrate,
        Start,
        Stop
    }

    #endregion
}
