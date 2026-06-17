using System.Collections.Generic;
using UnityEngine;
using System;

using static GlimmerOfHope.Editor.PlayModeSaver.PlayModeSaver;

// Serializes the instance IDs of any object reference fields. If internal, the index of the object in the serializer list is stored instead.

[System.Serializable]
public class InstanceReference
{
    #region Public Properties

    public bool isNull;
    public int id;
    public bool isInternal;
    #endregion

    #region Public Methods

    public InstanceReference()
    {
        isNull = true;
    }

    public InstanceReference(int id, bool isInternal)
    {
        this.id = id;
        this.isInternal = isInternal;
    }

    #endregion
}