using CS.Event.Bus.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CS.Event.Bus.Core.Interfaces
{
   public interface IEventBus :IDisposable
    {
        void Publish(IntegrationEvent @event);
        void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;
    }
}
