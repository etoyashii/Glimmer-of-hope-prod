using System;
using UnityEngine;

/// <summary>
/// Canal d'événement générique.
/// Crée un asset SO par canal, referencé dans l'Inspector
/// Pas de couplage statique.
/// </summary>
public abstract class EventChannel<T> : ScriptableObject
{
    #region State

    private event Action<T> OnEvent;

    #endregion

    #region API

    public void Raise(T value) => OnEvent?.Invoke(value);
    public void Subscribe(Action<T> handler) => OnEvent += handler;
    public void Unsubscribe(Action<T> handler) => OnEvent -= handler;

    #endregion
}

/// <summary>Canal sans payload.</summary>
[CreateAssetMenu(menuName = "Inventory/Events/VoidEventChannel")]
public class VoidEventChannel : ScriptableObject
{
    #region State

    private event Action OnEvent;

    #endregion

    #region API

    public void Raise() => OnEvent?.Invoke();
    public void Subscribe(Action handler) => OnEvent += handler;
    public void Unsubscribe(Action handler) => OnEvent -= handler;

    #endregion
}