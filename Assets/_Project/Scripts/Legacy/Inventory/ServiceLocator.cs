using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Point d'accès centralisé aux services globaux.
/// </summary>
public static class ServiceLocator
{
    #region State

    private static readonly Dictionary<Type, object> _services = new();

    #endregion

    #region Registration

    public static void Register<T>(T service) where T : class
    {
        _services[typeof(T)] = service;
    }

    public static void Unregister<T>() where T : class
    {
        _services.Remove(typeof(T));
    }

    #endregion

    #region Access

    public static T Get<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out object service))
            return service as T;

        Debug.LogError($"[ServiceLocator] Service non enregistré : {typeof(T).Name}");
        return null;
    }

    public static bool TryGet<T>(out T service) where T : class
    {
        if (_services.TryGetValue(typeof(T), out object found))
        {
            service = found as T;
            return true;
        }

        service = null;
        return false;
    }

    #endregion

    #region Utils

    public static void Clear() => _services.Clear();

    #endregion
}