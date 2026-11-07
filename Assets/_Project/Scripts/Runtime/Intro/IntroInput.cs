using UnityEngine.InputSystem;

namespace CoinRush.Intro
{
    /// <summary>"Any key / click / gamepad button" checks for the intro screens (new Input System).</summary>
    public static class IntroInput
    {
        public static bool AnyPressed()
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            Gamepad pad = Gamepad.current;
            return pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame);
        }

        public static bool NextPressed()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)) return true;
            Gamepad pad = Gamepad.current;
            return pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.dpad.right.wasPressedThisFrame);
        }

        public static bool BackPressed()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)) return true;
            Gamepad pad = Gamepad.current;
            return pad != null && pad.dpad.left.wasPressedThisFrame;
        }

        public static bool SkipPressed()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;
            Gamepad pad = Gamepad.current;
            return pad != null && (pad.startButton.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame);
        }
    }
}
