using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SieveVisualizer
{
    public partial class MainForm : Form
    {
        // ===== Recording (auto) =====
        private bool isRecording = false;
        private int frameIndex = 0;
        private string frameDir = "";

        // ถ้า DrawToBitmap ไม่เสถียรในเครื่องคุณ ให้เปลี่ยนเป็น true
        // true  = ใช้ CopyFromScreen (ได้ครบแน่ แต่มี cursor)
        // false = ใช้ DrawToBitmap (ภาพสะอาด ไม่มี cursor)
        private const bool FORCE_SCREEN_CAPTURE = false;

        // ===== Visualization state =====
        private enum CellState { Unknown, Prime, Composite }

        private int N = 200;
        private CellState[] state = Array.Empty<CellState>();
        private int[] markedBy = Array.Empty<int>(); // 0 = not marked, else base prime p that marked it

        // Sieve step machine
        private IEnumerator<Action>? stepper;
        private bool isRunning = false;

        // Current highlight
        private int currentP = 0;
        private int currentK = 0; // currently marking k

        // ===== UI =====
        private readonly DoubleBufferedPanel canvas = new DoubleBufferedPanel();

        private readonly Button btnStartPause = new Button();
        private readonly Button btnNext = new Button();
        private readonly Button btnReset = new Button();

        private readonly NumericUpDown nudN = new NumericUpDown();
        private readonly TrackBar tbSpeed = new TrackBar();
        private readonly Label lblInfo = new Label();
        private readonly Label lblSpeed = new Label();

        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

        // ===== Layout params =====
        private const int TopBarHeight = 92;
        private const int MarginAll = 12;

        // Cell drawing
        private int cellSize = 26;
        private int cellGap = 4;
        private readonly Font cellFont = new Font("Segoe UI", 9f, FontStyle.Regular);
        private readonly Font smallFont = new Font("Segoe UI", 9f, FontStyle.Regular);
        private readonly Font markedByFont = new Font("Segoe UI", 7.5f, FontStyle.Regular);

        public MainForm()
        {
            Text = "Sieve of Eratosthenes Visualizer (WinForms)";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(860, 560);

            // ลด flicker ทั้งฟอร์ม
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
            UpdateStyles();

            // Canvas panel
            canvas.Dock = DockStyle.Fill;
            canvas.BackColor = Color.White;
            canvas.Paint += Canvas_Paint;
            canvas.Resize += (_, __) => canvas.Invalidate();
            Controls.Add(canvas);

            // Top controls container (using a Panel)
            var top = new Panel
            {
                Dock = DockStyle.Top,
                Height = TopBarHeight,
                BackColor = SystemColors.Control
            };
            Controls.Add(top);

            // Buttons
            btnStartPause.Text = "Start";
            btnStartPause.Width = 110;
            btnStartPause.Height = 34;
            btnStartPause.Click += (_, __) => ToggleRun();

            btnNext.Text = "Next Step";
            btnNext.Width = 110;
            btnNext.Height = 34;
            btnNext.Click += (_, __) =>
            {
                // ถ้ากด Next Step แบบ manual และอยากให้ capture ด้วย
                // ให้เริ่ม recording แบบเงียบ ๆ ครั้งแรกที่กด
                if (!isRecording) StartRecordingSilently();
                StepOnce();
            };

            btnReset.Text = "Reset";
            btnReset.Width = 110;
            btnReset.Height = 34;
            btnReset.Click += (_, __) => ResetAll();

            // N input
            nudN.Minimum = 10;
            nudN.Maximum = 5000;
            nudN.Value = N;
            nudN.Width = 120;
            nudN.ValueChanged += (_, __) =>
            {
                N = (int)nudN.Value;
                ResetAll();
            };

            // Speed
            tbSpeed.Minimum = 1;   // slow
            tbSpeed.Maximum = 20;  // fast
            tbSpeed.Value = 10;
            tbSpeed.Width = 240;
            tbSpeed.TickFrequency = 1;
            tbSpeed.ValueChanged += (_, __) => UpdateTimerInterval();

            lblSpeed.AutoSize = true;
            lblSpeed.Text = "Speed";
            lblSpeed.Font = smallFont;

            // Info label
            lblInfo.AutoSize = false;
            lblInfo.TextAlign = ContentAlignment.MiddleLeft;
            lblInfo.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblInfo.Height = 34;

            // Place controls manually
            top.Controls.Add(btnStartPause);
            top.Controls.Add(btnNext);
            top.Controls.Add(btnReset);
            top.Controls.Add(new Label { Text = "N:", AutoSize = true, Font = smallFont });
            top.Controls.Add(nudN);
            top.Controls.Add(lblSpeed);
            top.Controls.Add(tbSpeed);
            top.Controls.Add(lblInfo);

            top.Layout += (_, __) =>
            {
                int x = MarginAll;
                int y = 10;

                btnStartPause.Location = new Point(x, y);
                x += btnStartPause.Width + 8;

                btnNext.Location = new Point(x, y);
                x += btnNext.Width + 8;

                btnReset.Location = new Point(x, y);
                x += btnReset.Width + 18;

                var lblN = (Label)top.Controls.OfType<Label>().First(l => l.Text == "N:");
                lblN.Location = new Point(x, y + 8);
                x += lblN.Width + 6;

                nudN.Location = new Point(x, y + 4);
                x += nudN.Width + 18;

                lblSpeed.Location = new Point(x, y + 8);
                x += lblSpeed.Width + 8;

                tbSpeed.Location = new Point(x, y);

                lblInfo.Location = new Point(MarginAll, 50);
                lblInfo.Width = top.Width - 2 * MarginAll;
            };

            // Timer
            timer.Tick += (_, __) => StepOnce();
            UpdateTimerInterval();

            // Init
            ResetAll();
        }

        private void UpdateTimerInterval()
        {
            // Speed mapping: higher = faster (lower interval)
            // 1 -> 900ms, 20 -> 50ms
            int v = tbSpeed.Value;
            int interval = (int)Math.Round(900 - (v - 1) * (850.0 / 19.0));
            timer.Interval = Math.Max(30, interval);
        }

        private void ResetAll()
        {
            isRunning = false;
            timer.Stop();
            btnStartPause.Text = "Start";

            // reset recording state
            StopRecordingSilently();

            state = new CellState[N + 1];
            markedBy = new int[N + 1];

            for (int i = 0; i <= N; i++)
            {
                state[i] = CellState.Unknown;
                markedBy[i] = 0;
            }

            currentP = 0;
            currentK = 0;

            stepper = BuildStepper();
            UpdateInfo("Ready. Click Start or Next Step.");
            canvas.Invalidate();
        }

        private void ToggleRun()
        {
            if (isRunning)
            {
                // pause
                isRunning = false;
                timer.Stop();
                btnStartPause.Text = "Start";
                UpdateInfo("Paused.");
            }
            else
            {
                // start (auto-record)
                //if (!isRecording) StartRecording(); // <<< เริ่ม capture เมื่อกด Start
                isRunning = true;
                timer.Start();
                btnStartPause.Text = "Pause";
                UpdateInfo("Running...");
            }
        }

        private void StepOnce()
        {
            if (stepper == null)
                return;

            bool ok = stepper.MoveNext();
            if (!ok)
            {
                FinalizePrimes();
                canvas.Invalidate();
                Application.DoEvents(); // ให้ภาพอัปเดตก่อน capture

                CaptureFrame(); // capture frame สุดท้าย
                StopRecording(); // <<< หยุด capture เมื่อจบ

                isRunning = false;
                timer.Stop();
                btnStartPause.Text = "Start";
                UpdateInfo("Done. All remaining unmarked numbers are Prime.");
                return;
            }

            stepper.Current?.Invoke();
            canvas.Invalidate();
            Application.DoEvents(); // ให้ Paint ทำงานก่อน capture

            CaptureFrame(); // <<< capture ทุก step (ทั้งฟอร์ม)
        }

        private void FinalizePrimes()
        {
            for (int i = 2; i <= N; i++)
            {
                if (state[i] == CellState.Unknown)
                    state[i] = CellState.Prime;
            }
            currentP = 0;
            currentK = 0;
        }

        private IEnumerator<Action> BuildStepper()
        {
            int p = 2;

            while (p * p <= N)
            {
                while (p <= N && state[p] == CellState.Composite)
                    p++;

                if (p * p > N) break;

                int baseP = p;

                yield return () =>
                {
                    currentP = baseP;
                    currentK = 0;
                    if (state[baseP] == CellState.Unknown)
                        state[baseP] = CellState.Prime;

                    UpdateInfo($"Base p = {baseP}. Marking multiples from {baseP}² = {baseP * baseP}.");
                };

                for (int k = baseP * baseP; k <= N; k += baseP)
                {
                    int kk = k;
                    yield return () =>
                    {
                        currentP = baseP;
                        currentK = kk;

                        if (state[kk] != CellState.Composite)
                        {
                            state[kk] = CellState.Composite;
                            markedBy[kk] = baseP;
                        }

                        UpdateInfo($"p = {baseP} → mark {kk} as composite" +
                                   (markedBy[kk] == baseP ? $" (by {baseP})" : ""));
                    };
                }

                p = baseP + 1;
            }

            yield return () =>
            {
                currentP = 0;
                currentK = 0;
                UpdateInfo("Sieve loop finished (p² > N). Finalizing primes...");
            };
        }

        private void UpdateInfo(string text)
        {
            int primes = 0;
            for (int i = 2; i <= N; i++)
                if (state[i] == CellState.Prime) primes++;

            lblInfo.Text = $"{text}    |   N={N}   |   primes found so far: {primes}";
        }

        // ===== Drawing =====
        private void Canvas_Paint(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.Clear(Color.White);

            if (N < 2) return;

            int availableW = canvas.ClientSize.Width - 2 * MarginAll;
            int availableH = canvas.ClientSize.Height - 2 * MarginAll;

            int fullCell = cellSize + cellGap;
            int cols = Math.Max(5, availableW / fullCell);
            int rowsNeeded = (int)Math.Ceiling((N - 1) / (double)cols);

            if (rowsNeeded * fullCell > availableH && cellSize > 14)
            {
                cellSize = Math.Max(14, cellSize - 2);
                fullCell = cellSize + cellGap;
                cols = Math.Max(5, availableW / fullCell);
            }

            int x0 = MarginAll;
            int y0 = MarginAll;

            using var penBorder = new Pen(Color.FromArgb(30, 0, 0, 0), 1);
            using var brushUnknown = new SolidBrush(Color.FromArgb(245, 245, 245));
            using var brushPrime = new SolidBrush(Color.FromArgb(210, 255, 210));
            using var brushComposite = new SolidBrush(Color.FromArgb(255, 230, 200)); // <<< ส้มอ่อน

            using var brushHighlightP = new SolidBrush(Color.FromArgb(255, 255, 0));
            using var brushHighlightK = new SolidBrush(Color.FromArgb(255, 200, 200));

            using var textBrush = new SolidBrush(Color.FromArgb(30, 30, 30));
            using var smallBrush = new SolidBrush(Color.FromArgb(110, 110, 110));

            StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            int idx = 0;
            for (int n = 2; n <= N; n++, idx++)
            {
                int r = idx / cols;
                int c = idx % cols;

                int x = x0 + c * fullCell;
                int y = y0 + r * fullCell;

                var rect = new Rectangle(x, y, cellSize, cellSize);

                Brush fill = state[n] switch
                {
                    CellState.Prime => brushPrime,
                    CellState.Composite => brushComposite,
                    _ => brushUnknown
                };

                bool isP = (n == currentP && currentP >= 2);
                bool isK = (n == currentK && currentK >= 2);

                if (isK) fill = brushHighlightK;
                else if (isP) fill = brushHighlightP;

                e.Graphics.FillRectangle(fill, rect);
                e.Graphics.DrawRectangle(penBorder, rect);

                e.Graphics.DrawString(n.ToString(), cellFont, textBrush, rect, sf);

                if (state[n] == CellState.Composite && markedBy[n] != 0)
                {
                    string by = $"×{markedBy[n]}";
                    var smallRect = new Rectangle(x, y + cellSize - 12, cellSize, 12);
                    e.Graphics.DrawString(by, markedByFont, smallBrush, smallRect, sf);
                }
            }

            DrawLegend(e.Graphics);
        }

        private void DrawLegend(Graphics g)
        {
            int x = canvas.ClientSize.Width - 260;
            int y = canvas.ClientSize.Height - 78;
            if (x < MarginAll) x = MarginAll;
            if (y < MarginAll) y = MarginAll;

            var box = new Rectangle(x, y, 240, 62);
            using var bg = new SolidBrush(Color.FromArgb(245, 245, 245));
            using var pen = new Pen(Color.FromArgb(40, 0, 0, 0));

            g.FillRectangle(bg, box);
            g.DrawRectangle(pen, box);

            using var f = new Font("Segoe UI", 9f, FontStyle.Regular);
            using var brush = new SolidBrush(Color.FromArgb(40, 40, 40));

            g.DrawString("Legend:", f, brush, x + 10, y + 8);

            DrawLegendItem(g, x + 10, y + 28, Color.FromArgb(245, 245, 245), "Unknown");
            DrawLegendItem(g, x + 92, y + 28, Color.FromArgb(210, 255, 210), "Prime");
            DrawLegendItem(g, x + 160, y + 28, Color.FromArgb(255, 230, 200), "Composite");
        }

        private void DrawLegendItem(Graphics g, int x, int y, Color color, string text)
        {
            var rect = new Rectangle(x, y, 16, 16);
            using var b = new SolidBrush(color);
            using var pen = new Pen(Color.FromArgb(40, 0, 0, 0));
            using var f = new Font("Segoe UI", 9f, FontStyle.Regular);
            using var brush = new SolidBrush(Color.FromArgb(60, 60, 60));

            g.FillRectangle(b, rect);
            g.DrawRectangle(pen, rect);
            g.DrawString(text, f, brush, x + 20, y - 1);
        }

        // ===== Recording helpers =====

        private void StartRecording()
        {
            frameDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "frames");
            Directory.CreateDirectory(frameDir);

            foreach (var f in Directory.GetFiles(frameDir, "*.png"))
                File.Delete(f);

            frameIndex = 0;
            isRecording = true;

            // capture frame 0 (ตอนเริ่มกด Start)
            CaptureFrame();
            UpdateInfo("Recording... (frames saved to /frames)");
        }

        private void StopRecording()
        {
            if (!isRecording) return;
            isRecording = false;
            UpdateInfo($"Recording stopped ({frameIndex} frames). Use ffmpeg to export MP4.");
        }

        private void StartRecordingSilently()
        {
            // ใช้ตอนกด Next Step แบบ manual
            frameDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "frames");
            Directory.CreateDirectory(frameDir);

            foreach (var f in Directory.GetFiles(frameDir, "*.png"))
                File.Delete(f);

            frameIndex = 0;
            isRecording = true;
            CaptureFrame();
        }

        private void StopRecordingSilently()
        {
            isRecording = false;
            frameIndex = 0;
        }

        private void CaptureFrame()
        {
            if (!isRecording) return;

            // ทำให้แน่ใจว่าฟอร์มถูกวาดล่าสุด
            Refresh();

            string path = Path.Combine(frameDir, $"frame_{frameIndex:D5}.png");

            if (!FORCE_SCREEN_CAPTURE)
            {
                try
                {
                    using Bitmap bmp = new Bitmap(Width, Height);
                    DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
                    bmp.Save(path, ImageFormat.Png);
                    frameIndex++;
                    return;
                }
                catch
                {
                    // fallback to screen capture below
                }
            }

            // Fallback: screen capture (ได้ครบแน่)
            Rectangle bounds = Bounds; // includes borders/titlebar (ตามที่เห็นบนจอ)
            using Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height);
            using (Graphics g = Graphics.FromImage(screenshot))
            {
                g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
            }
            screenshot.Save(path, ImageFormat.Png);
            frameIndex++;
        }
    }
}
