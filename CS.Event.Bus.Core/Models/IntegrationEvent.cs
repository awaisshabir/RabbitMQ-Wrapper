using System;
using System.Collections.Generic;
using System.Text;

namespace CS.Event.Bus.Core.Models
{
    public abstract class IntegrationEvent
    {
        public IntegrationEvent()
        {
            Id = Guid.NewGuid();
            CreationDate = DateTime.Now;
        }
        public Guid Id { get; protected set; }
        public DateTime CreationDate { get; protected set; }
    }
}
