using Azure.Messaging.ServiceBus;

class Program
{
    private const string SourceFolder = @"C:\Users\Aydin\source\repos\MessageQueues\Downloads";

    private const string ServiceBusConnectionString = "Endpoint=sb://aydinservicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=gnSD9zAicdprmvrvRRo36H1U9tmHVISHI+ASbGmelBY=";
    private const string TopicName = "files";
    private const string SubscriptionName = "filesubscription";
    static string currentFile = string.Empty;
    static string currentFilePath = string.Empty;
    static int bytesCount = 0;
    private static ServiceBusClient _serviceBusClient;
    private static ServiceBusProcessor _serviceBusProcessor;
    static void Main(string[] args)
    {
        ReceiveMessageAsync().GetAwaiter().GetResult();
    }

    static async Task ReceiveMessageAsync()
    {
        _serviceBusClient = new ServiceBusClient(ServiceBusConnectionString);
        _serviceBusProcessor = _serviceBusClient.CreateProcessor(TopicName, SubscriptionName);
        _serviceBusProcessor.ProcessMessageAsync += ProcessMessageAsync;
        _serviceBusProcessor.ProcessErrorAsync += ExceptionReceivedHandler;

        await _serviceBusProcessor.StartProcessingAsync();

        Console.ReadKey();

        await _serviceBusProcessor.StopProcessingAsync();
    }


    static async Task ProcessMessageAsync(ProcessMessageEventArgs eventArgs)
    {
        string fileName = eventArgs.Message.Subject;

        if (currentFile != fileName)
        {
            Console.WriteLine("Retrieving new file {0}", fileName);
            currentFilePath = Path.Combine(SourceFolder, fileName);
            currentFile = fileName;
        }

        var position = Convert.ToInt64(eventArgs.Message.ApplicationProperties["position"]);
        var size = Convert.ToInt32(eventArgs.Message.ApplicationProperties["size"]);
        var fileSize = Convert.ToInt64(eventArgs.Message.ApplicationProperties["fileSize"]);

        using (var fileStream = new FileStream(currentFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
        {
            fileStream.Position = position;
            await fileStream.WriteAsync(eventArgs.Message.Body.ToArray(), 0, size);
            await fileStream.FlushAsync();
        }

        bytesCount += size;
        Console.WriteLine("Current size: {0}", bytesCount);

        if (bytesCount == fileSize)
        {
            Console.WriteLine($"Received file {fileName} and saved it");
            bytesCount = 0;
        }
    }

    static Task ExceptionReceivedHandler(ProcessErrorEventArgs eventArgs)
    {
        Console.WriteLine($"Message handler encountered an exception {eventArgs.Exception}");
        return Task.CompletedTask;
    }
}