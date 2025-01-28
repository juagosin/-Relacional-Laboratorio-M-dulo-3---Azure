using System;
using System.Text.Json;
using System.Threading;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;


Console.WriteLine("Hello to the QueueProcessor!");

var queueClient = new QueueClient(Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING"), "pics-to-delete");

queueClient.CreateIfNotExists();

while (true)
{
    QueueMessage message = queueClient.ReceiveMessage();

    if (message != null)
    {
        Console.WriteLine($"Message received {message.Body}");

        var task = JsonSerializer.Deserialize<Task>(message.Body);

        Console.WriteLine($"Let's delete images of {task.heroName} and {task.alterEgoName}");

        if (task.heroName != null)
        {
            //Create a Blob service client
            var blobClient = new BlobServiceClient(Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING"));

            //Get container client
            BlobContainerClient heroContainer = blobClient.GetBlobContainerClient("heroes");
            BlobContainerClient alterEgoContainer = blobClient.GetBlobContainerClient("alteregos");

            //Get blob with old name
            var heroFileName = $"{task.heroName.Replace(' ', '-').ToLower()}.jpeg";
            var alterEgoFileName = $"{task.alterEgoName.Replace(' ', '-').ToLower()}.png";
            Console.WriteLine($"Looking for {heroFileName}");
            var heroBlob = heroContainer.GetBlobClient(heroFileName);

            if (heroBlob.Exists())
            {
                Console.WriteLine("Found it!");
                Console.WriteLine($"Deleting {heroFileName}");


                //Delete the old blob
                heroBlob.DeleteIfExists();

            }
            else
            {
                Console.WriteLine($"There is no hero image to delete.");
                Console.WriteLine($"Dismiss task.");
                //Delete message from the queue
                queueClient.DeleteMessage(message.MessageId, message.PopReceipt);
            }
            Console.WriteLine($"Looking for {alterEgoFileName}");
            var alterEgoBlob = alterEgoContainer.GetBlobClient(alterEgoFileName);

            if (alterEgoBlob.Exists())
            {
                Console.WriteLine("Found it!");
                Console.WriteLine($"Deleting {alterEgoFileName}");


                //Delete the old blob
                alterEgoBlob.DeleteIfExists();

                //Delete message from the queue
                queueClient.DeleteMessage(message.MessageId, message.PopReceipt);
            }
            else
            {
                Console.WriteLine($"There is no alterEgo image to delete.");
                Console.WriteLine($"Dismiss task.");
                //Delete message from the queue
                queueClient.DeleteMessage(message.MessageId, message.PopReceipt);
            }

        }
        else
        {
            Console.WriteLine($"Bad message. Delete it");
            //Delete message from the queue
            queueClient.DeleteMessage(message.MessageId, message.PopReceipt);

        }
    }
    else
    {
        Console.WriteLine($"Let's wait 5 seconds");
        Thread.Sleep(5000);
    }

}

class Task
{
    public string heroName { get; set; }
    public string alterEgoName { get; set; }
}
