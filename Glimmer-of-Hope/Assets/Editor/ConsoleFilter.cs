using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class ConsoleFilter
{
    const string MulticastKey = "ConsoleFilter_MulticastShown";

    static ConsoleFilter()
    {
        if (!SessionState.GetBool(MulticastKey, false))
        {
            SessionState.SetBool(MulticastKey, true);
            EditorApplication.delayCall += ShowNotice;
        }
    }

    static void ShowNotice()
    {
        Debug.Log("<color=#888>[Info]</color> Multicast error (10013) is expected on some networks. Safe to ignore.");
    }
}
