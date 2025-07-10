using AsconSendNotice;

namespace AsconNotificationService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Запуск службы");
            
            NoticeManager noticeManager = new(new HttpClient(new HttpClientHandler()), _logger);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await noticeManager.StopAllActiveNoticesAsync(stoppingToken);
                    await noticeManager.DeleteZeroStatusNoticesAsync(stoppingToken);
                    await noticeManager.SendNewNoticesAsync(stoppingToken);
                    await noticeManager.ProcessLongPendingNoticesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Остановка службы");
            await base.StopAsync(stoppingToken);
        }
    }
}
