using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsconNotificationService.Models
{
    internal class Notice
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string TextMessage { get; set; }
        public string RouteId { get; set; }
        public string TaskId { get; set; }
        public Notice(SqlDataReader reader)
        {
            Username = reader["UserName"].ToString()?.Remove(0, 4);
            TextMessage = reader["TextNotice"].ToString();
            RouteId = reader["IdRoute"].ToString();
            TaskId = reader["IdTask"].ToString();
        }
    }
}
