using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.TestCommon;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Nito.AsyncEx;

namespace TestConsole
{
    public class Program
    {
        private const string PrefixForAll = "t-0001-";
        private const string QueueNamePrefix = PrefixForAll + "queue-";
        private const string StartQueueName = QueueNamePrefix + "start";
        private const string SessionQueueName = QueueNamePrefix + "session";
        private const string TopicName = PrefixForAll + "topic";

        static void Main(string[] args)
        {
            new Program().Run("TestQueueName").GetAwaiter().GetResult();
        }


        public async Task Run(string queueName)
        {


            var serviceBusConfig = new ServiceBusConfiguration();
            serviceBusConfig.MessageOptions.AutoComplete = true;
            var namespaceManager = NamespaceManager.CreateFromConnectionString(serviceBusConfig.ConnectionString);
            var secondaryConnectionString = ConfigurationManager.ConnectionStrings["ServiceBusSecondary"].ConnectionString;
            var secondaryNamespaceManager = NamespaceManager.CreateFromConnectionString(secondaryConnectionString);

            TestTraceWriter trace = new TestTraceWriter(TraceLevel.Info);
     

            JobHostConfiguration config = new JobHostConfiguration
            {
                TypeLocator = new FakeTypeLocator(typeof(ServiceBusBindingTestJobs)),
                
            };
            config.Tracing.Tracers.Add(trace);
            config.UseServiceBus(serviceBusConfig);
      
            JobHost host = new JobHost(config);

            var random = new Random();

            for (int i = 0; i < 10; i ++)
            {
                var message = new SessionMessage { EndSession = i % 5  == 0, Message = $"Message {i}" };
                SendTopicMessage(secondaryNamespaceManager, secondaryConnectionString, TopicName, message,
                    random.Next(1, 5).ToString());
            }

            Console.WriteLine("Press any key to start processing");
            Console.ReadKey();

            await host.StartAsync();


            Console.WriteLine("Press any key to exit the scenario");
            Console.ReadKey();

            // Wait for the host to terminate
            await host.StopAsync();

            host.Dispose();


        }

        private void SendTopicMessage<T>(NamespaceManager namespaceManager, string connectionString, string topicName, T messageObject, string session = null)
        {
            if (!namespaceManager.TopicExists(topicName))
            {
                namespaceManager.CreateTopic(new TopicDescription(topicName) {  });
            }

            TopicClient topicClient = TopicClient.CreateFromConnectionString(connectionString, topicName);

            string messageContent = JsonConvert.SerializeObject(messageObject);
            BrokeredMessage message = new BrokeredMessage(new MemoryStream(Encoding.UTF8.GetBytes(messageContent)), true);
            message.ContentType = "application/json";
            if (!string.IsNullOrEmpty(session))
            {
                message.SessionId = session;
            }

            topicClient.Send(message);
            topicClient.Close();
        }

        private void SendQueueMessage<T>(NamespaceManager namespaceManager, string connectionString, string queueName, T messageObject, string session = null)
        {
            if (!namespaceManager.QueueExists(queueName))
            {
                namespaceManager.CreateQueue(new QueueDescription(queueName) { RequiresSession = !string.IsNullOrEmpty(session) });
            }

            QueueClient queueClient = QueueClient.CreateFromConnectionString(connectionString, queueName);

            string messageContent = JsonConvert.SerializeObject(messageObject);
            BrokeredMessage message = new BrokeredMessage(new MemoryStream(Encoding.UTF8.GetBytes(messageContent)), true);
            message.ContentType = "application/json";
            if (!string.IsNullOrEmpty(session))
            {
                message.SessionId = session;
            }

            queueClient.Send(message);
            queueClient.Close();
        }


        public class SessionMessage
        {
            public string Message { get; set; }

            public bool EndSession { get; set; }
        }

    }
}
