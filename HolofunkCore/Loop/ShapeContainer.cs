// Copyright by Rob Jellinghaus. All rights reserved.

using UnityEngine;

namespace Holofunk.Loop
{
    public enum ShapeType
    {
        /// <summary>
        /// Simple 0.1 m sphere
        /// </summary>
        Sphere,
        
        /// <summary>
        /// 0.1 m radius vertically thin cylinder
        /// </summary>
        Cylinder
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
