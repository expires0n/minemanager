using System;
using System.Drawing;
using System.Windows.Forms;

namespace MineManager
{
    internal class SettingsPage : UserControl
    {
        private AeroCommandBar _cmd;
        private Panel _body;
        private AeroCard _langCard, _themeCard;
        private AeroButton _btnEn, _btnTr, _btnLight, _btnDark;
        private Label _note;

        public SettingsPage()
        {
            BackColor = Theme.Bg;
            Padding = new Padding(0);
            DoubleBuffered = true;

            _cmd = new AeroCommandBar();
            _cmd.Dock = DockStyle.Top;
            _cmd.SetTitle(L.T("Settings"), L.T("Language · appearance"));
            Controls.Add(_cmd);

            _body = new Panel();
            _body.Dock = DockStyle.Fill;
            _body.BackColor = Theme.Bg;
            _body.Padding = new Padding(12, 10, 12, 10);
            Controls.Add(_body);
            _body.BringToFront();

            // ------------------------------------------------------- language
            _langCard = new AeroCard();
            _langCard.Caption = L.T("Language");
            _langCard.CaptionHint = L.T("Interface language");
            _body.Controls.Add(_langCard);

            _btnEn = new AeroButton("English");
            _btnEn.Size = new Size(88, Aero.ButtonH);
            _btnEn.Selected = !L.IsTr;
            _btnEn.SurroundColor = Theme.Surface;
            _btnEn.Click += delegate { ChooseLanguage("en"); };
            _langCard.Controls.Add(_btnEn);

            _btnTr = new AeroButton("Türkçe");
            _btnTr.Size = new Size(88, Aero.ButtonH);
            _btnTr.Selected = L.IsTr;
            _btnTr.SurroundColor = Theme.Surface;
            _btnTr.Click += delegate { ChooseLanguage("tr"); };
            _langCard.Controls.Add(_btnTr);

            // ------------------------------------------------------ appearance
            _themeCard = new AeroCard();
            _themeCard.Caption = L.T("Appearance");
            _themeCard.CaptionHint = L.T("Window and control colors");
            _body.Controls.Add(_themeCard);

            _btnLight = new AeroButton(L.T("Light"));
            _btnLight.Size = new Size(88, Aero.ButtonH);
            _btnLight.Selected = !AppSettings.Dark;
            _btnLight.SurroundColor = Theme.Surface;
            _btnLight.Click += delegate { ChooseTheme(false); };
            _themeCard.Controls.Add(_btnLight);

            _btnDark = new AeroButton(L.T("Dark"));
            _btnDark.Size = new Size(88, Aero.ButtonH);
            _btnDark.Selected = AppSettings.Dark;
            _btnDark.SurroundColor = Theme.Surface;
            _btnDark.Click += delegate { ChooseTheme(true); };
            _themeCard.Controls.Add(_btnDark);

            _note = Ui.Lbl(L.T("Changes apply immediately.") + " " +
                L.T("Settings are stored in MineManager.ini next to the application."),
                Aero.Small, Theme.Dim);
            _note.AutoSize = false;
            _note.AutoEllipsis = true;
            _note.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _body.Controls.Add(_note);

            Resize += delegate { LayoutCards(); };
            LayoutCards();
        }

        private void LayoutCards()
        {
            int w = Math.Max(200, _body.ClientSize.Width - 24);
            _langCard.SetBounds(12, 10, w, 92);
            _themeCard.SetBounds(12, 112, w, 92);
            _note.SetBounds(14, 214, Math.Max(100, w - 4), 54);
            _btnEn.Location = new Point(14, 44);
            _btnTr.Location = new Point(108, 44);
            _btnLight.Location = new Point(14, 44);
            _btnDark.Location = new Point(108, 44);
        }

        private void ChooseLanguage(string code)
        {
            if (L.Lang == code) return;
            AppSettings.SetLanguage(code);
            Reload();
        }

        private void ChooseTheme(bool dark)
        {
            if (AppSettings.Dark == dark) return;
            AppSettings.SetTheme(dark);
            Reload();
        }

        private void Reload()
        {
            MainForm m = Shell.MainForm as MainForm;
            if (m == null) return;
            try
            {
                m.BeginInvoke(new Action(delegate { m.ApplyPreferences(4); }));
            }
            catch { }
        }
    }
}
