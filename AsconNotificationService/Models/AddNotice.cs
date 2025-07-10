namespace AsconSendNotice.Models
{
    public class addNotice
    {
        public string typeObject { get; set; } = "TASK_NOTICE";
        public string[] usersList { get; set; }
        public string fromSystem { get; set; } = "ASCON_SYSTEM";
        public string nBody { get; set; } = string.Empty;
        public string url { get; set; } = string.Empty;
        public addNotice(string user, string message)
        {
            usersList = [user];
            nBody = message;
        }
    }
}
