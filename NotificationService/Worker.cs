using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace NotificationService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private ServiceBusClient _client;
        private ServiceBusProcessor _processor;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            // Connect to Queue
            var connectionString = _configuration.GetConnectionString("ServiceBusConnection");
            var queueName = _configuration["ServiceBusQueue"];

            _client = new ServiceBusClient(connectionString);
            _processor = _client.CreateProcessor(queueName, new ServiceBusProcessorOptions());

            // Define what happens when a message arrives
            _processor.ProcessMessageAsync += MessageHandler;
            _processor.ProcessErrorAsync += ErrorHandler;

            await _processor.StartProcessingAsync();
            await base.StartAsync(cancellationToken);
        }

        private async Task MessageHandler(ProcessMessageEventArgs args)
        {
            // 1. Read the message body from the Queue
            string body = args.Message.Body.ToString();

            // 2. Decide: Is this a "Booking" or an "Admin Alert"?
            if (body.Contains("LowCapacityAlert"))
            {
                // --- CASE A: Low Stock Alert (Triggered by Logic App -> API) ---
                _logger.LogWarning($"[ADMIN ALERT RECEIVED]: {body}");

                // Simulate sending an email to the Manager
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[URGENT]: Sending email to admin@hotel.com -> Stock Low!");
                Console.ResetColor();
            }
            else
            {
                // --- CASE B: New Reservation (Triggered by User Booking) ---
                _logger.LogInformation($"[NEW BOOKING RECEIVED]: {body}");

                // Simulate sending an email to the Guest
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[SUCCESS]: Sending confirmation email to guest.");
                Console.ResetColor();
            }

            // 3. Delete message from Queue so it isn't processed again
            await args.CompleteMessageAsync(args.Message);
        }

        private Task ErrorHandler(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception.ToString());
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _processor.StopProcessingAsync();
            await _processor.DisposeAsync();
            await _client.DisposeAsync();
            await base.StopAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
    }
}