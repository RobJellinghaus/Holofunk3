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
        /// A container for a hand-held game object; scaled to 0.1m
        /// </summary>
        HandHolder,

        /// <summary>
        /// Hollow circle
        /// </summary>
        HollowCircleSprite,

        /// <summary>
        /// Hollow hexagon
        /// </summary>
        HollowHexagonSprite,

        /// <summary>
        /// Hollow square
        /// </summary>
        HollowSquareSprite,

        /// <summary>
        /// A menu item (child 0 = text; child 1 = shape); scaled to 0.1m
        /// </summary>
        MenuItem,

        /// <summary>
        /// A microphone
        /// </summary>
        MicrophoneSprite,

        /// <summary>
        /// The mute icon in a circle
        /// </summary>
        MuteCircleSprite,

        /// <summary>
        /// A white dot in a circle, indicating recording mode (but not currently recording).
        /// </summary>
        NoRecCircleSprite,

        /// <summary>
        /// A number 1 in an oval
        /// </summary>
        Number1Sprite,

        /// <summary>
        /// A number 2 in an oval
        /// </summary>
        Number2Sprite,

        /// <summary>
        /// A red dot in a circle, indicating active recording.
        /// </summary>
        RecCircleSprite,

        /// <summary>
        /// Simple 0.1 m sphere
        /// </summary>
        Sphere,

        /// <summary>
        /// The "unmute" speaker-with-waves icon in a circle.
        /// </summary>
        UnmuteCircleSprite,
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
