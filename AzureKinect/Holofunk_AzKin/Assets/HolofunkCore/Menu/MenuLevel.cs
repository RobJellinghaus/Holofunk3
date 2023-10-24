/// Copyright by Rob Jellinghaus. All rights reserved.

using Holofunk.Core;
using Holofunk.Shape;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Holofunk.Menu
{
    /// <summary>
    /// A helper class which manages a set of (sub)menu items at a single level.
    /// </summary>
    public class MenuLevel
    {
        /// <summary>
        /// The containing menu.
        /// </summary>
        LocalMenu menu;

        /// <summary>
        /// Nesting depth of this level (0 = root).
        /// </summary>
        int depth;

        /// <summary>
        /// The structure of the menu at this level.
        /// </summary>
        MenuStructure menuStructure;

        /// <summary>
        /// The radius of the hollow circle (for menu item spacing).
        /// </summary>
        public float HollowCircleDiameter { get; private set; }

        /// <summary>
        /// The list of instantiated popup menu item game objects for this particular (sub)menu.
        /// </summary>
        /// <remarks>
        /// These are all children of this object's transform, and this object does hit tracking against this set.
        /// </remarks>
        List<GameObject> menuItemGameObjects = new List<GameObject>();

        /// <summary>
        /// Initialize a newly instantiated MenuLevel.
        /// </summary>
        /// <param name="menu">The local menu object this comes from.</param>
        /// <param name="parentLocalPosition">The position of this child relative to the parent.</param>
        /// <param name="depth">The nesting depth of this menu level.</param>
        /// <param name="menuStructure">The structure at this level.</param>
        public MenuLevel(
            LocalMenu menu,
            Vector3 parentLocalPosition,
            int depth,
            MenuStructure menuStructure)
        {
            this.menu = menu;
            this.depth = depth;
            this.menuStructure = menuStructure;


            for (int i = 0; i < menuStructure.Count; i++)
            {
                Vector3 submenuRootRelativePosition;

                // if this is the zeroth (base) level, add the "cancel" menu item in the middle
                if (depth == 0)
                {
                    if (i == 0)
                    {
                        submenuRootRelativePosition = Vector3.zero;
                    }
                    else
                    {
                        submenuRootRelativePosition = GetRelativePosition(parentLocalPosition, i);
                    }
                }
                else
                {
                    submenuRootRelativePosition = GetRelativePosition(parentLocalPosition, i);
                }

                GameObject menuItemGameObject = CreateMenuItem(menu.transform, submenuRootRelativePosition, menuStructure.Verb(i + 1).Name);

                menuItemGameObjects.Add(menuItemGameObject);
                // everything starts off disabled
                ColorizeMenuItem(i + 1, Color.grey);
            }
        }

        public static GameObject CreateMenuItem(Transform parentTransform, Vector3 localPosition, string text)
        {
            GameObject menuItemGameObject = ShapeContainer.InstantiateShape(ShapeType.MenuItem, parentTransform);
            // position this relative to its parent
            menuItemGameObject.transform.localPosition = localPosition;

            //_logBuffer.Append($"  Created menu item {_menuModel[i].Label} at local position {menuItemGameObject.transform.localPosition} and global position {menuItemGameObject.transform.position}{Environment.NewLine}");

            // set the text
            TextMesh textMesh = menuItemGameObject.transform.GetChild(0).GetComponent<TextMesh>();
            textMesh.text = text;

            return menuItemGameObject;
        }

        /// <summary>
        /// Get the position of the menu item with the given index, relative to the root position.
        /// </summary>
        internal Vector3 GetRelativePosition(Vector3 parentLocalPosition, int index)
        {
            Contract.Requires(index < menuStructure.Count);
            float scaledTextureDiameter = MagicNumbers.HandRadius * MagicNumbers.MenuScale;
            if (depth == 0)
            {
                // we are the root menu.  Handle 1-item and 2-item cases specially.
                if (menuStructure.Count == 1)
                {
                    Contract.Assert(index == 0);
                    return Vector3.zero;
                }
            }

            // By default we calculate the radius of a polygon whose sides are the diameter of our texture,
            // and whose angles are the angles of our menu.
            // Placing circles of that diameter at the vertices of such a polygon will result in them just touching.
            double startingAngle = 0;
            double perItemAngle = 2 * Math.PI / menuStructure.Count;
            double polygonRadius = menuStructure.Count == 1
                ? 0
                : menuStructure.Count == 2
                    ? scaledTextureDiameter
                    : (1 / Math.Sin(perItemAngle)) * scaledTextureDiameter;

            if (depth > 0)
            {
                // First calculate the angle of this item relative to the root; this is now the starting angle
                // for the submenu.
                startingAngle = Math.Atan2(parentLocalPosition.x, parentLocalPosition.y);
                Contract.Assert(!double.IsNaN(startingAngle));
                /// Now the distance.
                double thisItemDistance = parentLocalPosition.magnitude;

                // polygonRadius is the smallest circle that will fit all the items. But typically child menus
                // have fewer items than needed to fill a circle of the next radius. So we want to know, assuming
                // a radius of an additional textureDiameter, what angle separates circles which are in contact
                // at that radius?
                double outerMenuRadius = thisItemDistance + scaledTextureDiameter;

                if (polygonRadius > outerMenuRadius)
                {
                    // we need to place on the circle.  leave perItemAngle and polygonRadius alone
                }
                else
                {
                    // Now we need to know the angle of a right triangle with hypotenuse = outerMenuRadius and
                    // opposite side = textureDiameter.
                    double outerMenuAngle = Math.Asin(scaledTextureDiameter / outerMenuRadius);
                    Contract.Assert(!double.IsNaN(outerMenuAngle));

                    polygonRadius = outerMenuRadius;
                    perItemAngle = outerMenuAngle;
                    // offset starting angle so the full set of positioned items is centered on its original value
                    startingAngle -= (perItemAngle * (menuStructure.Count - 1)) / 2;
                }
            }

            // now the direction of the menu item in radians, with 0 being along positive x and the desired
            // zero menu item location being along negative Y
            double direction = perItemAngle * index + startingAngle;
            Vector2 menuItemVector = new Vector2(
                (float)(Math.Sin(direction) * polygonRadius),
                (float)(Math.Cos(direction) * polygonRadius));
            return menuItemVector;
        }

        /// <summary>
        /// Get the index of the closest menu item to the given hand location, returning an optional tuple of the
        /// minimum distance together with the index of the closest menu item.
        /// </summary>
        public Option<(float, int)> GetClosestMenuItemIfAny(Vector3 handLocation)
        {
            // see if it is within TextureRadius of any of the menu items; if so, closest one's selected
            float minDist = float.MaxValue;
            Option<int> selectedIndex = Option<int>.None;
            for (int i = 0; i < menuStructure.Count; i++)
            {
                Vector3 menuItemLocation = menuItemGameObjects[i].transform.position;

                float distance = Vector3.Distance(menuItemLocation, handLocation);

                //_logBuffer.Append($"    menuItemLocation[{i}] {menuItemLocation}, dotProduct {dotProduct}");

                if (distance < minDist)
                {
                    minDist = distance;
                    selectedIndex = i;
                }
            }

            // TODO: reimplement the concept of "disabled" menu items.

            return selectedIndex.HasValue ? ((minDist, selectedIndex.Value)) : Option<(float, int)>.None;
        }

        internal void Destroy()
        {
            foreach (GameObject gameObject in menuItemGameObjects)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
            menuItemGameObjects.Clear();
        }

        internal void ColorizeMenuItem(MenuItemId originalSelectedMenuItem, Color color)
        {
            ColorizeMenuItem(menuItemGameObjects[originalSelectedMenuItem.AsIndex], color);
        }

        public static void ColorizeMenuItem(GameObject menuItemGameObject, Color color)
        {
            menuItemGameObject.GetComponent<SpriteRenderer>().material.color = color;
            menuItemGameObject.transform.GetChild(0).gameObject.GetComponent<TextMesh>().color = color;
        }
    }
}
