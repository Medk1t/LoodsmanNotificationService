using System.Text.Json.Serialization;

namespace AsconSendNotice.Models
{
    public class PostNoticeResponse
    {
        [JsonPropertyOrder(0)]
        public string typeObject { get; set; }
        [JsonPropertyOrder(1)]
        public string postAnswer { get; set; }
        [JsonPropertyOrder(2)]
        public saveArray[] saveArray { get; set; }
    }
}
