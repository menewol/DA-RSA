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
using System.Security.Cryptography;

namespace DA_RSA
{
    public partial class BenutzerErstellen : Form
    {
        MySqlConnection conn1;
        string name, pw, type, cname;

        public BenutzerErstellen(MySqlConnection connB)
        {
            InitializeComponent();
            conn1 = connB;
            conn1 = new MySqlConnection(@"server='213.47.71.253';database='rsa';uid='rsa';pwd='rsa'");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            conn1.Open();
            name = textBox1.Text;
            pw = GetMd5Hash(textBox2.Text);
            if (radioButton1.Checked == true) type = "0";
            else if (radioButton2.Checked == true) type = "1";
            MySqlCommand cmd1 = new MySqlCommand("SELECT name FROM Logindaten WHERE name='" + name + "';", conn1);
            MySqlDataReader rdr1 = cmd1.ExecuteReader();
            while (rdr1.Read())
            {
                cname = Convert.ToString(rdr1[0].ToString());
            }
            rdr1.Close();

            if (cname == name)
            {
                MessageBox.Show("Name bereits vorhanden!");
            }
            else
            {
                MySqlCommand cmd2 = new MySqlCommand("INSERT INTO rsa.logindaten (name, passwort, lehrer) VALUES ('" + name + "', '" + pw + "', '" + type + "');", conn1);
                cmd2.ExecuteNonQuery();
                MessageBox.Show("Benutzer erstellt:\r\n Name: " + name + "\r\n Passwort: " + pw + "\r\n Lehrer: " + type);
            }
            conn1.Close();
        }

        static string GetMd5Hash(string input)
        {
            MD5 md5Hash = MD5.Create();
            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
