
# Step 1 how to use it in your project 
public void ConfigureServices(IServiceCollection services)
        {

        #region Register Rabbitmq services
            services.Configure<RabbitMQSettings>(options => Configuration.GetSection("RabbitMQSettings").Bind(options));
            services.AddSingleton<IRabbitMQConnectivity>(sp => {

                var connectionSetting = sp.GetRequiredService<IOptions<RabbitMQSettings>>().Value;
               var factory = new ConnectionFactory()
                {
                   HostName = connectionSetting.HostName,
                   UserName = connectionSetting.UserName,
                    Password = connectionSetting.Password,
                  Port = connectionSetting.Port,
                    DispatchConsumersAsync = true
              };

                return new RabbitMQConnectivity(factory);
            });
            services.AddSingleton<IEventBus, EventBusRabbitMQ>(sp=> {

                var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
               // var rabbitMqConnection = sp.GetRequiredService<IRabbitMQConnectivity>();
                var connectionSetting = sp.GetRequiredService<IOptions<RabbitMQSettings>>().Value;
                return new EventBusRabbitMQ(connectionSetting, scopeFactory);
            });
            #endregion
        }
# step 2 inherent IntegrationEvent to your class
  public class WeatherForecast :IntegrationEvent
    {
        public DateTime Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string Summary { get; set; }
    }
