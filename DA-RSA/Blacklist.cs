using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace DA_RSA
{
    public partial class Blacklist : Form
    {
        MySqlConnection conn1;

        public Blacklist()
        {
            InitializeComponent();
            conn1 = new MySqlConnection(@"server='213.47.71.253';database='rsa';uid='rsa';pwd='rsa'");
        }

        private void button_hin_Click(object sender, EventArgs e)
        {

        }

        private void Blacklist_Load(object sender, EventArgs e)
        {
            conn1.Open();

            MySqlCommand cmd = new MySqlCommand("SELECT Prozess FROM blacklist;", conn1);
            MySqlDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                listBox1.Items.Add(rdr[0].ToString());
            }
            rdr.Close();
        }
    }
}
