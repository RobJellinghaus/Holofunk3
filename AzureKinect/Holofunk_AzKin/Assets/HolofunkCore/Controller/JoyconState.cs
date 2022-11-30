using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.HolofunkCore.Controller
{
    /// <summary>Immutable struct describing a state of a Wii controller.</summary>
    public struct JoyconState
    {
        // True if button is down, false if up.
        public readonly bool ButtonA;
        public readonly bool ButtonB;
        public readonly bool Minus;
        public readonly bool Plus;
        public readonly bool Home;
        public readonly bool Down;
        public readonly bool Up;
        public readonly bool Left;
        public readonly bool Right;
        public readonly bool One;
        public readonly bool Two;
        public readonly float BatteryLevel;

        /*
        internal JoyconState(WiimoteState ws)
        {
            ButtonA = ws.ButtonState.A;
            ButtonB = ws.ButtonState.B;
            Minus = ws.ButtonState.Minus;
            Plus = ws.ButtonState.Plus;
            Home = ws.ButtonState.Home;
            Down = ws.ButtonState.Down;
            Up = ws.ButtonState.Up;
            Left = ws.ButtonState.Left;
            Right = ws.ButtonState.Right;
            One = ws.ButtonState.One;
            Two = ws.ButtonState.Two;
            BatteryLevel = ws.Battery;
        }
        */
    }
}
