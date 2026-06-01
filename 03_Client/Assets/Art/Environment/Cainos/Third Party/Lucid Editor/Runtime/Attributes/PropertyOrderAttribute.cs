using System;
using UnityEngine;

namespace Cainos.LucidEditor
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property)]
    public class PropertyOrderAttribute : Attribute
    {
        public readonly int propertyOrder;

        public PropertyOrderAttribute(int propertyOrder)
        {
            this.propertyOrder = propertyOrder;
        }
    }
}