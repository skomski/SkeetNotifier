using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Windows.Forms;
using Helper.Extensions;
using NLog;
using Newtonsoft.Json;
using SkeetNotifier.Properties;

using Settings = SkeetNotifier.Properties.Settings;
using Application = System.Windows.Forms.Application;

namespace SkeetNotifier
{
    internal class MainHandler
    {
        readonly NotifyIcon _notifyIcon = new NotifyIcon{Text = Application.ProductName,Visible = true,Icon = Resources.Icon};
        readonly BackgroundWorker _backgroundWorker = new BackgroundWorker();
        readonly Timer _updateTimer = new Timer{Interval = (int)new TimeSpan(0,10,0).TotalMilliseconds,Enabled = true};

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        static bool _running;

        readonly List<JsonTypes.Answer> _answers = new List<JsonTypes.Answer>();

        public static void Run()
        {
            if (_running)
            {
                throw new InvalidOperationException("Not more then one instance allowed!");
            }
            new MainHandler();
            _running = true;
        }

        private MainHandler()
        {
            if(Settings.Default.ClickedAnswers == null) Settings.Default.ClickedAnswers = new StringCollection();
            if(Settings.Default.ShowedAnswers == null) Settings.Default.ShowedAnswers = new StringCollection();

            switch (DateTime.Now.DayOfWeek)
            {
                case DayOfWeek.Sunday:
                    _notifyIcon.BalloonTipText = Resources.SkeetFactSunday;
                    break;
                case DayOfWeek.Monday:
                    _notifyIcon.BalloonTipText = Resources.SkeetFactMonday;
                    break;
                case DayOfWeek.Tuesday:
                    _notifyIcon.BalloonTipText = Resources.SkeetFactTuesday;
                    break;
                case DayOfWeek.Wednesday:
                    _notifyIcon.BalloonTipText = Resources.SkeetFactWednesday;
                    break;
                case DayOfWeek.Thursday:
                    _notifyIcon.BalloonTipText = Resources.SkeetFactThursday;
                    break;
                case DayOfWeek.Friday:
                    _notifyIcon.BalloonTipText = Resources.SkeetFactFriday;
                    break;
                case DayOfWeek.Saturday:
                    _notifyIcon.BalloonTipText = Resources.SkeetFactSaturday;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _notifyIcon.ShowBalloonTip(1000);

            var contextMenu = new ContextMenuStrip {ShowImageMargin = false};

            var exitMenuItem = new ToolStripButton("Close"){Image = Resources.Exit,AutoToolTip = false};
            exitMenuItem.Click += (s, a) => Application.Exit();

            var refreshMenuItem = new ToolStripButton("Refresh"){Image = Resources.Refresh,AutoToolTip = false};
            refreshMenuItem.Click += (s, a) => _backgroundWorker.RunWorker();

            var autostartMenuItem = new ToolStripButton("Autostart") { Image = Resources.AutoStart, AutoToolTip = false, Checked = Helper.Extensions.Application.GetAutoStart() };
            autostartMenuItem.Click += (s, a) =>
                                           {
                                               Helper.Extensions.Application.SetAutoStart(!autostartMenuItem.Checked);
                                               autostartMenuItem.Checked = Helper.Extensions.Application.GetAutoStart();
                                           };

            _notifyIcon.ContextMenuStrip = contextMenu;

            contextMenu.Items.AddRange(new ToolStripItem[] { new ToolStripSeparator(), refreshMenuItem, autostartMenuItem, exitMenuItem });

            // Timer
            _updateTimer.Tick += (s, a) => _backgroundWorker.RunWorker();

            // Worker
            _backgroundWorker.DoWork += BackgroundWorkerDoWork;
            _backgroundWorker.RunWorkerCompleted += BackgroundWorkerRunWorkerCompleted;
            _backgroundWorker.RunWorker();
        }

        static void BackgroundWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            using (var webClient = new WebClient())
            {
                using (var memoryStream = new MemoryStream(webClient.DownloadData(new Uri(Resources.APIUri))))
                {
                    using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                    {
                        using (var stringReader = new StreamReader(gzipStream, Encoding.UTF8))
                        {
                            e.Result = new JsonSerializer().Deserialize(stringReader, typeof(JsonTypes.Root));
                        }
                    }
                }
            }
       }
        void BackgroundWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                _logger.Error(e.Error);
                MessageBox.Show(e.Error.Message);
                return;
            }

            var root = (JsonTypes.Root)e.Result;

            for (var index = 9; index >= 0; index--)
            {
                var answer = root.answers[index];

                if (_answers.Exists(ans => ans.answer_id == answer.answer_id)) continue;
                
                _answers.Insert(0,answer);

                var menuItem = new ToolStripButton(answer.title){Name = answer.answer_id.ToString()};

                if (answer.accepted) menuItem.Image = Resources.Accepted;

                if (Settings.Default.ClickedAnswers.Contains(answer.answer_id.ToString()))
                {
                    menuItem.Font = new Font(menuItem.Font, FontStyle.Strikeout);
                }

                if (!Settings.Default.ShowedAnswers.Contains(answer.answer_id.ToString()))
                {
                    Settings.Default.ShowedAnswers.Add(answer.answer_id.ToString());
                    Settings.Default.Save();

                    _notifyIcon.ShowBalloonTip(500, "New answer", answer.title, ToolTipIcon.Info);
                }

                menuItem.ToolTipText = "Score: " + answer.score + " Creation: " + answer.creation_date.UnixTime().GetRelativeTime();

                menuItem.Click += (s, a) =>
                                      {
                                          Settings.Default.ClickedAnswers.Add(answer.answer_id.ToString());
                                          Settings.Default.Save();

                                          menuItem.Font = new Font(menuItem.Font,FontStyle.Strikeout);
                                          Process.Start(Resources.QuestionUri + answer.question_id);
                                      };

                _notifyIcon.ContextMenuStrip.Items.Insert(0,menuItem);
            }
        }
    }
}