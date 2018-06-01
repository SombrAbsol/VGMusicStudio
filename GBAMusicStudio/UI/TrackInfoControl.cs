﻿using GBAMusicStudio.Core;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace GBAMusicStudio.UI
{
    internal class TrackInfoControl : UserControl
    {
        IContainer components = null;
        readonly Color barColor = Color.FromArgb(0xA7, 0x44, 0xDD);
        readonly string noNotes = "…";
        readonly int checkboxSize = 15;

        //readonly string[] simpleNotes = { "Cn", "Cs", "Dn", "Ds", "En", "Fn", "Fs", "Gn", "Gs", "An", "As", "Bn" };
        readonly string[] simpleNotes = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        Tuple<int[], string[]> previousNotes;
        ushort tempo;
        uint[] positions;
        byte[] volumes, delays, voices, mods;
        float[] velocities, pans;
        int[] bends;
        string[] types;
        byte[][] notes;
        CheckBox[] mutes;
        CheckBox[] pianos;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        void InitializeComponent()
        {
            SuspendLayout();
            AutoScaleMode = AutoScaleMode.Font;
            DoubleBuffered = true;
            Name = "TrackInfoControl";
            Font = new Font("Microsoft Tai Le", 10.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Size = new Size(525, 675);
            Paint += TrackInfoControl_Paint;
            Resize += (o, s) => Invalidate();
            mutes = new CheckBox[17]; // Index 16 is master
            pianos = new CheckBox[16];
            for (int i = 0; i < 17; i++)
            {
                if (i < 16)
                {
                    pianos[i] = new CheckBox()
                    {
                        BackColor = Color.Transparent,
                        Checked = i == 0,
                        Size = new Size(checkboxSize, checkboxSize)
                    };
                    pianos[i].CheckStateChanged += TogglePiano;
                    pianos[i].VisibleChanged += TogglePiano;
                }
                mutes[i] = new CheckBox()
                {
                    BackColor = Color.Transparent,
                    Size = new Size(checkboxSize, checkboxSize)
                };
                mutes[i].CheckStateChanged += ToggleMute;
                mutes[i].VisibleChanged += (o, e) => ((CheckBox)o).Checked = ((CheckBox)o).Visible; // Update toggles
            }
            Controls.AddRange(mutes);
            Controls.AddRange(pianos);
            DeleteData();
            ResumeLayout(false);
        }
        
        void TogglePiano(object sender, EventArgs e)
        {
            if (ParentForm == null) return;
            for (int i = 0; i < 16; i++)
                ((MainForm)ParentForm).PianoTracks[i] = pianos[i].Checked && pianos[i].Visible;
        }
        void ToggleMute(object sender, EventArgs e)
        {
            var check = (CheckBox)sender;
            if (check == mutes[16])
            {
                bool b = check.CheckState != CheckState.Unchecked;
                for (int i = 0; i < MusicPlayer.NumTracks; i++)
                    mutes[i].Checked = b;
            }
            else
            {
                int on = 0;
                for (int i = 0; i < MusicPlayer.NumTracks; i++)
                {
                    if (mutes[i] == check)
                        MusicPlayer.SetMute(i, !check.Checked);
                    if (mutes[i].Checked)
                        on++;
                }
                mutes[16].CheckStateChanged -= ToggleMute;
                mutes[16].CheckState = on == MusicPlayer.NumTracks ? CheckState.Checked : (on == 0 ? CheckState.Unchecked : CheckState.Indeterminate);
                mutes[16].CheckStateChanged += ToggleMute;
            }
        }

        internal TrackInfoControl() => InitializeComponent();

        internal void ReceiveData((ushort, uint[], byte[], byte[], byte[][], float[], byte[], byte[], int[], float[], string[]) tup)
        {
            tempo = tup.Item1; positions = tup.Item2; volumes = tup.Item3;
            delays = tup.Item4; notes = tup.Item5; velocities = tup.Item6;
            voices = tup.Item7; mods = tup.Item8; bends = tup.Item9;
            pans = tup.Item10; types = tup.Item11;
            Invalidate();
        }
        internal void DeleteData()
        {
            previousNotes = new Tuple<int[], string[]>(new int[16], new string[16]);
            tempo = 120;
            positions = new uint[16];
            volumes = new byte[16];
            delays = new byte[16];
            notes = new byte[16][];
            velocities = new float[16];
            voices = new byte[16];
            mods = new byte[16];
            bends = new int[16];
            pans = new float[16];
            types = new string[16];
            for (int i = 0; i < 16; i++)
            {
                notes[i] = new byte[0];
                previousNotes.Item2[i] = noNotes;
            }
            for (int i = MusicPlayer.NumTracks; i < 16; i++)
                pianos[i].Visible = mutes[i].Visible = false;
            Invalidate();
        }

        void TrackInfoControl_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.FillRectangle(Brushes.Black, 0, 0, Width, Height);

            float ih = Height / 25.125f; // Info height
            float iy = ih - (e.Graphics.MeasureString("A", Font).Height * 1.125f); // Info y
            int co = (checkboxSize - 13) / 2; // Checkbox offset
            float px = checkboxSize * 2 + co * 2; // Position x
            int FWidth = Width - (int)px; // Fake width
            float dex = px + FWidth / 5.75f; // Del x
            float dx = dex - e.Graphics.MeasureString("Delay", Font).Width + e.Graphics.MeasureString("99", Font).Width; // "Delay" x
            float nx = px + FWidth / 4.4f; // Notes x
            float td = FWidth / 100f; // Voice type difference
            float ix = px + FWidth - td;
            float itx = ix - e.Graphics.MeasureString("Instrument Type", Font).Width; // "Instrument Type" x
            float vox = px + FWidth / 25f; // Voices x
            float r2d = FWidth / 15f; // Row 2's addition per element

            float ym = Height / 201f; // y margin
            float th = (Height - ym) / 16f; // Track height
            float r2o = th / 2.5f;
            int bh = (int)(Height / 30.3f); // Bar height
            int bx = (int)(px + FWidth / 2.35f); // Bar start x
            int bw = (int)(FWidth / 2.95f); // Bar width
            int cx = bx + (bw / 2); // Bar center x
            int bwd = bw % 2; // Add/Subtract by 1 if the bar width is odd

            string tempoStr = "Tempo - " + tempo.ToString();
            float tx = cx - (e.Graphics.MeasureString(tempoStr, Font).Width / 2); // "Tempo - 120" x

            mutes[16].Location = new Point(co, (int)iy + co);
            e.Graphics.DrawString("Position", Font, Brushes.Lime, px, iy);
            e.Graphics.DrawString("Delay", Font, Brushes.Crimson, dx, iy);
            e.Graphics.DrawString("Notes", Font, Brushes.Turquoise, nx, iy);
            e.Graphics.DrawString(tempoStr, Font, Brushes.OrangeRed, tx, iy);
            e.Graphics.DrawString("Instrument Type", Font, Brushes.DeepPink, itx, iy);
            e.Graphics.DrawLine(Pens.Gold, 0, ih, Width, ih);

            for (int i = 0; i < MusicPlayer.NumTracks; i++)
            {
                float r1y = ih + ym + (i * th); // Row 1 y
                float r2y = r1y + r2o; // Row 2 y

                byte velocity = (byte)(velocities[i] * 0xFF);

                mutes[i].Location = new Point(co, (int)r1y + co); // Checkboxes
                pianos[i].Visible = mutes[i].Visible = true;
                pianos[i].Location = new Point(checkboxSize + co * 2, (int)r1y + co);

                e.Graphics.DrawString(string.Format("0x{0:X6}", positions[i]), Font, Brushes.Lime, px, r1y);
                e.Graphics.DrawString(delays[i].ToString(), Font, Brushes.Crimson, dex, r1y);

                e.Graphics.DrawString(voices[i].ToString(), Font, Brushes.OrangeRed, vox, r2y);
                e.Graphics.DrawString(velocity.ToString(), Font, Brushes.PeachPuff, vox + r2d, r2y);
                e.Graphics.DrawString(volumes[i].ToString(), Font, Brushes.LightSeaGreen, vox + (r2d * 2), r2y);
                e.Graphics.DrawString(mods[i].ToString(), Font, Brushes.SkyBlue, vox + (r2d * 3), r2y);
                e.Graphics.DrawString(bends[i].ToString(), Font, Brushes.Purple, vox + (r2d * 4), r2y);

                int by = (int)(r1y + ym); // Bar y
                int pax = (int)(bx + (bw / 2) + (bw / 2 * pans[i])); // Pan line x

                e.Graphics.DrawLine(Pens.GreenYellow, bx, by, bx, by + bh); // Left bar bound line
                e.Graphics.DrawLine(Pens.Purple, cx, by, cx, by + bh); // Center line
                if (Config.PanpotIndicators) e.Graphics.DrawLine(Pens.OrangeRed, pax, by, pax, by + bh); // Pan line
                e.Graphics.DrawLine(Pens.GreenYellow, bx + bw - bwd, by, bx + bw - bwd, by + bh); // Right bar bound line

                float percentRight = (pans[i] + 1) * velocities[i] / 2,
                    percentLeft = (-pans[i] + 1) * velocities[i] / 2;

                var rect = new Rectangle((int)(bx + (bw / 2) - (percentLeft * bw / 2)) + bwd,
                    by,
                    (int)((percentLeft + percentRight) * bw / 2),
                    bh);
                e.Graphics.FillRectangle(
                    new LinearGradientBrush(new Point(bx, bh), new Point(bx + bw, bh), Color.FromArgb(velocity, barColor), Color.Purple),
                    rect);
                e.Graphics.DrawRectangle(Pens.Purple, rect);

                string theseNotes = string.Join(" ", notes[i].Select(n => string.Format("{0}{1}", simpleNotes[n % 12], (n / 12) - 2)));
                bool empty = string.IsNullOrEmpty(theseNotes);
                theseNotes = empty ? noNotes : theseNotes;
                if (empty && previousNotes.Item1[i]++ < Config.RefreshRate) theseNotes = previousNotes.Item2[i];
                else if (!empty || previousNotes.Item2[i] != theseNotes) { previousNotes.Item1[i] = 0; previousNotes.Item2[i] = theseNotes; }
                e.Graphics.DrawString(theseNotes, Font, Brushes.Turquoise, nx, r1y);

                var strSize = e.Graphics.MeasureString(types[i], Font);
                e.Graphics.DrawString(types[i], Font, Brushes.DeepPink, ix - strSize.Width, by + (r2o / (Font.Size / 2.5f)));
            }
        }
    }
}
