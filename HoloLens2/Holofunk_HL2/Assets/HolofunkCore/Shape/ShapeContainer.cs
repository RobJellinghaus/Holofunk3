// Copyright by Rob Jellinghaus. All rights reserved.

using UnityEngine;

namespace Holofunk.Shape
{
    public enum ShapeType
    {
        /// <summary>
        /// 0.1 m radius vertically thin cylinder
        /// </summary>
        Cylinder,

        /// <summary>
        /// Hollow circle sprite (0.1 m)
        /// </summary>
        HollowCircle,

        /// <summary>
        /// A menu item (0.05 m hollow circle w/text)
        /// </summary>
        MenuItem,

        /// <summary>
        /// Simple 0.1 m sphere
        /// </summary>
        Sphere,
    }

    public static class ShapeContainer
    {
        public static GameObject InstantiateShape(ShapeType shapeType, Transform parent)
        {
            // get the ShapeContainer
            GameObject shapeContainer = GameObject.Find("ShapeContainer");
            GameObject prototypeShape = shapeContainer.transform.Find(shapeType.ToString()).gameObject;

            Core.Contract.Assert(prototypeShape != null);

            // clone the shape and put the clone at this Loopie's position
            GameObject shape = GameObject.Instantiate(prototypeShape, parent); 
            shape.transform.localPosition = Vector3.zero;
            return shape;
        }
    }
}
