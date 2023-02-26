using System.Reflection;

namespace CloudBackup.Management.WebSockets
{
    public static class WebSocketManagerExtensions
    {
        public static IServiceCollection AddWebSocketManager(this IServiceCollection services)
        {
            services.AddTransient<WebSocketConnectionManager>();

            var entryAssembly = Assembly.GetEntryAssembly();

            if (entryAssembly != null)
            {
                foreach (var type in entryAssembly.ExportedTypes)
                {
                    if (type.GetTypeInfo().BaseType == typeof(WebSocketHandler))
                    {
                        services.AddSingleton(type);
                    }
                }
            }

            return services;
        }

        public static IApplicationBuilder MapWebSocketManager(this IApplicationBuilder app, 
                                                              PathString path,
                                                              WebSocketHandler? handler)
        {
            return app.Map(path, (_app) => _app.UseMiddleware<WebSocketManagerMiddleware>(handler));
        }
    }
}
