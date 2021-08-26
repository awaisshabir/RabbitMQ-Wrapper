using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace CS.Event.Bus.RabbitMQ.Common
{
   public interface IRabbitMQConnectivity :IDisposable
    {
        bool IsConnected { get; }
        bool Connect();
        IModel CreateModel();
    }
}
