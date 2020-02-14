using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Caliburn.Micro
{
    /// <summary>
    ///   A simple IoC container.
    /// </summary>
    public class SimpleContainer
    {
        private static readonly Type delegateType = typeof(Delegate);
        private static readonly Type enumerableType = typeof(IEnumerable);
        private readonly List<ContainerEntry> entries;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleContainer" /> class.
        /// </summary>
        public SimpleContainer()
        {
            entries = new List<ContainerEntry>();
        }

        private SimpleContainer(IEnumerable<ContainerEntry> entries)
        {
            this.entries = new List<ContainerEntry>(entries);
        }

        /// <summary>
        /// Whether to enable recursive property injection for all resolutions.
        /// </summary>
        public bool EnablePropertyInjection { get; set; }

        /// <summary>
        /// Registers the instance.
        /// </summary>
        /// <param name="service">The service.</param>
        /// <param name="key">The key.</param>
        /// <param name="implementation">The implementation.</param>
        public void RegisterInstance(Type service, string key, object implementation)
        {
            RegisterHandler(service, key, container => implementation);
        }

        /// <summary>
        /// Registers the class so that a new instance is created on every request.
        /// </summary>
        /// <param name="service">The service.</param>
        /// <param name="key">The key.</param>
        /// <param name="implementation">The implementation.</param>
        public void RegisterPerRequest(Type service, string key, Type implementation)
        {
            RegisterHandler(service, key, container => container.BuildInstance(implementation));
        }

        /// <summary>
        /// Registers the class so that it is created once, on first request, and the same instance is returned to all requester thereafter.
        /// </summary>
        /// <param name="service">The service.</param>
        /// <param name="key">The key.</param>
        /// <param name="implementation">The implementation.</param>
        public void RegisterSingleton(Type service, string key, Type implementation)
        {
            object singleton = null;
            RegisterHandler(service, key, container => singleton ??= container.BuildInstance(implementation));
        }

        /// <summary>
        /// Registers a custom handler for serving requests from the container.
        /// </summary>
        /// <param name="service">The service.</param>
        /// <param name="key">The key.</param>
        /// <param name="handler">The handler.</param>
        public void RegisterHandler(Type service, string key, Func<SimpleContainer, object> handler)
        {
            GetOrCreateEntry(service, key).Add(handler);
        }

        /// <summary>
        /// Unregistered any handlers for the service/key that have previously been registered.
        /// </summary>
        /// <param name="service">The service.</param>
        /// <param name="key">The key.</param>
        public void UnregisterHandler(Type service, string key)
        {
            var entry = GetEntry(service, key);
            if (!(entry is null))
            {
                entries.Remove(entry);
            }
        }

        /// <summary>
        /// Requests an instance.
        /// </summary>
        /// <param name="service">The service.</param>
        /// <param name="key">The key.</param>
        /// <returns>The instance, or null if a handler is not found.</returns>
        public object GetInstance(Type service, string key)
        {
            var entry = GetEntry(service, key);
            if (!(entry is null))
            {
                var instance = entry.Single()(this);

                if (EnablePropertyInjection && !(instance is null))
                {
                    BuildUp(instance);
                }

                return instance;
            }

            if (service is null)
            {
                return null;
            }

            if (delegateType.GetTypeInfo().IsAssignableFrom(service.GetTypeInfo()))
            {
                var typeToCreate = service.GetTypeInfo().GenericTypeArguments[0];
                var factoryFactoryType = typeof(FactoryFactory<>).MakeGenericType(typeToCreate);
                var factoryFactoryHost = Activator.CreateInstance(factoryFactoryType);
                var factoryFactoryMethod = factoryFactoryType.GetRuntimeMethod("Create", new[] { typeof(SimpleContainer) });
                return factoryFactoryMethod.Invoke(factoryFactoryHost, new object[] { this });
            }

            if (!enumerableType.GetTypeInfo().IsAssignableFrom(service.GetTypeInfo()) || !service.GetTypeInfo().IsGenericType)
            {
                return null;
            }

            var listType = service.GetTypeInfo().GenericTypeArguments[0];
            var instances = GetAllInstances(listType).ToList();
            var array = Array.CreateInstance(listType, instances.Count);

            for (var i = 0; i < array.Length; i++)
            {
                if (EnablePropertyInjection)
                {
                    BuildUp(instances[i]);
                }

                array.SetValue(instances[i], i);
            }

            return array;
        }

        /// <summary>
        /// Determines if a handler for the service/key has previously been registered.
        /// </summary>
        /// <param name="service">The service.</param>
        /// <param name="key">The key.</param>
        /// <returns>True if a handler is register; false otherwise.</returns>
        public bool HasHandler(Type service, string key)
        {
            return !(GetEntry(service, key) is null);
        }

        /// <summary>
        /// Requests all instances of a given type and the given key (default null).
        /// </summary>
        /// <param name="service">The service.</param>
        /// <param name="key">The key shared by those instances</param>
        /// <returns>All the instances or an empty enumerable if none are found.</returns>
        public IEnumerable<object> GetAllInstances(Type service, string key = null)
        {
            var Entries = GetEntry(service, key);

            if (Entries is null)
            {
                return Array.Empty<object>();
            }

            foreach (var instance in Entries.Select(e => e(this)).Where(instance => EnablePropertyInjection && !(instance is null)))
            {
                BuildUp(instance);
            }

            return Entries.Select(e => e(this));
        }

        /// <summary>
        /// Pushes dependencies into an existing instance based on interface properties with setters.
        /// </summary>
        /// <param name="instance">The instance.</param>
        public void BuildUp(object instance)
        {
            var properties = instance
                .GetType()
                .GetRuntimeProperties()
                .Where(p => p.CanRead && p.CanWrite && p.PropertyType.GetTypeInfo().IsInterface);

            foreach (var property in properties)
            {
                var value = GetInstance(property.PropertyType, null);

                if (!(value is null))
                {
                    property.SetValue(instance, value, null);
                }
            }
        }

        /// <summary>
        /// Creates a child container.
        /// </summary>
        /// <returns>A new container.</returns>
        public SimpleContainer CreateChildContainer()
        {
            return new SimpleContainer(entries);
        }

        private ContainerEntry GetOrCreateEntry(Type service, string key)
        {
            var entry = GetEntry(service, key);
            if (!(entry is null))
            {
                return entry;
            }

            entry = new ContainerEntry { Service = service, Key = key };
            entries.Add(entry);

            return entry;
        }

        private ContainerEntry GetEntry(Type service, string key)
        {
            return service is null
                ? entries.FirstOrDefault(x => x.Key == key)
                : key is null
                ? entries.FirstOrDefault(x => x.Service == service && x.Key is null)
                  ?? entries.FirstOrDefault(x => x.Service == service)
                : entries.FirstOrDefault(x => x.Service == service && x.Key == key);
        }

        /// <summary>
        /// Actually does the work of creating the instance and satisfying it's constructor dependencies.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        protected object BuildInstance(Type type)
        {
            var args = DetermineConstructorArgs(type);
            return ActivateInstance(type, args);
        }

        /// <summary>
        /// Creates an instance of the type with the specified constructor arguments.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="args">The constructor args.</param>
        /// <returns>The created instance.</returns>
        protected object ActivateInstance(Type type, object[] args)
        {
            var instance = args?.Length > 0
                ? Activator.CreateInstance(type, args)
                : Activator.CreateInstance(type);

            Activated(instance);
            return instance;
        }

        /// <summary>
        /// Occurs when a new instance is created.
        /// </summary>
        public event Action<object> Activated = delegate { };

        private object[] DetermineConstructorArgs(Type implementation)
        {
            var args = new List<object>();
            var constructor = SelectEligibleConstructor(implementation);

            if (!(constructor is null))
            {
                args.AddRange(constructor.GetParameters().Select(info => GetInstance(info.ParameterType, null)));
            }

            return args.ToArray();
        }

        private ConstructorInfo SelectEligibleConstructor(Type type)
        {
            return type.GetTypeInfo().DeclaredConstructors
                .Where(c => c.IsPublic)
                .Select(c => new
                {
                    Constructor = c,
                    HandledParamters = c.GetParameters().Count(p => HasHandler(p.ParameterType, null))
                })
                .OrderByDescending(c => c.HandledParamters)
                .Select(c => c.Constructor)
                .FirstOrDefault();
        }

        private sealed class ContainerEntry : List<Func<SimpleContainer, object>>
        {
            public string Key;
            public Type Service;
        }

        private sealed class FactoryFactory<T>
        {
            public Func<T> Create(SimpleContainer container) => () => (T)container.GetInstance(typeof(T), null);
        }
    }
}
