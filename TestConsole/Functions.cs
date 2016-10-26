using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;

namespace TestConsole
{
    public class ServiceBusBindingTestJobs
    {
        private const string PrefixForAll = "t-0001-";
        private const string PrefixMatch = "t-%rnd%-";
        private const string QueueNamePrefix = PrefixForAll + "queue-";
        private const string StartQueueName = QueueNamePrefix + "start";
        private const string TopicName = PrefixForAll + "topic";
        public static void Session([ServiceBusSessionTrigger(TopicName, QueueNamePrefix + "topic-1")] Program.SessionMessage message)
        {

         

            Console.WriteLine($"{message.Message}");
           
        }
    }

}
