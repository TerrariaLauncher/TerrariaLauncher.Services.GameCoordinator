using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator.Proxy.Events
{
    class InterceptorStartedArgs : HandlerArgs
    {

    }

    class InterceptorStoppedArgs : HandlerArgs
    {

    }

    class InterceptorEvents
    {
        public InterceptorEvents(ILoggerFactory loggerFactory)
        {
            this.InterceptorStartedHandlers = new HandlerList<Interceptor, InterceptorStartedArgs>(
                loggerFactory.CreateLogger($"{typeof(InterceptorEvents).FullName}.{nameof(InterceptorStartedHandlers)}")
            );
            this.InterceptorStoppedHandlers = new HandlerList<Interceptor, InterceptorStoppedArgs>(
                loggerFactory.CreateLogger($"{typeof(InterceptorEvents).FullName}.{nameof(InterceptorStoppedHandlers)}")
            );
        }

        public HandlerList<Interceptor, InterceptorStartedArgs> InterceptorStartedHandlers;
        public HandlerList<Interceptor, InterceptorStoppedArgs> InterceptorStoppedHandlers;

        public Task OnStarted(Interceptor sender, CancellationToken cancellationToken = default)
        {
            return this.InterceptorStartedHandlers.Invoke(sender, new InterceptorStartedArgs()
            {
                CancellationToken = cancellationToken
            });
        }

        public Task OnStopped(Interceptor sender, CancellationToken cancellationToken = default)
        {
            return this.InterceptorStoppedHandlers.Invoke(sender, new InterceptorStoppedArgs()
            {
                CancellationToken = cancellationToken
            });
        }
    }
}
