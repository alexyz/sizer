using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConsoleApp3 {

    public class SizerForm : Form
    {

        public const int SW_SHOWMAXIMIZED = 3, SW_SHOWMINIMIZED = 2, SW_SHOWNORMAL = 1;

        [System.STAThreadAttribute()]
        public static void Main(string[] args) {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SizerForm());
        }

        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowplacement
        /// </summary>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos
        /// </summary>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, int hWnd2, int x, int y, int cx, int cy, uint flags);

        private Button queryProcessButton = new Button();
        private Button updateProcessButton = new Button();
        private ComboBox processCombo = new ComboBox();
        private Label processLabel = new Label();
        private ComboBox processResCombo = new ComboBox();
        private Button applyProcessButton = new Button();

        public SizerForm() {
            Resize += FormResized;

            // alternatives are Panel and TableLayoutPanel
            FlowLayoutPanel p1 = new FlowLayoutPanel();
            p1.BorderStyle = BorderStyle.FixedSingle;
            //p1.Dock = DockStyle.Top;
            //p1.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            //p1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            p1.Location = new Point(16, 16);
            p1.Size = new Size(640 - 48, 128);
            foreach (R r in new[] { R.R1, R.R2, R.R3, R.R4, R.R5 }) {
                p1.Controls.Add(CreateResButton(r));
            }

            queryProcessButton.Text = "Query";
            queryProcessButton.Click += new EventHandler(QueryProcessClicked);

            processCombo.Size = new Size(384, 32);
            processCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            processCombo.SelectedIndexChanged += new EventHandler(ProcessChanged);
            processCombo.Anchor = AnchorStyles.Left;

            updateProcessButton.Text = "Update";
            updateProcessButton.Click += new EventHandler(UpdateProcessClicked);

            //processLabel.Size = new Size(384, 32);
            processLabel.AutoSize = true;
            processLabel.Anchor = AnchorStyles.Left;

            foreach (R r in new R[] { new R(320, 640), new R(400, 800), R.R1, R.R2, R.R3, R.R4, R.R5 }) {
                processResCombo.Items.Add(r);
            }
            processResCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            processResCombo.SelectedItem = processResCombo.Items[0];
            //processSizeCombo.Anchor = AnchorStyles.Left;

            applyProcessButton.Text = "Apply";
            applyProcessButton.Click += new EventHandler(ApplyProcessClicked);

            TableLayoutPanel p2 = new TableLayoutPanel();
            p2.ColumnCount = 2;
            p2.RowCount = 3;
            p2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            p2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80f));
            p2.BorderStyle = BorderStyle.FixedSingle;
            p2.Location = new Point(16, 160);
            p2.Size = new Size(640 - 48, 128);
            p2.Controls.Add(queryProcessButton);
            p2.Controls.Add(processCombo);
            p2.Controls.Add(updateProcessButton);
            p2.Controls.Add(processLabel);
            p2.Controls.Add(applyProcessButton);
            p2.Controls.Add(processResCombo);
            
            SuspendLayout();
            Size = new Size(1024, 768);
            Controls.Add(p1);
            Controls.Add(p2);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Text = "Sizer";
            StartPosition = FormStartPosition.CenterScreen;
            ResumeLayout(false);
        }

        private void ApplyProcessClicked(object s, EventArgs e) {
            PV ps = (PV)processCombo.SelectedItem;
            R res = (R)processResCombo.SelectedItem;
            if (ps != null) {
                IntPtr h = ps.p.MainWindowHandle;
                if (h != IntPtr.Zero) {
                    WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
                    wp.length = Marshal.SizeOf(wp);
                    if (GetWindowPlacement(h, ref wp)) {
                        Console.WriteLine("updating size");
                        if (SetWindowPos(h, 0, wp.normPos.left, wp.normPos.top, res.x, res.y, 0)) {
                            UpdateProcessLabel();
                        } else {
                            MessageBox.Show("could not set window position!");
                        }
                    } else {
                        MessageBox.Show("could not get window placement!");
                    }
                }
            }
        }

        private void UpdateProcessClicked(object s, EventArgs e) {
            UpdateProcessLabel();
        }

        private void ProcessChanged(object s, EventArgs e) {
            UpdateProcessLabel();
        }

        private void UpdateProcessLabel() {
            PV ps = (PV)processCombo.SelectedItem;
            if (ps != null && !ps.p.HasExited) {
                IntPtr h = ps.p.MainWindowHandle;
                if (h != IntPtr.Zero) {
                    WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
                    wp.length = Marshal.SizeOf(wp);
                    if (GetWindowPlacement(h, ref wp)) {
                        processLabel.Text = wp.ToString();
                    } else {
                        processLabel.Text = "could not get window placement!";
                    }
                }
                else {
                    processLabel.Text = "no main window!";
                }
            }
            else {
                processLabel.Text = "no process!";
            }
        }

        private void QueryProcessClicked(object s, EventArgs e) {
            processLabel.Text = "";
            processCombo.BeginUpdate();
            PV prev = (PV) processCombo.SelectedItem;
            processCombo.Items.Clear();
            List<PV> list = new List<PV>();
            foreach (Process p in Process.GetProcesses()) {
                IntPtr h = p.MainWindowHandle;
                if (h != IntPtr.Zero) {
                    WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
                    wp.length = Marshal.SizeOf(wp);
                    GetWindowPlacement(h, ref wp);
                    if (wp.showCmd == SW_SHOWNORMAL) {
                        string t = p.MainWindowTitle;
                        if (t.Length == 0) {
                            t = "#" + p.ProcessName;
                        }
                        list.Add(new PV(p, t + " [" + p.Id + "]"));
                    }
                }
            }
            list.Sort();
            processCombo.Items.AddRange(list.ToArray());
            if (prev != null) {
                foreach (object i in processCombo.Items) {
                    if (((PV)i).p.Id == prev.p.Id) {
                        processCombo.SelectedItem = i;
                        break;
                    }
                }
            }
            processCombo.EndUpdate();
        }

        static string WpShowStr(WINDOWPLACEMENT wp) {
            switch (wp.showCmd) {
                case SW_SHOWMINIMIZED: return "MIN";
                case SW_SHOWMAXIMIZED: return "MAX";
                case SW_SHOWNORMAL: return "NORM";
                default: return "UNKN";
            }
        }

        private void FormResized(object s, EventArgs e) {
            Text = "Sizer " + Size.Width + "*" + Size.Height;
        }

        private Button CreateResButton(R r) {
            Button b1 = new Button();
            b1.Text = r.ToString();
            b1.Click += (s, e) => ResButtonClicked(s, e, r);
            return b1;
        }

        private void ResButtonClicked(object s, EventArgs e, R r) {
            SuspendLayout();
            Size = new Size(r.x, r.y);
            ResumeLayout(false);
        }

    }

    public class R
    {
        public static readonly R R1 = new R(640, 480);
        public static readonly R R2 = new R(800, 600);
        public static readonly R R3 = new R(1024, 768);
        public static readonly R R4 = new R(1152, 864);
        public static readonly R R5 = new R(1280, 960);
        public readonly int x, y;
        public R(int x, int y) {
            this.x = x;
            this.y = y;
        }
        public override string ToString() {
            return x + ", " + y;
        }
    }
    
    public class PV : IComparable<PV> {
        public readonly Process p;
        public readonly string v;
        public PV(Process p, string v) {
            this.p = p;
            this.v = v;
        }

        public int CompareTo(PV other)
        {
            return v.CompareTo(other.v);
        }

        public override string ToString() {
            return v;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;        // x position of upper-left corner
        public int top;         // y position of upper-left corner
        public int right;       // x position of lower-right corner
        public int bottom;      // y position of lower-right corner
        public override string ToString() {
            return "R[" + (right - left) + ", " + (bottom - top) + "]";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPLACEMENT
    {
        public int length;
        public int flags;
        public int showCmd;
        public Point minPos;
        public Point maxPos;
        public RECT normPos;
        public override string ToString() {
            return "WP[flags=" + flags + " show=" + showCmd + " min=" + minPos + " max=" + maxPos + " norm=" + normPos + "]";
        }
    }
}
