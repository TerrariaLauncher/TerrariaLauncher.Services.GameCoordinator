using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator.Proxy.Events
{
    class HandlerArgs : EventArgs
    {
        public CancellationToken CancellationToken { get; set; } = default;

        public bool Handled { get; set; }
    }

    delegate Task Handler<TSender, TArgs>(TSender sender, TArgs args);

    class HandlerList<TSender, TArgs> where TArgs : HandlerArgs
    {
        public class HandlerItem
        {
            public int Priority { get; set; }

            public Handler<TSender, TArgs> Handler { get; set; }

            /// <summary>
            /// Indicate handler should be continue to run even after the event is handled.
            /// </summary>
            public bool SkipHandled { get; set; }
        }

        protected List<HandlerItem> handlerItems;
        public HandlerList()
        {
            this.handlerItems = new List<HandlerItem>();
        }

        public void Register(Handler<TSender, TArgs> handler, int priority = 10, bool skipHandled = false)
        {
            this.handlerItems.Add(new HandlerItem()
            {
                Handler = handler,
                Priority = priority,
                SkipHandled = skipHandled
            });

            this.handlerItems = this.handlerItems.OrderBy(handlerItem => handlerItem.Priority).ToList();
        }

        public void Deregister(Handler<TSender, TArgs> handler)
        {
            this.handlerItems.RemoveAll(handlerItem => handlerItem.Handler == handler);
        }

        public async Task Invoke(TSender sender, TArgs args)
        {
            foreach (var handlerItem in this.handlerItems)
            {
                if (args.CancellationToken.IsCancellationRequested) break;
                try
                {
                    if (!args.Handled || handlerItem.SkipHandled)
                    {
                        await handlerItem.Handler(sender, args);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
    }
}
