using CS.Event.Bus.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CS.Event.Bus.Core.Interfaces
{
    public interface IIntegrationEventHandler<in TEvent> : IIntegrationEventHandler
        where TEvent : IntegrationEvent
    {
        Task Handle(TEvent @event);
    }
    public interface IIntegrationEventHandler
    {
    }

}
