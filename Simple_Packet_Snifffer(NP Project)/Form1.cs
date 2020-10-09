using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Simple_Packet_Snifffer_NP_Project_
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }


        private void clearDetail()
        {
            this.CharTextBox.Text = "";
            this.HexTextBox.Text = "";
        }
        /// <summary>
        /// used to rake the underlying packets;
        /// </summary>
        List<Monitor> monitorList = new List<Monitor>();

        /// <summary>
        /// presenting packets;
        /// </summary>
        List<Packet> pList = new List<Packet>();

        /// <summary>
        /// the packets sniffed -- all;
        /// </summary>
        List<Packet> allList = new List<Packet>();

        /// <summary>
        /// used to refresh the packets sniffed and listView and all the related info;
        /// </summary>
        /// <param name="p"></param>
        delegate void refresh(Packet p);

        /// <summary>
        /// total length sniffed so far - isolating the filtered;
        /// </summary>
        long totalLength = 0;

        /// <summary>
        /// the count of the packets sniffed;
        /// </summary>
        long totalCount = 0;

        
        /// <summary>
        /// deactivate some buttons;
        /// </summary>
        private void deactivateSearch()
        {
            filterCheckBox.Enabled = false;
            ipTextBox.Enabled = false;
            typeComboBox.Enabled = false;
            startButton.Enabled = false;
            filterButton.Enabled = false;
            allButton.Enabled = false;
        }

        private void activateSearch()
        {
            filterCheckBox.Enabled = true;
            ipTextBox.Enabled = true;
            typeComboBox.Enabled = true;
            startButton.Enabled = true;
            filterButton.Enabled = true;
            allButton.Enabled = true;
        }

        private void startRaking()
        {
            monitorList.Clear();
            
            IPAddress[] hosts = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
            if (hosts == null || hosts.Length == 0)
            {
                MessageBox.Show("No hosts detected, please check your network!");
            }

            for (int i = 0; i < hosts.Length; i++)
            {
                //ye host==ip's la kar dega ho aap ke system mai connected device ki (for confirmation check cmd pe ipconfig)
                Monitor monitor = new Monitor(hosts[i]);
                monitor.newPacketEventHandler += new Monitor.NewPacketEventHandler(onNewPacket);
                monitorList.Add(monitor);
            }
            foreach (Monitor monitor in monitorList)
            {
                monitor.start();
            }
        }

        private void onNewPacket(Monitor monitor, Packet p)
        {

            this.Invoke(new refresh(onRefresh), p);
            //this.BeginInvoke(new refresh(onRefresh), p);
        }

        private void onRefresh(Packet p)
        {
            //MessageBox.Show(filterCheckBox.Checked.ToString() + filteringPattern);
            if (this.filterCheckBox.Checked)
            {
                string[] conditions = getFilterCondition();
                if (isIPOkay(p, conditions[0]) && isPORTOkay(p, conditions[1])
                    && (conditions[2] == "" || conditions[2] == p.Type))
                {
                    addAndUpdatePackets(p);
                }
            }
            else
            {
                addAndUpdatePackets(p);
            }
            //MessageBox.Show(p.Src_IP);
            if (totalLength < 10 * 1024)
            {
                this.hintLabel.Text = string.Format("Packets received {0}  Total length： [{1} bytes]", totalCount, totalLength);
            }
            else if (totalLength < 10 * 1024 * 1024)
            {
                this.hintLabel.Text = string.Format("Packets received {0}  Total length： [{1} KB]", totalCount, totalLength / 1024);
            }
            else if (totalLength < 1024 * 1024 * 1024)
            {
                this.hintLabel.Text = string.Format("Packets received {0}  Total length： [{1} MB]", totalCount, totalLength / (1024 * 1024));
            }
            else if (totalLength < (long)1024 * 1024 * 1024 * 2)
            {
                this.hintLabel.Text = string.Format("Packets received {0}  Total length： [{1} GB]", totalCount, totalLength / (1024 * 1024 * 1024));
            }
            else
            {
                totalCount = 0;
                totalLength = 0;
                this.listView.Items.Clear();
                this.pList.Clear();
                this.allList.Clear();
                this.hintLabel.Text = string.Format("Packets received {0}  Total length： [{1} bytes]", 0, 0);
            }
        }

        /// <summary>
        /// ip is "" or is equal to p.Src_IP or p.Des_IP;
        /// </summary>
        /// <param name="p"></param>
        /// <param name="ip"></param>
        /// <returns></returns>
        private bool isIPOkay(Packet p, string ip)
        {
            return ip == "" || p.Src_IP == ip || p.Des_IP == ip;
        }

        /// <summary>
        /// port is either "" or is equal to p.Src_PORT or p.Des_PORT;
        /// </summary>
        /// <param name="p"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        private bool isPORTOkay(Packet p, string port)
        {
            return port == "" || p.Src_PORT == port || p.Des_PORT == port;
        }

        /// <summary>
        /// add one packet to pList and the listView;
        /// besides, refresh the totalCount, totalLength and allList globally;
        /// </summary>
        /// <param name="p"></param>
        private void addAndUpdatePackets(Packet p)
        {
            totalCount++;
            totalLength += p.TotalLength;
            allList.Add(p);
            pList.Add(p);
            this.listView.Items.Add(new ListViewItem(new string[] { p.Src_IP, p.Src_PORT,p.Des_IP, p.Des_PORT,
                        p.Type, p.Time, p.TotalLength.ToString(), p.getCharString()}));
            //this.listView.EnsureVisible(listView.Items.Count > 5 ? listView.Items.Count - 10 : listView.Items.Count);
        }

        /// <summary>
        /// stop sniffing the network;
        /// </summary>
        private void stopReceiving()
        {
            foreach (Monitor monitor in monitorList)
            {
                monitor.stop();
            }
        }

        
        private void ipTextBox_GotFocus(object sender, System.EventArgs e)
        {
            ipTextBox.ForeColor = Color.Black;
            ipTextBox.Text = "";
            ipTextBox.GotFocus -= ipTextBox_GotFocus;
        }

        
        private void listView_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListView listView = sender as ListView;
            if (listView.SelectedItems != null && listView.SelectedItems.Count != 0)
            {
                Packet p = pList[listView.SelectedItems[0].Index];
                //MessageBox.Show("charLength:" + p.getCharString().Length + "\n" + "hexLength:" + p.getHexString().Length);
                this.HexTextBox.Text = p.getHexString();
                this.CharTextBox.Text = p.getCharString();
            }
        }

        private void filterCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.filterCheckBox.Checked && this.ipTextBox.ForeColor != Color.Black)
            {
                this.ipTextBox.Text = "";
                this.ipTextBox.ForeColor = Color.Black;
            }
        }

        


        //count a certain char in a string
        private int getCharCount(string s, char c)
        {
            int count = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == c)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// return the indexes of the substring - s0 in its parentString
        /// </summary>
        /// <param name="s">this value can be changed within the method</param>
        /// <param name="s0"></param>
        /// <returns></returns>
        private List<int> getStringIndex(string s, string s0)
        {
            List<int> countList = new List<int>();
            int index = 0;///indicate the current index
            while (s.Contains(s0))
            {
                index = s.IndexOf(s0);
                s = s.Substring(0, index + s0.Length);
                countList.Add(index);///record the relative indexes
            }
            for (int i = 1; i < countList.Count; i++)///get the absolute indexes
            {
                countList[i] += (countList[i - 1] + s0.Length);
            }
            return countList;
        }

        private void charTextBox_SelectionChanged(object sender, System.EventArgs e)
        {
            ///get the started position and the tmpString.Length
            string charString = this.CharTextBox.Text;
            //int start0 = this.charTextBox.Text.IndexOf(this.charTextBox.SelectedText);///there can be exactly the same string as the selected
            string selectedString = this.CharTextBox.SelectedText;
            int selectedLength = selectedString.Length;

            ///maybe this is not quite enough to make that difference outstanding
            ///just the same string just around the the start point
            ///ToDo!
            int start0 = this.CharTextBox.SelectionStart - selectedLength;
            int start1 = this.CharTextBox.SelectionStart;

            int index = 0;
            if (start0 > -1 && charString.Substring(start0, selectedLength).Equals(selectedString))
            {
                index = start0;
            }
            else
            {
                index = start1;
            }

            string tmpString = this.CharTextBox.Text.Substring(0, index);
            int spaceCount = getCharCount(tmpString, '\n');
            
            int start = tmpString.Length * 3 - 2 * spaceCount;
            int selectedHexLength = this.CharTextBox.SelectedText.Length * 3 - 2 * getCharCount(this.CharTextBox.SelectedText, '\n');
            if (selectedHexLength > 0)
            {
                //reset backcolor
                this.HexTextBox.SelectionStart = 0;
                this.HexTextBox.SelectionLength = this.HexTextBox.Text.Length;
                this.HexTextBox.SelectionBackColor = Color.White;

                this.HexTextBox.SelectionStart = start;
                this.HexTextBox.SelectionLength = selectedHexLength;
                //this.hexTextBox.SelectionColor = Color.Red;
                this.HexTextBox.SelectionBackColor = Color.Red;
            }
            
        }

        

        

        /// <summary>
        /// according to the ipTextBox and typeComboBox get the filter conditions including ip, port and type;
        /// default value "";
        /// </summary>
        /// <returns></returns>
        private string[] getFilterCondition()
        {
            string[] conditions = { "", "", "" };
            string tmpString = this.ipTextBox.Text;
            int port = 0;
            if (this.typeComboBox.SelectedIndex > -1)
                conditions[2] = this.typeComboBox.SelectedItem.ToString();
            if (tmpString.Contains('/') || tmpString.Contains(':'))//IP:PORT OR IP/PORT
            {
                string[] arr = { null, null };
                if (tmpString.Contains('/'))
                    arr = tmpString.Split(new char[] { '/' });
                else
                    arr = tmpString.Split(new char[] { ':' });
                conditions[0] = arr[0];
                conditions[1] = arr[1];
            }
            else if (int.TryParse(tmpString, out port))//just port;
                conditions[1] = port.ToString();
            else//just IP;
                conditions[0] = tmpString;
            //Console.WriteLine(conditions);
            return conditions;
        }

        private void showIPPackets(string[] conditions)
        {
            string ipString = conditions[0];
            string port = conditions[1];
            string type = conditions[2];
            Packet p;
            this.listView.Items.Clear();
            pList.Clear();
            for (int i = 0; i < allList.Count; i++)
            {
                p = allList[i];
                if (isIPOkay(p, conditions[0]) && isPORTOkay(p, conditions[1])
                    && (conditions[2] == "" || conditions[2] == p.Type))
                {
                    pList.Add(p);
                    this.listView.Items.Add(new ListViewItem(new string[] { p.Src_IP, p.Src_PORT,p.Des_IP, p.Des_PORT,
                        p.Type, p.Time, p.TotalLength.ToString(), p.getCharString()}));
                }
            }
        }
        private void startButton_Click(object sender, EventArgs e)
        {
            clearDetail();
            deactivateSearch();
            startRaking();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //this.CharTextBox = this.HexTextBox;
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            clearDetail();
            activateSearch();
            stopReceiving();
        }

        private void Clearbutton_Click(object sender, EventArgs e)
        {
            this.listView.Items.Clear();
            pList.Clear();
            clearDetail();
        }

        private void CharTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void filterButton_Click(object sender, EventArgs e)
        {
            if (this.listView.Items.Count < 1)
            {
                MessageBox.Show("Please sniff or show all the sniffed packets first！");
            }
            showIPPackets(getFilterCondition());
            clearDetail();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure to close !" ,"EXIT",MessageBoxButtons.OKCancel);
            if (result == DialogResult.OK)
            {
                e.Cancel = false;  //close it;
            }
            else
            {
                e.Cancel = true;
            }
        }
    }
}
