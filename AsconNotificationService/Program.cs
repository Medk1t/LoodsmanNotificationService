namespace AsconNotificationService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHostedService<Worker>();

            builder.Services.AddWindowsService(options =>
                options.ServiceName = "Служба уведомлений Лоцман"
            );

            builder.Logging.AddEventLog(c =>
            {
                c.LogName = "Служба уведомлений Лоцман";
                c.SourceName = "Служба уведомлений Лоцман";
            });

            builder.Logging.AddEventSourceLogger();

            var host = builder.Build();
            host.Run();
        }
    }
}