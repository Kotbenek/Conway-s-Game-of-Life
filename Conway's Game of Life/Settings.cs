using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Conway_s_Game_of_Life
{
    public partial class Settings : System.Windows.Forms.Form
    {
        
        public Settings(Form owner)
        {
            InitializeComponent();
            this.Owner = owner;
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            //Load settings from parent form
            nudStepInterval.Value = ((Form)this.Owner).tmrStep.Interval;
            chkResize.Checked = ((Form)this.Owner).resize_to_fit;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            //Save settings to parent form
            ((Form)this.Owner).tmrStep.Interval = (int)nudStepInterval.Value;
            ((Form)this.Owner).resize_to_fit = chkResize.Checked;

            //Save settings to file
            ((Form)this.Owner).save_settings();
        }
    }
}
