using System.Collections.Concurrent;
using System.Threading;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Azure.SignalR.Management;

namespace cyngim.SignalRIssue.Library
{
    public class Class1
    {
        private readonly ConcurrentDictionary<string, Lazy<Task<ServiceHubContext>>> _serviceHubContexts = new ConcurrentDictionary<string, Lazy<Task<ServiceHubContext>>>();
        private readonly string _connectionString;

        public Class1(string connectionString)
        {
            _connectionString = connectionString;
        }

        private async Task PublishMessageLocal(HubMessage message, bool recreateServiceHubContext, CancellationToken cancellationToken)
        {
            var serviceHub = await GetServiceHubContext(cancellationToken);
            await serviceHub.Clients.Group("groupName").SendAsync("method", message, cancellationToken);            
        }

        private async Task<ServiceHubContext> GetServiceHubContext(CancellationToken cancellationToken = default)
        {
            var serviceHubContextTask = _serviceHubContexts.GetOrAdd("testKey", x => new Lazy<Task<ServiceHubContext>>(() => InitializeServiceHubContext(cancellationToken)));
            return await serviceHubContextTask.Value;
        }

        private async Task<ServiceHubContext> InitializeServiceHubContext(CancellationToken cancellationToken = default)
        {
            using var hubServiceManager = new ServiceManagerBuilder().WithOptions(option =>
            {
                option.ConnectionString = _connectionString;
                option.ServiceTransportType = ServiceTransportType.Persistent;
            })
            .WithNewtonsoftJson()
            .BuildServiceManager();

            var hubContext = await hubServiceManager.CreateHubContextAsync("hubName", cancellationToken);
            return hubContext;
        }

        private async Task DisposeOfServiceHubContext(string hubName)
        {
            if (_serviceHubContexts.TryRemove(hubName, out var serviceHubContextTask))
            {
                if (serviceHubContextTask.IsValueCreated)
                {
                    var serviceHubContext = await serviceHubContextTask.Value;
                    await serviceHubContext.DisposeAsync();
                }
            }
        }
    }
}