using System;
using System.Collections.Generic;
using System.Text;

namespace CS.Event.Bus.RabbitMQ.Common
{
    public class RabbitMQSettings
    {
        
        public string HostName { get;  set; }
        public string UserName { get;  set; }
        public string Password { get;  set; }
        public int Port { get;  set; }

        
    }
}
