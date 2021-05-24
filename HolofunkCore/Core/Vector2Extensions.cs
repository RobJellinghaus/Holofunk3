/// Copyright by Rob Jellinghaus.  All rights reserved.

using UnityEngine;

namespace Holofunk.Core
{
    public static class Vector2Extensions
    {
        public static bool CloserThan(this Vector2 thiz, Vector2 other, float distance)
        {
            Vector2 delta = other - thiz;
            return delta.x * delta.x + delta.y * delta.y < distance * distance;
        }
    }
}
