using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class NoticeProcessor
{
    private readonly string _connectionString;
    private readonly HttpClient _httpClient;

    public NoticeProcessor(string connectionString)
    {
        _connectionString = connectionString;
        _httpClient = new HttpClient();
    }

    public async Task ProcessNoticesAsync()
    {
        try
        {
            await SendNewNoticesAsync();
            await ProcessLongPendingNoticesAsync();
            await StopActiveNoticesAsync();
            await DeleteZeroStatusNoticesAsync();
            await CheckDateAndNotifyAsync();
        }
        catch (Exception ex)
        {
            // TODO: Логировать ошибку
            Console.WriteLine($"Ошибка в ProcessNoticesAsync: {ex.Message}");
        }
    }

    private async Task SendNewNoticesAsync()
    {
        const string selectSql = @"SELECT * FROM [Гражданская_продукция].[dbo].[Notice] WHERE TaskStatus = @status";

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var selectCmd = new SqlCommand(selectSql, connection);
        selectCmd.Parameters.AddWithValue("@status", "1");

        using var reader = await selectCmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            try
            {
                var userName = reader["UserName"].ToString();
                if (userName.Length > 4)
                    userName = userName.Substring(4);
                var usersList = new[] { userName };

                var textNotice = reader["TextNotice"].ToString();

                var jsonObj = new
                {
                    typeObject = "TASK_NOTICE",
                    usersList = usersList,
                    fromSystem = "ASCON_SYSTEM",
                    nBody = textNotice,
                    url = ""
                };

                var json = JsonSerializer.Serialize(jsonObj);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("http://ls3.lan.zlatmash.ru:8080/addNotice_01", content);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();

                // Парсим JSON-ответ, ожидаем что есть поле "id"
                using var doc = JsonDocument.Parse(responseString);
                if (!doc.RootElement.TryGetProperty("id", out var idElement))
                {
                    // Логируем ошибку, пропускаем
                    Console.WriteLine("Ответ не содержит поле id");
                    continue;
                }
                var idNotice = idElement.GetString();

                // Обновляем запись в базе
                await UpdateNoticeAsync(connection,
                    reader["IdRoute"].ToString(),
                    reader["IdTask"].ToString(),
                    reader["UserName"].ToString(),
                    "2",
                    idNotice);
            }
            catch (Exception ex)
            {
                // Логируем ошибку для конкретной записи и продолжаем
                Console.WriteLine($"Ошибка обработки записи: {ex.Message}");
            }
        }
    }

    private async Task UpdateNoticeAsync(SqlConnection connection, string idRoute, string idTask, string userName, string taskStatus, string idNotice)
    {
        const string updateSql = @"
            UPDATE [dbo].[Notice]
            SET TaskStatus = @taskStatus, IdNotice = @idNotice
            WHERE IdRoute = @idRoute AND IdTask = @idTask AND UserName = @userName";

        using var updateCmd = new SqlCommand(updateSql, connection);
        updateCmd.Parameters.AddWithValue("@taskStatus", taskStatus);
        updateCmd.Parameters.AddWithValue("@idNotice", idNotice);
        updateCmd.Parameters.AddWithValue("@idRoute", idRoute);
        updateCmd.Parameters.AddWithValue("@idTask", idTask);
        updateCmd.Parameters.AddWithValue("@userName", userName);

        await updateCmd.ExecuteNonQueryAsync();
    }

    // Аналогично реализуйте остальные части (ProcessLongPendingNoticesAsync, StopActiveNoticesAsync, DeleteZeroStatusNoticesAsync, CheckDateAndNotifyAsync)
    // Они будут похожи по структуре: выборка, формирование JSON, отправка HTTP, обновление базы.

    // Например, пример для удаления записей со статусом 0:
    private async Task DeleteZeroStatusNoticesAsync()
    {
        const string deleteSql = @"DELETE FROM [Гражданская_продукция].[dbo].[Notice] WHERE TaskStatus = @status";

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var deleteCmd = new SqlCommand(deleteSql, connection);
        deleteCmd.Parameters.AddWithValue("@status", "0");

        await deleteCmd.ExecuteNonQueryAsync();
    }
}
