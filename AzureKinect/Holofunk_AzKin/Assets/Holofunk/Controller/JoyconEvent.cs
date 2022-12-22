/// Copyright by Rob Jellinghaus.  All rights reserved.

using Holofunk.Core;
using Holofunk.Hand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Holofunk.Controller
{
    /// <summary>A Joycon event in a state machine.</summary>
    public struct JoyconEvent
    {
        /// <summary>
        /// Which button?
        /// </summary>
        readonly Joycon.Button _button;
        /// <summary>
        /// Is the button down?
        /// </summary>
        readonly bool _isDown;
        /// <summary>
        /// Has this event been captured by another UI layer?
        /// </summary>
        readonly bool _isCaptured;

        /// <summary>The button that was down or up.</summary>
        internal Joycon.Button Button => _button;

        /// <summary>
        /// Is the button down? (if not, it's up)
        /// </summary>
        internal bool IsDown => _isDown;

        /// <summary>
        /// Has this event already been captured?
        /// </summary>
        /// <remarks>
        /// We may still want to respond to already-captured events in some way (e.g. by changing a hand sprite),
        /// but we want to be able to detect that the capture already took place, and modify the transition
        /// appropriately.
        /// </remarks>
        internal bool IsCaptured => _isCaptured;

        internal JoyconEvent(Joycon.Button button, bool isDown) { _button = button; _isDown = isDown; _isCaptured = false; }

        /// <summary>
        /// Construct a captured JoyconEvent from an existing one.
        /// </summary>
        internal JoyconEvent AsCaptured() { return new JoyconEvent(this); }

        private JoyconEvent(JoyconEvent other) { _button = other._button; _isDown = other._isDown; _isCaptured = true; }

        public static bool operator ==(JoyconEvent l, JoyconEvent r)
        {
            return l.Button == r.Button && l.IsDown == r.IsDown && l.IsCaptured == r.IsCaptured;
        }

        public static bool operator !=(JoyconEvent l, JoyconEvent r)
        {
            return !(l == r);
        }

        public override string ToString()
        {
            return Button.ToString() + (_isDown ? "(down)" : "(up)") + (_isCaptured ? "(captured)" : "");
        }

        public override bool Equals(object obj)
        {
            return obj is JoyconEvent @event && (this == @event);
        }

        public override int GetHashCode()
        {
            int hashCode = 1793886979;
            hashCode = hashCode * -1521134295 + _button.GetHashCode();
            hashCode = hashCode * -1521134295 + _isDown.GetHashCode();
            hashCode = hashCode * -1521134295 + _isCaptured.GetHashCode();
            return hashCode;
        }

        /// <summary>
        /// Trigger is pressed.
        /// </summary>
        public static readonly JoyconEvent TriggerPressed = new JoyconEvent(Joycon.Button.SHOULDER_2, isDown: true);
        /// <summary>
        /// Trigger is released.
        /// </summary>
        public static readonly JoyconEvent TriggerReleased = new JoyconEvent(Joycon.Button.SHOULDER_2, isDown: false);
        /// <summary>
        /// Shoulder is pressed.
        /// </summary>
        public static readonly JoyconEvent ShoulderPressed = new JoyconEvent(Joycon.Button.SHOULDER_1, isDown: true);
        /// <summary>
        /// Shoulder is released.
        /// </summary>
        public static readonly JoyconEvent ShoulderReleased = new JoyconEvent(Joycon.Button.SHOULDER_1, isDown: false);
        /// <summary>
        /// D-pad up is pressed.
        /// </summary>
        public static readonly JoyconEvent DPadUpPressed = new JoyconEvent(Joycon.Button.DPAD_UP, isDown: true);
        /// <summary>
        /// D-pad up is released.
        /// </summary>
        public static readonly JoyconEvent DPadUpReleased = new JoyconEvent(Joycon.Button.DPAD_UP, isDown: false);
        /// <summary>
        /// D-pad left is pressed.
        /// </summary>
        public static readonly JoyconEvent DPadLeftPressed = new JoyconEvent(Joycon.Button.DPAD_LEFT, isDown: true);
        /// <summary>
        /// D-pad left is released.
        /// </summary>
        public static readonly JoyconEvent DPadLeftReleased = new JoyconEvent(Joycon.Button.DPAD_LEFT, isDown: false);
        /// <summary>
        /// D-pad right is pressed.
        /// </summary>
        public static readonly JoyconEvent DPadRightPressed = new JoyconEvent(Joycon.Button.DPAD_RIGHT, isDown: true);
        /// <summary>
        /// D-pad right is released.
        /// </summary>
        public static readonly JoyconEvent DPadRightReleased = new JoyconEvent(Joycon.Button.DPAD_RIGHT, isDown: false);
        /// <summary>
        /// D-pad down is pressed.
        /// </summary>
        public static readonly JoyconEvent DPadDownPressed = new JoyconEvent(Joycon.Button.DPAD_DOWN, isDown: true);
        /// <summary>
        /// D-pad down is released.
        /// </summary>
        public static readonly JoyconEvent DPadDownReleased = new JoyconEvent(Joycon.Button.DPAD_DOWN, isDown: false);
    }

    public class JoyconEventComparer : IComparer<JoyconEvent>
    {
        internal static readonly JoyconEventComparer Instance = new JoyconEventComparer();

        public int Compare(JoyconEvent x, JoyconEvent y)
        {
            int delta = (int)x.Button - (int)y.Button;
            if (delta != 0) return delta;
            if (x.IsDown != y.IsDown)
            {
                return x.IsDown ? -1 : 1;
            }
            if (x.IsCaptured != y.IsCaptured)
            {
                return x.IsCaptured ? 1 : -1;
            }
            return 0;
        }
    }
}
