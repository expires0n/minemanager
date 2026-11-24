using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MineManager
{
    /// <summary>
    /// Share-safe reports: identifiers (computer name, user, profile path, serial
    /// numbers, IP/MAC addresses) are masked, and the exact text is previewed before
    /// anything is copied or saved. Nothing is uploaded — this is about what the user
    /// hands to someone else.
    /// </summary>
    internal static class Privacy
    {
        /// <summary>Line-based masking of the report text.</summary>
        public static string Scrub(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            string machine = Environment.MachineName ?? "";
            string user = Environment.UserName ?? "";
            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) ?? "";

            string[] lines = text.Replace("\r\n", "\n").Split('\n');
            StringBuilder sb = new StringBuilder(text.Length);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string masked = line;

                int colon = masked.IndexOf(':');
                if (colon > 0)
                {
                    string label = masked.Substring(0, colon).ToLowerInvariant();
                    if (SensitiveLabel(label))
                        masked = masked.Substring(0, colon + 1) + " ***";
                }

                if (profile.Length > 0) masked = masked.Replace(profile, "C:\\Users\\***");
                if (user.Length > 0) masked = masked.Replace(user, "***");
                if (machine.Length > 0) masked = masked.Replace(machine, "***");

                sb.Append(masked);
                if (i < lines.Length - 1) sb.Append(Environment.NewLine);
            }
            return sb.ToString();
        }

        private static bool SensitiveLabel(string label)
        {
            string[] keys = new string[]
            {
                "serial", "seri", "uuid", "guid", "mac", "asset", "ip", "ipv4", "ipv6",
                "computer name", "bilgisayar ad", "user", "kullan", "host", "anfitriyon",
                "machine", "makine"
            };
            for (int i = 0; i < keys.Length; i++)
                if (label.IndexOf(keys[i], StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        // ------------------------------------------------------------ dialog

        internal class Choice
        {
            public bool Ok;
            public string Text = "";
        }

        /// <summary>
        /// Preview before share: the user sees the exact text, toggles masking and
        /// confirms or cancels. Cancel never falls through to an unmasked save.
        /// </summary>
        public static Choice ShowShareDialog(Form owner, string title, string rawText)
        {
            Choice result = new Choice();
            using (Form d = new Form())
            {
                d.Text = title;
                d.StartPosition = FormStartPosition.CenterParent;
                d.FormBorderStyle = FormBorderStyle.Sizable;
                d.MinimizeBox = false;
                d.MaximizeBox = true;
                d.ShowInTaskbar = false;
                d.BackColor = Theme.Bg;
                d.ForeColor = Theme.Text;
                d.Font = Aero.Body;
                d.ClientSize = new Size(620, 460);
                d.MinimumSize = new Size(420, 320);
                d.KeyPreview = true;

                Label intro = new Label();
                intro.Text = L.T("This is the exact text that will be shared. Identifiers are masked by default.");
                intro.Font = Aero.Body;
                intro.ForeColor = Theme.Text;
                intro.BackColor = Theme.Bg;
                intro.UseMnemonic = false;
                intro.SetBounds(14, 12, ClientSizeSafe(d) - 28, 34);
                intro.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                d.Controls.Add(intro);

                CheckBox chk = new CheckBox();
                chk.Text = L.T("Mask identifiers (computer, user, serial numbers, addresses)");
                chk.Font = Aero.Body;
                chk.ForeColor = Theme.Text;
                chk.BackColor = Theme.Bg;
                chk.Checked = true;
                chk.SetBounds(14, 48, 460, 22);
                chk.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                d.Controls.Add(chk);

                TextBox box = new TextBox();
                box.Multiline = true;
                box.ReadOnly = true;
                box.ScrollBars = ScrollBars.Both;
                box.WordWrap = false;
                box.Font = Aero.Mono;
                box.BackColor = Aero.Field;
                box.ForeColor = Theme.Text;
                box.BorderStyle = BorderStyle.FixedSingle;
                box.SetBounds(14, 76, ClientSizeSafe(d) - 28, 300);
                box.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                d.Controls.Add(box);

                EventHandler refresh = delegate
                {
                    box.Text = chk.Checked ? Scrub(rawText) : rawText;
                };
                chk.CheckedChanged += refresh;
                refresh(null, EventArgs.Empty);

                int y = d.ClientSize.Height - 12 - Aero.ButtonH;
                AeroButton ok = new AeroButton(L.T("Continue"));
                ok.Size = new Size(96, Aero.ButtonH);
                ok.SurroundColor = Theme.Bg;
                ok.Default = true;
                ok.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                ok.SetBounds(d.ClientSize.Width - 14 - 96, y, 96, Aero.ButtonH);
                ok.Click += delegate
                {
                    result.Ok = true;
                    result.Text = box.Text;
                    d.DialogResult = DialogResult.OK;
                    d.Close();
                };
                d.Controls.Add(ok);

                AeroButton cancel = new AeroButton(L.T("Cancel"));
                cancel.Size = new Size(90, Aero.ButtonH);
                cancel.SurroundColor = Theme.Bg;
                cancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                cancel.SetBounds(d.ClientSize.Width - 14 - 96 - 8 - 90, y, 90, Aero.ButtonH);
                cancel.Click += delegate
                {
                    result.Ok = false;
                    d.DialogResult = DialogResult.Cancel;
                    d.Close();
                };
                d.Controls.Add(cancel);

                d.KeyDown += delegate(object s, KeyEventArgs e)
                {
                    if (e.KeyCode == Keys.Escape)
                    {
                        result.Ok = false;
                        d.DialogResult = DialogResult.Cancel;
                        d.Close();
                        e.Handled = true;
                    }
                };

                if (owner != null) d.ShowDialog(owner);
                else d.ShowDialog();
            }
            return result;
        }

        private static int ClientSizeSafe(Form d)
        {
            return Math.Max(320, d.ClientSize.Width);
        }
    }
}