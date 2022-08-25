using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GlobalHotkeys;

namespace ClipboardHistory
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void ClipboardMonitor_Tick(object sender, EventArgs e)
        {
            var Entry = Clipboard.GetText();

            if (LayoutPanel.Controls.ToList().All(o => o.Text != Entry))
            {
                var button = new Button
                {
                    Text = Entry,
                    FlatStyle = FlatStyle.Flat,
                    FlatAppearance = { BorderColor = Color.FromArgb(50, 50, 50)},
                    BackColor = Color.FromArgb(17, 17, 17),
                    ForeColor = Color.FromArgb(153, 153, 153),
                    Size = new Size(770, 31),
                    Name = $"CopyButton_{LayoutPanel.Controls.Count + 1}"
                };

                button.Click += (_, _) =>
                {
                    Clipboard.SetText(Entry);
                };

                LayoutPanel.Controls.Add(button);

                LayoutPanel.ScrollControlIntoView(button);
            }
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case 0x84:
                    base.WndProc(ref m);
                    if ((int)m.Result == 0x1)
                        m.Result = (IntPtr)0x2;
                    return;
            }

            base.WndProc(ref m);
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowInTaskbar = true;
            WindowState = FormWindowState.Normal;
            Focus();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void SystemTrayIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ShowInTaskbar = true;
            WindowState = FormWindowState.Normal;
            Focus();
        }

        private KeyboardHook HotkeyManager = new ();

        private void MainForm_Load(object sender, EventArgs e)
        {
            HotkeyManager.KeyPressed += HotkeyManagerOnKeyPressed;
            HotkeyManager.RegisterHotKey(GlobalHotkeys.ModifierKeys.Win, Keys.V);
        }

        private void HotkeyManagerOnKeyPressed(object sender, KeyPressedEventArgs e)
        {
            if (e.Modifier == GlobalHotkeys.ModifierKeys.Win && e.Key == Keys.V)
            {
                ShowInTaskbar = true;
                WindowState = FormWindowState.Normal;
                Focus();
            }
        }
    }

    internal static class Extensions
    {
        internal static List<Control> ToList(this Control.ControlCollection Col)
        {
            var list = new List<Control>();

            foreach (Control control in Col)
            {
                list.Add(control);
            }

            return list;
        }
    }
}
