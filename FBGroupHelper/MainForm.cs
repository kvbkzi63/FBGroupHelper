using Facebook;
using FBGroupHelper.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FBGroupHelper.Helper;
using System.Xml.Linq;
using System.Dynamic;
using System.Threading;
using System.Xml;
using System.Diagnostics;

namespace FBGroupHelper
{
    public partial class MainForm : Form
    {
        CheckBox cbHeader = new CheckBox(); 
        private MyFormBase _waitForm; 
        public String PAGE_TOKEN = ConfigurationManager.AppSettings["PageToken"];
        public string GROUP_ID = ConfigurationManager.AppSettings["GroupID"];
        public List<PostsModel> POSTS = null;
        public List<CommentsModel> Comments = new List<CommentsModel>();
        private string parserPath = Application.StartupPath + "\\parserData.xml";
        private string commentsPath = Application.StartupPath + "\\comments.xml";
        private string DynamicCharPath = Application.StartupPath + "\\DynamicString.xml";
        private List<ParserModel> PARSERS = null;
        private List<DynamicCharModel> DynamicChar = null;
        private bool error = false;
        private string currentPost = "";
        Random rnd = new Random();
        private int currentCount = 0;
        private int DynamicCharIndex = 0;
        public MainForm()
        {
            InitializeComponent();
            Process[] _p = Process.GetProcessesByName("FBGroupHelper");
            if (_p.Count() > 1)
            {
                foreach (Process p in _p)
                {
                    if (Process.GetCurrentProcess().Id == p.Id)
                        continue;
                    p.Kill();
                }
            }
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true;
            backgroundWorker1.DoWork += new DoWorkEventHandler(worker_DoWork);
            backgroundWorker1.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
            txt_PostTitle.Focus();
            GROUP_ID = EncDecCode.Form1.Decrypt(GROUP_ID, "abcdefgh");
            FBHelper.Instance.WriteLog("Login App Start");
            ShowWaitForm("授權驗證中，請稍後..."); 
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.ShowBalloonTip(5000, "開啟成功", "開啟成功", ToolTipIcon.Info);
            StartWork();
        }
        private void ResetPostByDate()
        {
            //POSTS = FBHelper.Instance.GetGroupPostsByDate(PAGE_TOKEN, GROUP_ID, _dt._Sdatetime, _dt._EdateTime).ToList();
        }
        protected void ShowWaitForm(string message)
        {
            // don't display more than one wait form at a time
            if (_waitForm != null && !_waitForm.IsDisposed)
            {
                return;
            }
            if (!FBHelper.Instance.CheckAuthIsActived())
            {
                MessageBox.Show("授權失效，應用程式關閉");
                this.Close();
            }

            _waitForm = new MyFormBase();
            _waitForm.SetMessage(message); // "Loading data. Please wait..."
            _waitForm.TopMost = true;
            _waitForm.StartPosition = FormStartPosition.CenterScreen;
            _waitForm.Show();
            _waitForm.Refresh();

            // force the wait window to display for at least 700ms so it doesn't just flash on the screen
            System.Threading.Thread.Sleep(3000);
            Application.Idle += OnLoaded;
        }

        private void OnLoaded(object sender, EventArgs e)
        {
            Application.Idle -= OnLoaded;
            _waitForm.Close();
        }
        private void initialDgvParser()
        {
            this.dgv_parser.DataSource = null;
            this.dgv_parser.Rows.Clear();
            this.dgv_parser.Columns.Clear();
            dgv_parser.DataSource = PARSERS;
            dgv_parser.Columns["message"].HeaderText = "預設片語訊息";
            dgv_parser.Columns["id"].Visible = false; 
        } 
        private void initialEditParser()
        {
            this.dgv_ParserEdit.DataSource = null;
            this.dgv_ParserEdit.Rows.Clear();
            this.dgv_ParserEdit.Columns.Clear();
            dgv_ParserEdit.DataSource = PARSERS;
            DataGridViewButtonColumn btnRemove = new DataGridViewButtonColumn();
            btnRemove.Name = "btnRemove";
            btnRemove.Text = "X";
            btnRemove.UseColumnTextForButtonValue = true;
            dgv_ParserEdit.Columns.Insert(0, btnRemove);
            dgv_ParserEdit.Columns[0].HeaderText = "刪";
            dgv_ParserEdit.Columns[0].Width = 40;
            dgv_ParserEdit.Columns["message"].HeaderText = "片語內容";
            dgv_ParserEdit.Columns["id"].Visible =false;

        }
        private void lockUI()
        {
            tlp_Main.Enabled = false; 
        }

        private void unLockUI()
        {
            tlp_Main.Enabled = true;
            btn_Reply.Enabled = true;
        }
        #region events
        /// <summary>
        /// 全選事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbHeader_CheckedChanged(Object sender, EventArgs e)
        {
            CheckBox ckbH = (CheckBox)dgv_comments.Controls.Find("checkboxHeader", true)[0];
            foreach (DataGridViewRow dr in dgv_comments.Rows)
                dr.Cells["Chk"].Value = ckbH.Checked;
        }
        /// <summary>
        /// 初始化界面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Load(object sender, System.EventArgs e)
        {
            try
            {
 
                PARSERS = FBHelper.Instance.FromXML(parserPath);
                DynamicChar = FBHelper.Instance.DynamicChar(DynamicCharPath);
                if (PARSERS != null && PARSERS.Count > 0)
                { 
                    this.initialDgvParser();
                    this.initialEditParser();
                }
                if (System.IO.File.Exists(commentsPath))
                {
                    Comments = FBHelper.Instance.LeftComments(commentsPath);
                    if (Comments.Count >= 0)
                    {
                        if (MessageBox.Show("是否繼續未完成的回覆? 尚有: "+Comments.Count +" 筆回覆未完成", "確認",MessageBoxButtons.OKCancel) == DialogResult.OK)
                        {
                            backgroundWorker1.RunWorkerAsync(); 
                        }
                        else
                        {
                            System.IO.File.Delete(commentsPath);
                            Comments = new List<CommentsModel>();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        /// <summary>
        /// 查詢貼文
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_query_Click(object sender, EventArgs e)
        {
            try
            {
                string postId = "";
                if (txt_PostTitle.Text.Contains("posts"))
                {
                    for (int i = 0; i <  txt_PostTitle.Text.Split('/').Length; i++)
                    {
                        if (txt_PostTitle.Text.Split('/')[i] == "posts")
                        {
                            postId = txt_PostTitle.Text.Split('/')[i+1];
                            break;
                        }
                    }
                 }
                else
                {
                      postId = txt_PostTitle.Text.Substring(txt_PostTitle.Text.IndexOf("gm.") + 3, 15); 
                }
                FacebookClient fb = new FacebookClient(PAGE_TOKEN);
                JsonObject post1 = fb.Get<JsonObject>($"{GROUP_ID + "_" + postId + "?limit=500"}");
                List<PostsModel> dt = new List<PostsModel>();
                dt.Add(JsonConvert.DeserializeObject<PostsModel>(post1.ToString()));
                if (dt.Count > 0)
                {
                    GetCommentByPostId(GROUP_ID + "_" + postId);
                    txt_PostContent.Text = dt[0].message;
                }
                else
                {
                    txt_PostContent.Text = "";
                }
 
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        private void tb_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btn_query.PerformClick();
            }
        }
        /// <summary>
        /// 取得留言
        /// </summary>
        /// <param name="id"></param>
        private void GetCommentByPostId(string id)
        {
            FacebookClient fb = new FacebookClient(PAGE_TOKEN);
            JsonObject post1 = fb.Get<JsonObject>($"{id}/comments?limit=200");
            var posts = post1["data"];
            List<CommentsModel> dt = JsonConvert.DeserializeObject<List<CommentsModel>>(posts.ToString());
            dt = dt.OrderBy(x => x.created_time).ToList();
            DataGridViewCheckBoxColumn dgvCmb = new DataGridViewCheckBoxColumn();
            dgvCmb.ValueType = typeof(bool);
            dgvCmb.Name = "Chk";
            dgvCmb.HeaderText = "選";
            DataGridViewCheckBoxCell ckbSelect = new DataGridViewCheckBoxCell();
            this.dgv_comments.DataSource = null;
            this.dgv_comments.Rows.Clear();
            this.dgv_comments.Columns.Clear();
            dgv_comments.DataSource = dt;
            dgv_comments.Columns.Insert(0, dgvCmb);
            dgv_comments.Columns[0].HeaderText = "    ";
            dgv_comments.Columns[0].Width = 60;
            dgv_comments.Columns["message"].HeaderText = "留言內容";
            dgv_comments.Columns["id"].Visible = false;
            dgv_comments.Columns["post_id"].Visible = false;
            dgv_comments.Columns["reply_message"].Visible = false;
            dgv_comments.Columns["created_time"].HeaderText = "發佈時間";
            dgv_comments.Columns["updated_time"].Visible = false;
            dgv_comments.Columns["replyed"].Visible = false;
            //dataGridView2.Columns[1].Visible = false; 
            //建立個矩形，等下計算 CheckBox 嵌入 GridView 的位置
            Rectangle rect = dgv_comments.GetCellDisplayRectangle(0, -1, true);
            rect.X = rect.Location.X + rect.Width / 4 + 3;
            rect.Y = rect.Location.Y + (rect.Height / 2 - 9);
            cbHeader.Checked = false;
            cbHeader.Name = "checkboxHeader";
            cbHeader.Size = new Size(18, 18);
            cbHeader.Location = rect.Location;
            //全選要設定的事件
            cbHeader.CheckedChanged += new EventHandler(cbHeader_CheckedChanged);

            //將 CheckBox 加入到 dataGridView
            dgv_comments.Controls.Add(cbHeader);
        }
        /// <summary>
        /// 取得留言
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            int gigUrlColumnIdx = 0;
            if (e.ColumnIndex == gigUrlColumnIdx)
            {
                string id = dgv_posts.Rows[e.RowIndex].Cells["id"].Value.ToString();
                GetCommentByPostId(id);
            }
        }
        /// <summary>
        /// 代入片語
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgv_parser_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            rtb_Reply.Text += dgv_parser.Rows[e.RowIndex].Cells["message"].Value.ToString();

        }
        /// <summary>
        /// 修改片語
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgv_ParserEdit_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            int gigUrlColumnIdx = 0;
            ///移除片語
            if (e.ColumnIndex == gigUrlColumnIdx)
            {
                var itemToRemove = PARSERS.SingleOrDefault(r => r.id == (int)dgv_ParserEdit.Rows[e.RowIndex].Cells["id"].Value);
                if (itemToRemove != null)
                    PARSERS.Remove(itemToRemove);
                string s = FBHelper.Instance.ToXML<List<ParserModel>>(PARSERS);
                FBHelper.Instance.SaveXml(s, FBHelper.xmlName.parserData);
                this.initialEditParser();
                this.initialDgvParser();
            }
            else
            {
                if (e.RowIndex >= 0)
                {
                    rtb_Parser.Text= dgv_ParserEdit.Rows[e.RowIndex].Cells["message"].Value.ToString();
                    btn_EditParser.Enabled = true;
                }
            }
        }
        void addedRichTextBox_TextChanged(object sender, EventArgs e)
        {
            btn_AddParser.Enabled = (!(rtb_Parser.Text.Length == 0));
            if (rtb_Parser.Text.Length == 0)
            {
                btn_AddParser.Enabled = false;
                btn_EditParser.Enabled = false;
            }
 
        } 
        /// <summary>
        /// 編輯片語
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_EditParser_Click(object sender, EventArgs e)
        {
            var item = PARSERS.SingleOrDefault(r =>  r.id == (int)dgv_ParserEdit.Rows[dgv_ParserEdit.CurrentRow.Index].Cells["id"].Value);
            if (item != null)
                item.message = rtb_Parser.Text;
            this.initialEditParser();
            this.initialDgvParser();

        }
        /// <summary>
        /// 新增片語
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_AddParser_Click(object sender, EventArgs e)
        {
            if (rtb_Parser.Text.Length == 0)
                return; 
            if (PARSERS == null)
            {
                PARSERS = new List<ParserModel>();
            }
            if (PARSERS.Count == 0)
            {
                PARSERS.Add(new ParserModel() { id = 1, message = rtb_Parser.Text });
            } 
            else
            {
                PARSERS.Add(new ParserModel() { id = PARSERS.Max(x => x.id) + 1, message = rtb_Parser.Text });
            }
            this.initialDgvParser();
            this.initialEditParser();
            rtb_Parser.Text = "";
            string s = FBHelper.Instance.ToXML<List<ParserModel>>(PARSERS);
            FBHelper.Instance.SaveXml(s, FBHelper.xmlName.parserData);
        }

        #endregion

        private void btn_Reply_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgv_comments.Rows.Count < 0 ) return;
                if (rtb_Reply.Text.Length == 0)
                {
                    MessageBox.Show("未選擇訊息");
                    return;
                }

                _replyText =   rtb_Reply.Text ;
                var checkedRows = from DataGridViewRow r in dgv_comments.Rows
                                  where Convert.ToBoolean(r.Cells[0].Value) == true && (Comments.Where(x=>x.id== r.Cells["id"].Value.ToString()).Count()==0)
                                  select new CommentsModel() { post_id=txtPostTitle.Text, id = r.Cells["id"].Value.ToString(), message = r.Cells["message"].Value.ToString() ,reply_message=_replyText};
                if (checkedRows.Count() != 0)
                {
                    Comments.AddRange(checkedRows.ToList()); 
                }
                if (!backgroundWorker1.IsBusy)
                {
                    this.lblMsg.Text = "開始";
                    this.ProgressBar1.Value = 0;
                    FBHelper.Instance.WriteLog("送出回覆 開始");

                    backgroundWorker1.RunWorkerAsync();
                }
                else
                { 
                    backgroundWorker1.ReportProgress(Convert.ToInt16(Math.Round((decimal)currentCount / Comments.Count(), 2) * 100)); 
                } 
            }
            catch (Exception ex)
            {
                lblMsg.Text = ex.ToString();
            }
 
        }

        private string _replyText = "";
        #region     最小化
        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                this.notifyIcon1.Visible = true;
            }
        }
        private void notifyIcon1_MouseDoubleClick(Object sender, MouseEventArgs e)
        {
            //讓Form再度顯示，並寫狀態設為Normal
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {

            if (this.WindowState != FormWindowState.Minimized && !error)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                notifyIcon1.Tag = string.Empty;
                notifyIcon1.ShowBalloonTip(3000, this.Text,
                     "程式並未結束，要結束請在圖示上按右鍵，選取結束功能!",
                     ToolTipIcon.Info);
            }
        }
        #endregion
        #region     背景執行
        System.Timers.Timer timersTimer = new System.Timers.Timer();
        private void StartWork()
        {
            this.lblMsg.Text = "開始";
            timersTimer.Interval = 10000;
            timersTimer.Enabled = true;
            timersTimer.Elapsed += TimersTimer_Elapsed;
            timersTimer.Start();
        }
        private void StopWork()
        {
            this.lblMsg.Text = "暫停監控";
            timersTimer.Elapsed -= TimersTimer_Elapsed;
            timersTimer.Stop();
            if (backgroundWorker1.IsBusy)
            {
                backgroundWorker1.CancelAsync();
            }
        }
        private void TimersTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.BeginInvoke(new Action(() =>
            {
                if (!backgroundWorker1.IsBusy)
                {
                    backgroundWorker1.RunWorkerAsync();
                }
            }), null);
        } 
 
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            { 
                int p = 0;
                for (int i = 0; i <= Comments.Count - 1; i++)
                {
                     dynamic parameters = new ExpandoObject();
                    if (DynamicCharIndex == DynamicChar.Count-1)
                    {
                        DynamicCharIndex = 0;
                    }
                    else
                    {
                        DynamicCharIndex += 1;
                    }
                    if (i % 2 == 1)
                    {
                        parameters.message = $" {DynamicChar[DynamicCharIndex].start} 隨機碼：{rnd.Next(0, 9999).ToString()} {DynamicChar[DynamicCharIndex].end}" + dgv_parser.Rows[rnd.Next(0, dgv_parser.Rows.Count - 1)].Cells["message"].Value.ToString()+"，" + Comments[i].reply_message   ; 
                    }
                    else
                    {
                        parameters.message = dgv_parser.Rows[rnd.Next(0, dgv_parser.Rows.Count - 1)].Cells["message"].Value.ToString() + " " + Comments[i].reply_message + $"  {DynamicChar[DynamicCharIndex].start} 隨機碼：{rnd.Next(0, 9999).ToString()} {DynamicChar[DynamicCharIndex].end}";
                    }
 
                    currentCount += 1;

                    p = Convert.ToInt16(Math.Round((decimal)currentCount / Comments.Count(), 2) * 100);
                    backgroundWorker1.ReportProgress((p < 100 ? p : 100), Comments[i].id);
                    Thread.Sleep((rnd.Next(2, 4) * 60) * 1000); 
                    FacebookClient fb = new FacebookClient(PAGE_TOKEN);
                    FBHelper.Instance.WriteLog("回覆中：" + currentCount.ToString() + "　/　" + Comments.Count.ToString());
                    FBHelper.Instance.WriteLog("回覆連結：" + Comments[i].post_id);
                    FBHelper.Instance.WriteLog("回覆內容：" + parameters.message);
                    try
                    {
                        JsonObject post1 = fb.Post($"{Comments[i].id}/comments", parameters);
                        Comments[i].replyed = true;

                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("#1705"))
                        {
                            FBHelper.Instance.WriteLog("回覆失敗，無效連結：" + Comments[i].post_id + " 留言ID:" + Comments[i].id + "　內文:　" + Comments[i].message);
                            Comments[i].replyed = true;
                        }
                        else 
                        {
                            FBHelper.Instance.WriteLog("回覆失敗，無效連結：" + Comments[i].post_id + " 留言ID:" + Comments[i].id + "　內文:　" + Comments[i].message +" 錯誤內容:"+ ex.ToString());
                        }
                    }
                    FBHelper.Instance.WriteLog("回覆完成：" + currentCount.ToString() + "　/　" + Comments.Count.ToString()); 
                    string s = FBHelper.Instance.ToXML<List<CommentsModel>>(Comments.Where(x => x.replyed == false).ToList());
                    FBHelper.Instance.SaveXml(s, FBHelper.xmlName.comments);
                }
            }
            catch (Exception ex)
            {
                currentCount -= 1; 
                throw ex;
            }

        }
 
        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.ProgressBar1.Value = e.ProgressPercentage;
            this.lblMsg.Text = "回覆中：" + currentCount.ToString() + "　/　" + Comments.Count.ToString();
        }
        //執行完成
        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                if ((e.Cancelled == true))
                {
                    this.lblMsg.Text = "取消!";
                    Comments = new List<CommentsModel>();
                    FBHelper.Instance.WriteLog("取消回覆");

                }
                else if (!(e.Error == null))
                {
                    FBHelper.Instance.WriteLog(e.ToString()); 
                    FBHelper.Instance.WriteLog("回覆失敗：" + currentCount.ToString() + "　/　" + Comments.Count.ToString()); 

                    if (e.Error.Message.Contains("#368"))
                    {
                        MessageBox.Show("Faceook帳號被鎖定，請明日再試");
                        this.Close();
                    }
                    this.lblMsg.Text = ("Error: " + e.Error.Message +" "); 
                }
                else
                {
                    this.lblMsg.Text = "回覆完畢!";
                    FBHelper.Instance.WriteLog("回覆完畢"); 
                    Comments = new List<CommentsModel>();
                    if (System.IO.File.Exists(commentsPath))
                        System.IO.File.Delete(commentsPath);
                }
            }
            catch (Exception ex)
            {
                lblMsg.Text = ex.ToString();

            }
            //unLockUI();
        }
        private void Menu_Close_Click(object sender, EventArgs e)
        {
            error = true;
            this.Close();
        }
        #endregion

    }
}
