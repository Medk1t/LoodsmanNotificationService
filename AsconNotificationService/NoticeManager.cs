using AsconNotificationService;
using AsconSendNotice.Models;
using Microsoft.Data.SqlClient;
using System.Net.Http.Json;
using System.Text.Json;

namespace AsconSendNotice
{
    public class NoticeManager
    {
        private readonly HttpClient _httpClient;
        private readonly string _sqlConnectionString;
        private readonly ILogger<Worker> _logger;
        public NoticeManager(HttpClient httpClient, ILogger<Worker> logger)
        {
            _httpClient = httpClient;
            var constr = new SqlConnectionStringBuilder()
            {
                UserID = "SYSDBA",
                Password = "8prusobar8",
                PersistSecurityInfo = true,
                InitialCatalog = "Гражданская_продукция",
                DataSource = "Ascon1",
                TrustServerCertificate = true,

            };
            _sqlConnectionString = constr.ConnectionString;
            _logger = logger;
        }
        /// <summary>
        /// Создает уведомления для задач со статусом 1
        /// </summary>
        public async Task SendNewNoticesAsync(CancellationToken cancellation)
        {
            using (SqlConnection connection = new(_sqlConnectionString))
            {
                await connection.OpenAsync(cancellation);
                SqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT * FROM [Гражданская_продукция].[dbo].[Notice] where TaskStatus = 1";
                SqlDataReader reader = await cmd.ExecuteReaderAsync(cancellation);
                string? userName = reader["UserName"].ToString()?.Remove(0, 4);
                string? textMessage = reader["TextNotice"].ToString();
                string? idRoute = reader["IdRoute"].ToString();
                string? idTask = reader["IdTask"].ToString();
                while (await reader.ReadAsync(cancellation))
                {
                    var response = await SendNotice(userName, textMessage, cancellation);
                    var idNotice = $"'{response.saveArray[0].id}'";
                    await UpdateTaskStatus(idRoute, idTask, userName, "2", cancellation, idNotice);
                }
                await connection.CloseAsync();
            }
        }
        /// <summary>
        /// Помечает уведомления задач с большим сроком (для статуса 2 -- 4 часа. для статуса 4 -- 24 часа) красным
        /// </summary>
        public async Task ProcessLongPendingNoticesAsync(CancellationToken cancellation)
        {
            using (SqlConnection connection = new(_sqlConnectionString))
            {
                await connection.OpenAsync(cancellation);
                SqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT * FROM [Гражданская_продукция].[dbo].[Notice] where (TaskStatus = 2) AND (GETDATE()-[Notice].date > '03:59:59')";
                SqlDataReader reader = await cmd.ExecuteReaderAsync(cancellation);
                string? userName = reader["UserName"].ToString()?.Remove(0, 4);
                string? textMessage = $"<b>{reader["TextNotice"].ToString()}</b>";
                string? idRoute = reader["IdRoute"].ToString();
                string? idTask = reader["IdTask"].ToString();
                string? idNotice = reader["IdNotice"].ToString();

                while (await reader.ReadAsync(cancellation))
                {
                    await DeleteNotice(idNotice, cancellation);
                    await SendNotice(userName, textMessage, cancellation);
                    await UpdateTaskStatus(idRoute, idTask, userName, "3", cancellation, idNotice);
                }
                await connection.CloseAsync();
            }
            using (SqlConnection connection = new(_sqlConnectionString))
            {
                await connection.OpenAsync(cancellation);
                SqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT * FROM [Гражданская_продукция].[dbo].[Notice] where (TaskStatus = 4) AND (GETDATE()-[Notice].date > '23:59:59')";
                SqlDataReader reader = await cmd.ExecuteReaderAsync(cancellation);
                string? userName = reader["UserName"].ToString()?.Remove(0, 4);
                string? textMessage = $"<b>Задание *{reader["TextNotice"].ToString()}* находится у Вас в работе более одного дня. Необходимо завершить задание.</b>";
                string? idRoute = reader["IdRoute"].ToString();
                string? idTask = reader["IdTask"].ToString();
                string? idNotice = reader["IdNotice"].ToString();
                while (await reader.ReadAsync(cancellation))
                {
                    await DeleteNotice(idNotice, cancellation);
                    await SendNotice(userName, textMessage, cancellation);
                    await UpdateTaskStatus(idRoute, idTask, userName, "5", cancellation, idNotice);
                }
                await connection.CloseAsync();
            }
        }
        /// <summary>
        /// Удаляет уведомления со статусами 0 и 3 и обновляет статус 3 на 4
        /// </summary>
        public async Task StopAllActiveNoticesAsync(CancellationToken cancellation)
        {
            using (SqlConnection connection = new(_sqlConnectionString))
            {
                await connection.OpenAsync(cancellation);
                SqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT * FROM [Гражданская_продукция].[dbo].[Notice] where (TaskStatus = 0) AND (TaskStatus = 3)";
                SqlDataReader reader = await cmd.ExecuteReaderAsync(cancellation);
                string? userName = reader["UserName"].ToString()?.Remove(0, 4);
                string? textMessage = reader["TextNotice"].ToString();
                string? idRoute = reader["IdRoute"].ToString();
                string? idTask = reader["IdTask"].ToString();
                string? idNotice = reader["IdNotice"].ToString();
                while (await reader.ReadAsync(cancellation))
                {
                    await DeleteNotice(idNotice, cancellation);
                    if (reader["TaskStatus"].ToString() == "3")
                    {
                        await UpdateTaskStatus(idRoute, idTask, userName, "4", cancellation);
                        await UpdateTaskDate(idRoute, idTask, userName, cancellation);
                    }
                }
                await connection.CloseAsync();
            }
        }
        public async Task UpdateTaskStatus(string? idRoute, string? idTask, string? userName, string? newStatus, CancellationToken cancellation, string idNotice = "NULL")
        {
            if (!string.IsNullOrEmpty(idRoute) && !string.IsNullOrEmpty(idTask) && !string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(newStatus))
            {
                using (SqlConnection connection = new(_sqlConnectionString))
                {
                    await connection.OpenAsync(cancellation);
                    SqlCommand cmd = connection.CreateCommand();
                    cmd.CommandText = $"UPDATE [dbo].[Notice] SET [TaskStatus] = {newStatus}, [IdNotice] = {idNotice} WHERE IdRoute = '{idRoute}' AND IdTask = '{idTask}' AND UserName = '{userName}'";
                    await cmd.ExecuteNonQueryAsync(cancellation);
                    await connection.CloseAsync();
                }
            }
        }
        public async Task UpdateTaskDate(string? idRoute, string? idTask, string? userName, CancellationToken cancellation)
        {
            if (!string.IsNullOrEmpty(idRoute) && !string.IsNullOrEmpty(idTask) && !string.IsNullOrEmpty(userName))
            {
                using (SqlConnection connection = new(_sqlConnectionString))
                {
                    await connection.OpenAsync(cancellation);
                    SqlCommand cmd = connection.CreateCommand();
                    cmd.CommandText = $"UPDATE [dbo].[Notice] SET [Date] = GETDATE() WHERE IdRoute = '{idRoute}' AND IdTask = '{idTask}' AND UserName = '{userName}'";
                    await cmd.ExecuteNonQueryAsync(cancellation);
                    await connection.CloseAsync();
                }
            }
        }
        /// <summary>
        /// Удаляет из базы задачи со статусом 0
        /// </summary>
        public async Task DeleteZeroStatusNoticesAsync(CancellationToken cancellation)
        {
            using (SqlConnection connection = new(_sqlConnectionString))
            {
                await connection.OpenAsync(cancellation);
                SqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM [Гражданская_продукция].[dbo].[Notice] where TaskStatus = 0";
                await cmd.ExecuteNonQueryAsync(cancellation);
                await connection.CloseAsync();
            }
        }
        public async Task<DeleteNoticeResponse?> DeleteNotice(string? id, CancellationToken cancellation)
        {
            if (!string.IsNullOrEmpty(id))
            {
                DeleteNotice deleteNotice = new(id);
                var responseMessage = await _httpClient.PostAsJsonAsync("http://ls3.lan.zlatmash.ru:8080/deleteLoodsmanNotice_01", deleteNotice, cancellation);
                _logger.LogTrace($"Delete Notice Reguest: {await responseMessage.RequestMessage.Content.ReadAsStringAsync(cancellation)}");
                _logger.LogTrace($"Delete Notice Response: {await responseMessage.Content.ReadAsStringAsync(cancellation)}");
                var noticeResponse = await JsonSerializer.DeserializeAsync<DeleteNoticeResponse>(await responseMessage.Content.ReadAsStreamAsync(cancellation), cancellationToken: cancellation);
                _logger.LogInformation($"Delete Notice: {noticeResponse?.message} {noticeResponse.sendTo[0]} {noticeResponse.sendTo[1]}");
                return noticeResponse;
            }
            return null;
        }
        public async Task<PostNoticeResponse?> SendNotice(string? userName, string? messageText, CancellationToken cancellation)
        {
            if (!string.IsNullOrEmpty(userName))
            {
                addNotice notice = new addNotice(userName, messageText ?? string.Empty);
                var responseMessage = await _httpClient.PostAsJsonAsync("http://ls3.lan.zlatmash.ru:8080/addNotice_01", notice, cancellation);
                _logger.LogTrace($"Send Notice Request: {responseMessage.RequestMessage.Content.ReadAsStringAsync(cancellation)}");
                _logger.LogTrace($"Send Notice Response: {responseMessage.Content.ReadAsStringAsync(cancellation)}");
                var postNoticeResponse = await JsonSerializer.DeserializeAsync<PostNoticeResponse>(await responseMessage.Content.ReadAsStreamAsync(cancellation), cancellationToken: cancellation);
                _logger.LogInformation($"Send notice: Уведомления сохранены и отправлены пользователю [{postNoticeResponse?.saveArray[0].login}]");
                return postNoticeResponse;
            }
            return null;
        }

    }
}
