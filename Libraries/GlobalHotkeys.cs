using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GlobalHotkeys
{
    public sealed class KeyboardHook : IDisposable
    {
        // Registers a hot key with Windows.
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        // Unregisters the hot key with Windows.
        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        /// <summary>
        /// Represents the window that is used internally to get the messages.
        /// </summary>
        public sealed class Window : NativeWindow, IDisposable
        {
            public static int WM_HOTKEY = 0x0312;

            public Window()
            {
                // create the handle for the window.
                CreateHandle(new CreateParams());
            }

            /// <summary>
            /// Overridden to get the notifications.
            /// </summary>
            /// <param name="m"></param>
            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);

                // check if we got a hot key pressed.
                if (m.Msg == WM_HOTKEY)
                {
                    // get the keys.
                    var key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
                    var modifier = (ModifierKeys)((int)m.LParam & 0xFFFF);

                    // invoke the event to notify the parent.
                    if (KeyPressed != null)
                        KeyPressed(this, new KeyPressedEventArgs(modifier, key));
                }
            }

            public event EventHandler<KeyPressedEventArgs> KeyPressed;

            #region IDisposable Members

            public void Dispose()
            {
                DestroyHandle();
            }

            #endregion
        }

        public Window _window = new();
        public int _currentId;
        public List<int> PausedHotkeys = new();

        public KeyboardHook()
        {
            // register the event of the inner native window.
            _window.KeyPressed += delegate (object sender, KeyPressedEventArgs args)
            {
                if (KeyPressed != null)
                    KeyPressed(this, args);
            };
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Registers a hot key in the system.
        /// </summary>
        /// <param name="modifier">The modifiers that are associated with the hot key.</param>
        /// <param name="key">The key itself that is associated with the hot key.</param>
        public void RegisterHotKey(ModifierKeys modifier, Keys key, Form LimitedTo = null)
        {
            // increment the counter.
            _currentId += 1;

            // register the hot key.
            if (!RegisterHotKey(_window.Handle, _currentId, (uint)modifier, (uint)key))
                throw new InvalidOperationException("Couldn’t register the hot key.");

            if (LimitedTo != null)
            {
                //Local
                var ID = _currentId;

                LimitedTo.Activated += (_, _) =>
                {
                    if (PausedHotkeys.Contains(ID))
                    {
                        if (!RegisterHotKey(_window.Handle, ID, (uint)modifier, (uint)key))
                            throw new InvalidOperationException("Couldn’t register the hot key.");

                        PausedHotkeys.Remove(ID);
                    }
                };

                LimitedTo.Deactivate += (_, _) =>
                {
                    if (!PausedHotkeys.Contains(ID))
                    {
                        if (!UnregisterHotKey(_window.Handle, ID))
                            throw new InvalidOperationException("Couldn’t unregister the hot key.");

                        PausedHotkeys.Add(ID);
                    }
                };
            }
        }

        /// <summary>
        /// A hot key has been pressed.
        /// </summary>
        public event EventHandler<KeyPressedEventArgs> KeyPressed;

        #region IDisposable Members

        public void UnregisterAllHotkeys()
        {
            // unregister all the registered hot keys.
            for (var i = _currentId; i > 0; i--)
            {
                if (!UnregisterHotKey(_window.Handle, i))
                    throw new InvalidOperationException("Couldn’t unregister the hot key.");
            }
        }

        public void Dispose()
        {
            // unregister all the registered hot keys.
            for (var i = _currentId; i > 0; i--)
            {
                if (!UnregisterHotKey(_window.Handle, i))
                    throw new InvalidOperationException("Couldn’t unregister the hot key.");
            }

            // dispose the inner native window.
            _window.Dispose();
        }

        #endregion
    }

    /// <summary>
    /// Event Args for the event that is fired after the hot key has been pressed.
    /// </summary>
    public class KeyPressedEventArgs : EventArgs
    {
        public ModifierKeys _modifier;
        public Keys _key;

        internal KeyPressedEventArgs(ModifierKeys modifier, Keys key)
        {
            _modifier = modifier;
            _key = key;
        }

        public ModifierKeys Modifier => _modifier;

        public Keys Key => _key;
    }

    /// <summary>
    /// The enumeration of possible modifiers.
    /// </summary>
    [Flags]
    public enum ModifierKeys : uint
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Win = 8
    }
}
