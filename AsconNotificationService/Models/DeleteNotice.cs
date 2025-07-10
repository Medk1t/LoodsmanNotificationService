using System.Text.Json.Serialization;

namespace AsconSendNotice.Models
{
    public class DeleteNotice
    {
        [JsonPropertyOrder(0)]
        public string typeObject { get; set; } = "CLOSE_ONE_NOTICE_LOODSMAN";
        [JsonPropertyOrder(1)]
        public string id { get; set; }
        public DeleteNotice(string noticeId)
        {
            id = noticeId;
        }
    }
}
