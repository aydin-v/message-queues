using Azure.Messaging.ServiceBus;

namespace DataCapturing;

public class Program
{
    private const string SourceFolder = @"C:\Users\Aydin\source\repos\MessageQueues\Files";
    private const string ServiceBusConnectionString = "Endpoint=sb://aydinservicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=gnSD9zAicdprmvrvRRo36H1U9tmHVISHI+ASbGmelBY=";
    private const string TopicName = "files";

    private const int chunkSize = 256000;

    private static ServiceBusClient _serviceBusClient;
    private static ServiceBusSender _serviceBusSender;


    static void Main(string[] args)
    {
        MainMessageAsync().GetAwaiter().GetResult();
    }

    static async Task MainMessageAsync()
    {
        _serviceBusClient = new ServiceBusClient(ServiceBusConnectionString);
        _serviceBusSender = _serviceBusClient.CreateSender(TopicName);

        var path = Path.Combine(SourceFolder);
        var fileWatcher = new FileSystemWatcher()
        {
            Path = path,
            EnableRaisingEvents = true
        };

        fileWatcher.Created += FileWatcher_CreatedAsync;

        Console.WriteLine("Press Enter to stop the service");

        var input = Console.ReadKey();
        if (input.Key == ConsoleKey.Enter)
        {
            await _serviceBusSender.CloseAsync();
        }
    }

    private static async void FileWatcher_CreatedAsync(object sender, FileSystemEventArgs e)
    {
        try
        {
            int count = 0;
                using (FileStream fileStream = File.OpenRead(e.FullPath))
                {
                    while (fileStream.Position != fileStream.Length)
                    {
                        var buffer = new byte[chunkSize];

                        var message = new ServiceBusMessage(buffer)
                        {
                            Subject = e.Name
                        };

                        message.ApplicationProperties.Add("position", fileStream.Position);

                        var readBytes = fileStream.Read(buffer, 0, chunkSize);

                        message.ApplicationProperties.Add("size", readBytes);
                        message.ApplicationProperties.Add("fileSize", fileStream.Length);

                        await _serviceBusSender.SendMessageAsync(message);

                        count += readBytes;
                        Console.WriteLine("Sent: {0}", count);
                    }
                }           

            count = 0;
            Console.WriteLine($"File {e.Name} successfully sent");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}