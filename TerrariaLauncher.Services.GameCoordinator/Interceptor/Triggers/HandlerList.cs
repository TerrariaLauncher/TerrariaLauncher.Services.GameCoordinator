using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator
{
    class HandlerList<TSender, TArgs> where TArgs : EventArgs
    {
        public delegate Task Handler(TSender sender, TArgs args);

        public class HandlerItem
        {
            public int Priority { get; set; }
            public Handler Handler { get; set; }
        }

        protected List<HandlerItem> handlerItems;
        public HandlerList()
        {
            this.handlerItems = new List<HandlerItem>();
        }

        public void Register(Handler handler, int priority = 10)
        {
            this.handlerItems.Add(new HandlerItem()
            {
                Handler = handler,
                Priority = priority
            });

            this.handlerItems = this.handlerItems.OrderBy(handlerItem => handlerItem.Priority).ToList();
        }

        public void Deregister(Handler handler, int priority)
        {
            this.handlerItems.RemoveAll(handlerItem => handlerItem.Handler == handler);
        }

        public async Task Invoke(TSender sender, TArgs args)
        {
            foreach (var handlerItem in this.handlerItems)
            {
                await handlerItem.Handler(sender, args);
            }
        }
    }
}
