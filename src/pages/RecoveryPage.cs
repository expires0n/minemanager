using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MineManager
{
    /// <summary>
    /// Recovery: runs the documented Windows repair commands one after another
    /// (DISM CheckHealth → ScanHealth → RestoreHealth → sfc /scannow → chkdsk /scan)
    /// from a single button, with a live, copyable log and a working stop button.
    /// </summary>
    internal class RecoveryPage : UserControl
    {
        private const int CardH = 332;
        private const int RowTop = 74;
        private const int RowH = 42;

        private AeroCommandBar _cmd;
        private Panel _host;
        private AeroCard _card, _logCard;
        private AeroConsole _log;
        private AeroButton _btnStart, _btnStop, _btnCopy, _btnReport;
        private Label _lblIntro, _lblState, _lblReport;
        private List<RecoveryStep> _steps;
        private List<RecoveryFinding> _findings;
        private string _lastReport = "";
        private Label[] _rowIndex, _rowTitle, _rowCommand, _rowState;
        private ToolTip _tip;

        private readonly StringBuilder _logText = new StringBuilder();
        private CancelFlag _cancel;
        private Timer _timer;
        private DateTime _startedUtc;
        private DateTime _lastProgress;
        private volatile int _current = -1;
        private volatile bool _busy;
        private bool _reboot;

        public RecoveryPage()
        {
            BackColor = Theme.Bg;
            DoubleBuffered = true;
            _steps = RecoveryTools.Steps();

            _cmd = new AeroCommandBar();
            _cmd.Dock = DockStyle.Top;
            _cmd.SetTitle(L.T("Recovery"), L.T("System file scan & repair · one button"));
            Controls.Add(_cmd);

            _btnReport = Ui.Outline(L.T("Report"), 82);
            _btnReport.Enabled = false;
            _btnReport.Click += delegate { ShowReport(); };
            _cmd.AddButton(_btnReport);

            _btnCopy = Ui.Outline(L.T("Copy log"), 104);
            _btnCopy.Click += delegate { CopyLog(); };
            _cmd.AddButton(_btnCopy);

            _host = new Panel();
            _host.Dock = DockStyle.Fill;
            _host.BackColor = Theme.Bg;
            Controls.Add(_host);

            _card = new AeroCard();
            _card.Caption = L.T("Recovery steps");
            _card.CaptionHint = "DISM · SFC · CHKDSK";
            _host.Controls.Add(_card);

            _lblIntro = Ui.Lbl(L.T("DISM repairs the Windows image; SFC repairs protected system files; CHKDSK scans the system drive. This can take 30–60 minutes."),
                Aero.Small, Theme.Dim);
            _lblIntro.AutoSize = false;
            _lblIntro.UseMnemonic = false;
            _card.Controls.Add(_lblIntro);

            _rowIndex = new Label[_steps.Count];
            _rowTitle = new Label[_steps.Count];
            _rowCommand = new Label[_steps.Count];
            _rowState = new Label[_steps.Count];
            _tip = new ToolTip();
            for (int i = 0; i < _steps.Count; i++)
            {
                _rowIndex[i] = Ui.Lbl((i + 1).ToString(), Aero.BodySemibold, Theme.Dim);
                _rowIndex[i].AutoSize = false;
                _rowIndex[i].TextAlign = ContentAlignment.MiddleLeft;
                _card.Controls.Add(_rowIndex[i]);

                _rowTitle[i] = Ui.Lbl(L.T(_steps[i].Name), Aero.Body, Theme.Text);
                _rowTitle[i].AutoSize = false;
                _rowTitle[i].UseMnemonic = false;
                _rowTitle[i].AutoEllipsis = true;
                _rowTitle[i].TextAlign = ContentAlignment.MiddleLeft;
                _card.Controls.Add(_rowTitle[i]);

                _rowCommand[i] = Ui.Lbl(_steps[i].Command, Aero.Mono, Theme.Dim);
                _rowCommand[i].AutoSize = false;
                _rowCommand[i].UseMnemonic = false;
                _rowCommand[i].AutoEllipsis = true;
                _rowCommand[i].TextAlign = ContentAlignment.MiddleLeft;
                _card.Controls.Add(_rowCommand[i]);

                _rowState[i] = Ui.Lbl(L.T("Waiting"), Aero.Small, Theme.Dim);
                _rowState[i].AutoSize = false;
                _rowState[i].UseMnemonic = false;
                _rowState[i].AutoEllipsis = true;
                _rowState[i].TextAlign = ContentAlignment.MiddleRight;
                _card.Controls.Add(_rowState[i]);

                _tip.SetToolTip(_rowTitle[i], L.T(_steps[i].Name) + " — " + _steps[i].Command);
                _tip.SetToolTip(_rowCommand[i], _steps[i].Command);
            }

            _btnStart = Ui.Btn(L.T("Start all"), 138);
            _btnStart.Click += delegate { StartAll(); };
            _card.Controls.Add(_btnStart);

            _btnStop = Ui.Outline(L.T("Stop"), 82);
            _btnStop.Enabled = false;
            _btnStop.Click += delegate { StopAll(); };
            _card.Controls.Add(_btnStop);

            _lblState = Ui.Lbl(L.T("Ready"), Aero.Small, Theme.Dim);
            _lblState.AutoSize = false;
            _lblState.UseMnemonic = false;
            _lblState.AutoEllipsis = true;
            _lblState.TextAlign = ContentAlignment.MiddleLeft;
            _card.Controls.Add(_lblState);

            _logCard = new AeroCard();
            _logCard.Caption = L.T("Recovery log");
            _logCard.CaptionHint = L.T("live · last 300 lines");
            _host.Controls.Add(_logCard);

            _lblReport = Ui.Lbl(L.T("No run yet — start the scan to see the verdict for each step."),
                Aero.SmallSemibold, Theme.Dim);
            _lblReport.AutoSize = false;
            _lblReport.AutoEllipsis = true;
            _lblReport.UseMnemonic = false;
            _lblReport.TextAlign = ContentAlignment.MiddleLeft;
            _logCard.Controls.Add(_lblReport);

            _log = new AeroConsole();
            _logCard.Controls.Add(_log);

            _timer = new Timer();
            _timer.Interval = 1000;
            _timer.Tick += delegate { if (_busy) RefreshState(); };

            _host.Resize += delegate { LayoutPage(); };
            LayoutPage();

            AppendLine(L.T("Commands: Microsoft Learn — DISM /Cleanup-Image, sfc /scannow, chkdsk /scan."));
        }

        public void OnShown()
        {
            if (!_busy) LayoutPage();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            LayoutPage();
        }

        // --------------------------------------------------------------- layout

        private void LayoutPage()
        {
            if (_host == null || _card == null || _logCard == null) return;
            int w = _host.ClientSize.Width;
            int h = _host.ClientSize.Height;
            if (w < 240 || h < 200) return;

            _card.SetBounds(0, 6, w, CardH);
            int logTop = 6 + CardH + 8;
            _logCard.SetBounds(0, logTop, w, Math.Max(120, h - logTop));

            int cw = _card.ClientSize.Width;
            int statusW = Math.Min(112, Math.Max(64, cw / 5));
            int statusX = cw - 14 - statusW;
            int textW = Math.Max(60, statusX - 40 - 8);

            _lblIntro.SetBounds(14, 30, cw - 28, 40);
            for (int i = 0; i < _steps.Count; i++)
            {
                int y = RowTop + i * RowH;
                _rowIndex[i].SetBounds(14, y + 1, 22, 18);
                _rowTitle[i].SetBounds(40, y, textW, 18);
                _rowCommand[i].SetBounds(40, y + 18, textW, 16);
                _rowState[i].SetBounds(statusX, y, statusW, 18);
            }

            int by = _card.ClientSize.Height - 33;
            _btnStart.SetBounds(14, by, 138, Aero.ButtonH);
            _btnStop.SetBounds(158, by, 82, Aero.ButtonH);
            _lblState.SetBounds(250, by, Math.Max(40, cw - 264), Aero.ButtonH);
            _lblState.Visible = cw >= 560;
            _card.CaptionHint = cw < 560 ? _lblState.Text : "DISM · SFC · CHKDSK";

            int lw = _logCard.ClientSize.Width;
            int lh = _logCard.ClientSize.Height;
            if (lw > 20 && lh > 30)
            {
                _lblReport.SetBounds(12, 30, lw - 24, 18);
                _log.SetBounds(12, 52, lw - 24, Math.Max(44, lh - 64));
            }
        }

        // --------------------------------------------------------------- running

        private void StartAll()
        {
            if (_busy) return;
            if (!ProcRunner.IsElevated())
            {
                AeroDialog.Info(FindForm(), L.T("Administrator required"),
                    L.T("Administrator rights are required for these commands."), "warn");
                return;
            }
            if (!AeroDialog.Confirm(FindForm(), L.T("Start the recovery scan?"),
                L.T("DISM, SFC and CHKDSK run one after another with administrator rights. Save your work and close other installers first. The app stays open; a restart may be needed at the end."),
                L.T("Start"), L.T("Cancel"), "warn", false))
                return;
            if (!ProcRunner.TryBeginJob())
            {
                Shell.Toast(L.T("Another task is running. Wait for it to finish."), Aero.Warning);
                return;
            }

            _busy = true;
            _cancel = new CancelFlag();
            _reboot = false;
            _findings = new List<RecoveryFinding>();
            _lastReport = "";
            _btnReport.Enabled = false;
            _lblReport.Text = L.T("No run yet — start the scan to see the verdict for each step.");
            _lblReport.ForeColor = Theme.Dim;
            _log.Clear();
            _logText.Length = 0;
            _lastProgress = DateTime.MinValue;
            _startedUtc = DateTime.UtcNow;
            _current = -1;
            ResetRows();
            SetState(L.T("Ready"));
            _lblState.ForeColor = Theme.Dim;
            _btnStart.Enabled = false;
            _btnStop.Enabled = true;
            _timer.Enabled = true;

            RecoveryStep[] steps = _steps.ToArray();
            CancelFlag cancel = _cancel;
            DateTime started = _startedUtc;
            Task.Run(delegate { RunSequence(steps, cancel, started); });
        }

        private void StopAll()
        {
            if (!_busy || _cancel == null) return;
            _cancel.Cancelled = true;
            _btnStop.Enabled = false;
            SetState(L.T("Stopping…"));
        }

        private void RunSequence(RecoveryStep[] steps, CancelFlag cancel, DateTime started)
        {
            int done = 0, failed = 0;
            bool cancelled = false;
            List<RecoveryFinding> findings = new List<RecoveryFinding>();
            try
            {
                for (int i = 0; i < steps.Length; i++)
                {
                    if (cancel.Cancelled) break;
                    int idx = i;
                    _current = i;
                    SafeUi(delegate
                    {
                        MarkRow(idx, L.T("Running…"), Theme.Text);
                        RefreshState();
                        AppendLine("");
                        AppendLine(L.F("▶ Step {0}/{1} · {2}", idx + 1, steps.Length, L.T(steps[idx].Name)));
                        AppendLine("> " + steps[idx].Command);
                    });

                    RunResult r = ProcRunner.RunEncoded(steps[idx].Exe, steps[idx].Args, steps[idx].TimeoutSec,
                        LogFromWorker, cancel, RecoveryTools.OutputEncoding(steps[idx].Exe));

                    RecoveryFinding f = RecoveryReport.Parse(idx, steps[idx], r);
                    findings.Add(f);
                    if (f.Ok) done++; else failed++;
                    if (r.NeedsReboot) _reboot = true;

                    SafeUi(delegate
                    {
                        MarkRow(idx, L.T(f.StateKey), TonFor(f));
                        AppendLine("  → " + f.Detail);
                        if (f.Evidence.Length > 0 && !f.Ok)
                            AppendLine("    " + L.F("Windows: {0}", f.Evidence));
                    });
                    if (r.Cancelled) break;
                }
            }
            catch (Exception ex)
            {
                SafeUi(delegate { AppendLine(L.F("Error: {0}", ex.Message)); });
            }
            finally
            {
                cancelled = cancel.Cancelled;
                ProcRunner.EndJob();
                int d = done, f = failed;
                List<RecoveryFinding> report = findings;
                SafeUi(delegate { FinishUi(d, f, cancelled, started, report); });
            }
        }

        private static Color TonFor(RecoveryFinding f)
        {
            switch (f.Verdict)
            {
                case RecoveryVerdict.Clean: return Aero.SuccessText;
                case RecoveryVerdict.Repaired: return Aero.Dark ? Aero.Blue300 : Aero.Blue600;
                case RecoveryVerdict.Issues: return Aero.WarningText;
                case RecoveryVerdict.Failed: return Aero.DangerText;
                default: return Theme.Dim;
            }
        }

        private void FinishUi(int done, int failed, bool cancelled, DateTime started, List<RecoveryFinding> findings)
        {
            _busy = false;
            _current = -1;
            _timer.Enabled = false;
            _btnStart.Enabled = true;
            _btnStop.Enabled = false;
            _btnReport.Enabled = true;
            if (cancelled)
            {
                for (int i = findings.Count; i < _steps.Count; i++)
                    MarkRow(i, L.T("Stopped"), Theme.Dim);
            }
            Shell.ClearProgress();

            string took = Elapsed();
            _lastReport = RecoveryReport.BuildReport(findings, cancelled, _reboot, took);
            string headline = RecoveryReport.Headline(findings, cancelled, took);
            _lblReport.Text = headline;

            bool warn = cancelled || failed > 0;
            bool repaired = false;
            for (int i = 0; i < findings.Count; i++)
            {
                if (findings[i].Verdict == RecoveryVerdict.Repaired) repaired = true;
                if (findings[i].Verdict != RecoveryVerdict.Clean &&
                    findings[i].Verdict != RecoveryVerdict.Repaired) warn = true;
            }
            _lblReport.ForeColor = warn ? Aero.WarningText :
                (repaired ? (Aero.Dark ? Aero.Blue300 : Aero.Blue600) : Aero.SuccessText);
            SetState(headline);
            _lblState.ForeColor = warn ? Aero.WarningText : Aero.SuccessText;

            AppendLine("");
            AppendLine("=== " + L.T("Recovery report") + " ===");
            AppendLine(_lastReport);

            if (cancelled) Shell.Toast(L.T("Recovery stopped"), Aero.Warning);
            else if (warn) Shell.Toast(L.T("Recovery finished with problems"), Aero.Warning);
            else Shell.Toast(L.T("Recovery finished"), Aero.Success);

            if (Visible && FindForm() != null)
                AeroDialog.Info(FindForm(), L.T("Recovery report"), _lastReport,
                    warn ? "warn" : "shield");
        }

        private void ShowReport()
        {
            if (_lastReport.Length == 0)
            {
                Shell.Toast(L.T("No report yet."), Aero.Warning);
                return;
            }
            AeroDialog.Info(FindForm(), L.T("Recovery report"), _lastReport, "warn");
        }

        private void ResetRows()
        {
            for (int i = 0; i < _steps.Count; i++)
            {
                _rowIndex[i].ForeColor = Theme.Dim;
                MarkRow(i, L.T("Waiting"), Theme.Dim);
            }
        }

        private void SetState(string text)
        {
            _lblState.Text = text;
            if (_card != null && _card.ClientSize.Width < 560) _card.CaptionHint = text;
            if (_card != null) _card.Invalidate();
        }

        private void MarkRow(int i, string state, Color color)
        {
            if (i < 0 || i >= _rowState.Length) return;
            _rowState[i].Text = state;
            _rowState[i].ForeColor = color;
        }

        private void RefreshState()
        {
            if (!_busy || _current < 0) return;
            SetState(L.F("Step {0}/{1} · {2}", _current + 1, _steps.Count, Elapsed()));
            Shell.SetProgress((_current + 1) / (double)_steps.Count);
        }

        private string Elapsed()
        {
            TimeSpan t = DateTime.UtcNow - _startedUtc;
            if (t < TimeSpan.Zero) t = TimeSpan.Zero;
            return ((int)t.TotalMinutes).ToString("00") + ":" + t.Seconds.ToString("00");
        }

        // --------------------------------------------------------------- logging

        private void LogFromWorker(string text)
        {
            if (text == null) return;
            string line = text.TrimEnd();
            if (line.Length == 0) return;
            if (IsProgressLine(line))
            {
                DateTime now = DateTime.UtcNow;
                if ((now - _lastProgress).TotalMilliseconds < 700) return;
                _lastProgress = now;
            }
            SafeUi(delegate { AppendLine(line); });
        }

        /// <summary>DISM/SFC print a percentage line per tick; keep the log readable.</summary>
        private static bool IsProgressLine(string line)
        {
            return line.Length < 90 && line.IndexOf('%') >= 0;
        }

        private void AppendLine(string line)
        {
            _log.Append(line);
            if (_logText.Length < 200000) _logText.AppendLine(line);
        }

        private void CopyLog()
        {
            if (_logText.Length == 0)
            {
                Shell.Toast(L.T("Nothing to copy yet."), Aero.Warning);
                return;
            }
            try
            {
                Clipboard.SetText(_logText.ToString());
                Shell.Toast(L.T("Log copied to clipboard."), Aero.Success);
            }
            catch (Exception ex)
            {
                Shell.Toast(L.F("Copy failed: {0}", ex.Message), Aero.Danger);
            }
        }

        private void SafeUi(Action a)
        {
            try
            {
                if (IsDisposed || Disposing || !IsHandleCreated) return;
                BeginInvoke(a);
            }
            catch { }
        }
    }
}
