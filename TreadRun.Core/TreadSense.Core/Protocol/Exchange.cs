using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

public abstract class Exchange
{

    #region JSONClasses

    public class ActionJSON
    {
        [JsonProperty("exchangeKey")]
        public string ExchangeKey { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }
    }

    public class WelcomeJSON
    {
        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("exchangeKey")]
        public string ExchangeKey { get; set; }

        [JsonProperty("isCalibrated")]
        public bool IsCalibrated { get; set; }

        [JsonProperty("isValidDevice")]
        public bool IsValidDevice { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("deviceType")]
        public string DeviceType { get; set; }

        public override string ToString()
        {
            return ExchangeKey + " " +
                IsCalibrated + " " +
                IsValidDevice + " " +
                Version + " " +
                DeviceType;
        }
    }

    public class CalibrateAndStopJSON
    {
        [JsonProperty("exchangeKey")]
        public string ExchangeKey { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }
    }

    public class StartJSON
    {
        [JsonProperty("exchangeKey")]
        public string ExchangeKey { get; set; }

        [JsonProperty("velocity")]
        public float Velocity { get; set; }

        [JsonProperty("incline")]
        public float Incline { get; set; }

        [JsonProperty("spm")]
        public float Spm { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }
    }

    #endregion

}