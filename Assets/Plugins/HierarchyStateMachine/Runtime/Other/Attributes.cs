using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SLS.StateMachineH
{
    /// <summary>
    /// Attribute to hide serialized fields in the inspector but not in the debug inspector.
    /// </summary>
    /// <remarks>
    /// This attribute is used to prevent fields that aren't meant to be manually set up from being accessible.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class HiddenSerializedAttribute : PropertyAttribute { }

}