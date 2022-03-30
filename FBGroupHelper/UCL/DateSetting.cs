using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FBGroupHelper
{
    public partial class DateSetting : Form
    {
        public DateTime _Sdatetime { get; set; }
        public DateTime _EdateTime { get; set; }
        public DateSetting()
        {
            InitializeComponent();
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            _Sdatetime = dtp_SDate.Value;
            _EdateTime = dtp_EDate.Value;
            DialogResult = DialogResult.OK;
            this.Close();
        }

    }
}
