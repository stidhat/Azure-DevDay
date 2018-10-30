using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Azure;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using System.Threading.Tasks;
using System.Text;
using DevDaysApp.Models;

namespace DevDaysApp.Controllers
{
    public class QueueSbController : Controller
    {
        private IQueueClient queueClient;
        private MessageReceiver receiver;
        public QueueSbController()
        {
            queueClient = new QueueClient(CloudConfigurationManager.GetSetting("ServiceBusConnectionString"), CloudConfigurationManager.GetSetting("ServiceBusQueue"));
            receiver = new MessageReceiver(CloudConfigurationManager.GetSetting("ServiceBusConnectionString"), CloudConfigurationManager.GetSetting("ServiceBusQueue"));
        }
        public async Task<ActionResult> Index()
        {
            return View(new QueueSb()
            {
                TotalOfMessages = await GetTotalMessages()
            });
        }
        [HttpPost]
        public async Task<ActionResult> AddMessage(int totalMessage)
        {
            Message message;
            for (int i = 0; i < totalMessage; i++)
            {
                message = new Message(Encoding.UTF8.GetBytes(string.Format("Test message {0}", i)));
                message.Label = string.Format("msj {0}", i);
                await queueClient.SendAsync(message);
            }
            await queueClient.CloseAsync();
            return RedirectToAction("Index", "QueueSb");
        }
        private async Task<int> GetTotalMessages()
        {
            int cont = 0;
            while (true)
            {
                var message = await receiver.PeekAsync();
                if (message != null)
                    cont++;
                else
                    break;
            }
            return cont;
        }
        public async Task<ActionResult> PeekMessage()
        {
            string result = "PEEK: There are no messages";
            var message = await receiver.PeekAsync();
            int total = await GetTotalMessages();
            if (message != null)
            {
                result = string.Format("PEEK:{0}", Encoding.UTF8.GetString(message.Body));
                total++;
            }
            await receiver.CloseAsync();
            return View("Index", new QueueSb()
            {
                TotalOfMessages = total,
                Message = result
            });
        }
        public async Task<ActionResult> GetMessage()
        {
            string result = "GET: There are no messages ";
            var message = await receiver.ReceiveAsync();
            if (message != null)
            {
                result = string.Format("GET :{0}", Encoding.UTF8.GetString(message.Body));
                await receiver.CompleteAsync(message.SystemProperties.LockToken);
            }
            Task.WaitAll();
            int total = await GetTotalMessages();
            await receiver.CloseAsync();
            return View("Index", new QueueSb()
            {
                TotalOfMessages = total,
                Message = result
            });
        }
    }
}