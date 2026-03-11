using System;
using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Core.Services
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, IService> _services = new();
        private static bool _isInitialized;

        public static void Register<T>(T service) where T : class, IService
        {
            var type = typeof(T);

            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] Service {type.Name} already registered. Replacing.");
                _services[type].Shutdown();
            }

            _services[type] = service;
            service.Initialize();
        }

        public static T Get<T>() where T : class, IService
        {
            var type = typeof(T);

            if (_services.TryGetValue(type, out var service))
            {
                return service as T;
            }

            Debug.LogError($"[ServiceLocator] Service {type.Name} not found.");
            return null;
        }

        public static bool TryGet<T>(out T service) where T : class, IService
        {
            var type = typeof(T);

            if (_services.TryGetValue(type, out var found))
            {
                service = found as T;
                return true;
            }

            service = null;
            return false;
        }

        public static bool IsRegistered<T>() where T : class, IService
        {
            return _services.ContainsKey(typeof(T));
        }

        public static void Unregister<T>() where T : class, IService
        {
            var type = typeof(T);

            if (_services.TryGetValue(type, out var service))
            {
                service.Shutdown();
                _services.Remove(type);
            }
        }

        public static void Clear()
        {
            foreach (var service in _services.Values)
            {
                service.Shutdown();
            }
            _services.Clear();
            _isInitialized = false;
        }

        public static IReadOnlyDictionary<Type, IService> GetAllServices()
        {
            return _services;
        }
    }
}
