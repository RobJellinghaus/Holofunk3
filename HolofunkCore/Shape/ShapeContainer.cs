// Copyright by Rob Jellinghaus. All rights reserved.

using UnityEngine;

namespace Holofunk.Shape
{
    public enum ShapeType
    {
        /// <summary>
        /// Pair of cones, 0.05m/0.1m/0.05m each in dimension
        /// </summary>
        Bicone,

        /// <summary>
        /// Cone, 0.05m/0.1m/0.05m
        /// </summary>
        Cone,

        /// <summary>
        /// 0.1m/0.05m/0.1m cube
        /// </summary>
        FlatCube,

        /// <summary>
        /// 0.1 m radius vertically thin cylinder
        /// </summary>
        FlatCylinder,

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
            GameObject shapeContainer = GameObject.Find(nameof(ShapeContainer));
            GameObject prototypeShape = shapeContainer.transform.Find(shapeType.ToString()).gameObject;

            Core.Contract.Assert(prototypeShape != null);

            // clone the shape and put the clone at this Loopie's position
            GameObject shape = GameObject.Instantiate(prototypeShape, parent);
            shape.SetActive(true);
            shape.transform.localPosition = Vector3.zero;
            return shape;
        }
    }
}
