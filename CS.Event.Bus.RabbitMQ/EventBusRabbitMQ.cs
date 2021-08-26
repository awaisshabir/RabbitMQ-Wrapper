using CS.Event.Bus.Core.Interfaces;
using CS.Event.Bus.Core.Models;
using CS.Event.Bus.RabbitMQ.Common;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS.Event.Bus.RabbitMQ
{
    public class EventBusRabbitMQ : IEventBus
    {
        private string _ExchangeName;
        private readonly IRabbitMQConnectivity _Connection;
        private readonly Dictionary<string, List<Type>> _handlers;
        private readonly List<Type> _eventTypes;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private IModel _consumerChannel;
        private string _queueName;
        public EventBusRabbitMQ(IRabbitMQConnectivity connectivity,
            IServiceScopeFactory serviceScopeFactory,
            string exchangeName ,
            string queueName)
        {
            _Connection = connectivity ?? throw new ArgumentNullException(nameof(connectivity));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _handlers = new Dictionary<string, List<Type>>();
            _eventTypes = new List<Type>();
           _ExchangeName = exchangeName;
            _queueName = queueName;

            _consumerChannel = CreateConsumerChannel();
        }
        public void Publish(IntegrationEvent @event)
        {
            
            if (!_Connection.IsConnected)
            {
                _Connection.Connect();
            }
            //using (var connection = factory.CreateConnection())
            using (var channel = _Connection.CreateModel())
            {
                var eventName = @event.GetType().Name;
                //#region Declare Queue 
                //channel.QueueDeclare(eventName, false, false, false, null);
                //#endregion
                channel.ExchangeDeclare(exchange: _ExchangeName, type: "direct");

                var message = JsonConvert.SerializeObject(@event);
                var body = Encoding.UTF8.GetBytes(message);
                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = 2; // persistent

                channel.BasicPublish(
                        exchange: _ExchangeName,
                        routingKey: eventName,
                        mandatory: true,
                        basicProperties: properties,
                        body: body);

            }
        }

        public void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = typeof(T).Name;
            var handlerType = typeof(TH);
            DoInternalSubscription(eventName);

            if (!_eventTypes.Contains(typeof(T)))
            {
                _eventTypes.Add(typeof(T));
            }
            if (!_handlers.ContainsKey(eventName))
            {
                _handlers.Add(eventName, new List<Type>());
            }
            if (_handlers[eventName].Any(t=>t.GetType() == handlerType))
            {
                throw new ArgumentException($"Handler Type {handlerType.Name} already is registered for '{eventName}'",nameof(handlerType));
            }

            _handlers[eventName].Add(handlerType);
            StartConsume();
        }

        private void StartConsume() //where T : IntegrationEvent
        {
            //var factory = new ConnectionFactory()
            //{
            //    HostName = settings.HostName,
            //    UserName = settings.UserName,
            //    Password = settings.Password,
            //    Port = settings.Port,
            //    DispatchConsumersAsync = true
            //};
            //var connection = factory.CreateConnection();

            // var channel = connection.CreateModel();
            if (_consumerChannel !=null)
            {
               // var eventName = typeof(T).Name;
                #region Declare Queue 
               // channel.QueueDeclare(eventName, false, false, false, null);
                #endregion

                var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
                consumer.Received += Consumer_Received;
                _consumerChannel.BasicConsume(_queueName, true, consumer);
            }
           
        }
        
        private async Task Consumer_Received(object sender, BasicDeliverEventArgs @event)
        {
            var eventName = @event.RoutingKey;
            var message = Encoding.UTF8.GetString(@event.Body.Span);
            try
            {
                await ProcessEvent(eventName, message).ConfigureAwait(false);
            }
            catch (Exception)
            {

                
            }
        }

        private async Task ProcessEvent(string eventName, string message)
        {
            if (_handlers.ContainsKey(eventName))
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var subscriptions = _handlers[eventName];
                    foreach (var subscription in subscriptions)
                    {
                        var handler = scope.ServiceProvider.GetService(subscription);
                        if (handler == null) continue;
                        var eventType = _eventTypes.SingleOrDefault(t => t.Name == eventName);
                        var @event = JsonConvert.DeserializeObject(message, eventType);
                        var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                        await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { @event });
                    }
                }
            }
        }
        private void DoInternalSubscription(string eventName)
        {
            var containsKey = _handlers.ContainsKey(eventName);
            if (!containsKey)
            {
                if (!_Connection.IsConnected)
                {
                    _Connection.Connect();
                }

                using (var channel = _Connection.CreateModel())
                {
                    channel.QueueBind(queue: _queueName,
                                      exchange: _ExchangeName ,
                                      routingKey: eventName);
                }
            }
        }
        private IModel CreateConsumerChannel()
        {
            if (!_Connection.IsConnected)
            {
                _Connection.Connect();
            }

            var channel = _Connection.CreateModel();
            channel.ExchangeDeclare(exchange: _ExchangeName , type: "direct");

            channel.QueueDeclare(queue: _queueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            channel.CallbackException += (sender, ea) =>
            {
                
                _consumerChannel.Dispose();
                _consumerChannel = CreateConsumerChannel();
                StartConsume();
            };

            return channel;
        }

        public void Dispose()
        {
            if (_consumerChannel != null)
            {
                _consumerChannel.Dispose();
            }
        }
    }
}
