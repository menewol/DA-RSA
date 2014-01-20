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
        string scmd = "";

        public Blacklist()
        {
            InitializeComponent();
            //conn1 = new MySqlConnection(@"server='213.47.71.253';database='rsa';uid='rsa';pwd='rsa'");
            conn1 = new MySqlConnection(@"server='127.0.0.1';database='rsa_daten';uid='root';pwd=''");
        }

        private void button_hin_Click(object sender, EventArgs e)
        {
            //hinzufügen von Element
            string name = textBox1.Text;

            MySqlCommand cmd = new MySqlCommand("SELECT Prozess FROM blacklist WHERE prozess='" + name + "';", conn1);
            MySqlDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                if (rdr[0].ToString() == name) return;
            }
            rdr.Close();

            scmd += "INSERT INTO rsa_daten.blacklist (Prozess, Block) VALUES ('" + name + "', '1');";
            listBox1.Items.Add(name);
        }

        private void Blacklist_Load(object sender, EventArgs e)
        {
            conn1.Open();

            read();
        }

        public void read()
        {
            //Blacklist auslesen
            listBox1.Items.Clear();

            MySqlCommand cmd = new MySqlCommand("SELECT Prozess FROM blacklist;", conn1);
            MySqlDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                listBox1.Items.Add(rdr[0].ToString());
            }
            rdr.Close();
        }

        private void button_del_Click(object sender, EventArgs e)
        {
            //element löschen
            if (listBox1.SelectedIndex != -1)
            {
                scmd += "DELETE FROM blacklist WHERE prozess='" + listBox1.SelectedItem.ToString() + "';";
                listBox1.Items.Remove(listBox1.SelectedItem);
            }
            else MessageBox.Show("Bitte wählen Sie ein Element aus!");
        }

        private void button_sp_Click(object sender, EventArgs e)
        {
            //speichern
            if (scmd != "")
            {
                MySqlCommand cmd = new MySqlCommand(scmd, conn1);
                cmd.ExecuteNonQuery();
            }
            this.Close();
        }

        private void button_abb_Click(object sender, EventArgs e)
        {
            //abbrechen
            this.Close();
        }
    }
}
