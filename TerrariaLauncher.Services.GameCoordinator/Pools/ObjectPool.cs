using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator
{
    class ObjectPool<T>
    {
        private readonly IObjectPoolPolicy<T> policy;
        private readonly ConcurrentBag<T> instances;

        public ObjectPool(IObjectPoolPolicy<T> policy)
        {
            if (policy is null) throw new ArgumentNullException(nameof(policy));

            this.policy = policy;
            this.instances = new ConcurrentBag<T>();
        }

        public T Get()
        {
            if (this.instances.TryTake(out var instance))
            {
                return instance;
            }

            return this.policy.Create();
        }

        public void Return(T instance)
        {
            if (this.policy.Return(instance))
            {
                if (this.instances.Count > this.policy.RetainedSize)
                {
                    return;
                }
                else
                {
                    this.instances.Add(instance);
                }
            }
        }
    }
}
