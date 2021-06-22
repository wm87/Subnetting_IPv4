using Subnetting;
using System;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WPF_Supernetting
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DataTable dt = new DataTable();
        private SubnettingScanner ssc;
        private int[] Slds;

        public MainWindow()
        {
            InitializeComponent();
            cb_ClsC.IsChecked = true;
            sldrHosts.Maximum = 8;
            sldrNets.Maximum = 8;
            ssc = new SubnettingScanner();
            ssc.SubnetFound += Ss_SubnetFound;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("Netz-ID", typeof(string));
            dt.Columns.Add("BC-ID", typeof(string));

            DefaultText();
        }

        private void cb_ClsC_Checked(object sender, RoutedEventArgs e)
        {
            txtCidr.Text = "24";
            sldrHosts.Value = 0;
            sldrNets.Value = 0;
        }

        private void cb_ClsB_Checked(object sender, RoutedEventArgs e)
        {
            txtCidr.Text = "16";
            sldrHosts.Value = 0;
            sldrNets.Value = 0;
        }

        private void cb_ClsA_Checked(object sender, RoutedEventArgs e)
        {
            txtCidr.Text = "8";
            sldrHosts.Value = 0;
            sldrNets.Value = 0;
        }

        private void txtbox_Cidr_TextChanged(object sender, TextChangedEventArgs e)
        {
            sldrHosts.Maximum = 4;
        }

        private void sldrHost_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int actValue = (int)sldrHosts.Value;
            txtNets.Text = Slds[0].ToString();
            sldrNets.Value = 0;
            txtHosts.Text = Slds[actValue].ToString();
        }

        private void sldrNets_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int actValue = (int)sldrNets.Value;
            txtHosts.Text = Slds[0].ToString();
            sldrHosts.Value = 0;
            txtNets.Text = Slds[actValue].ToString();
        }

        private void Ss_SubnetFound(object sender, SubnetFoundEventArgs e)
        {
            dgvIPRanges.ItemsSource = null;
            dt.Clear();

            txtRes.Text += string.Format("{0}\t\t {1}.{2}.{3}.{4}", "Net-ID", e.SubnetInformation.NetzID[0], e.SubnetInformation.NetzID[1], e.SubnetInformation.NetzID[2], e.SubnetInformation.NetzID[3]) + "\n";
            txtRes.Text += string.Format("{0}\t\t {1}.{2}.{3}.{4}", "BC-ID", e.SubnetInformation.BCID[0], e.SubnetInformation.BCID[1], e.SubnetInformation.BCID[2], e.SubnetInformation.BCID[3]) + "\n";
            txtRes.Text += $"SubMask\t {e.SubnetInformation.Submask}" + "\n";
            txtRes.Text += $"Iterations\t {Convert.ToString(e.SubnetInformation.Iterationen)}" + "\n";
            txtRes.Text += $"Among of Hosts\t {Convert.ToString(e.SubnetInformation.Hostanzahl)}" + "\n";

            for (int i = 1; i <= e.SubnetInformation.Iterationen; i++)
                dt.Rows.Add(i, e.SubnetInformation.RangesNetzId[i - 1], e.SubnetInformation.RangesBcId[i - 1]);

            dgvIPRanges.ItemsSource = dt.DefaultView;
        }

        private void DefaultText()
        {
            txtRes.Text = "In addition to the IP address and CIDR, please also select the number of hosts or the number of subnets or one of the three classes. You can then start the calculation via \"RUN\".";
        }

        private async void Run_Click(object sender, RoutedEventArgs e)
        {
            dt.Clear();
            txtRes.Text = "";
            int checkA = 0;
            int checkB = 0;

            int.TryParse(txtHosts.Text, out checkA);
            int.TryParse(txtNets.Text, out checkB);

            btn_Run.IsEnabled = false;
            btn_Run.Content = "WAIT";

            if (checkA > 0)
            {
                Stopwatch sw = Stopwatch.StartNew();

                ssc.SetIp(string.Format("{0}.{1}.{2}.{3}/{4}", txt_Oct1.Text, txt_Oct2.Text, txt_Oct3.Text, txt_Oct4.Text, txtCidr.Text));
                await Task.WhenAll(ssc.GivenHosts(Convert.ToInt32(txtHosts.Text)));

                sw.Stop();
                TimeSpan ts = sw.Elapsed;
                string elapsedTime = string.Format("{0},{1} sec", ts.Seconds, ts.Milliseconds / 10);
                txtRes.Text += $"Runtime\t\t" + elapsedTime;
            }
            else if (checkB > 0)
            {
                Stopwatch sw = Stopwatch.StartNew();

                ssc.SetIp(string.Format("{0}.{1}.{2}.{3}/{4}", txt_Oct1.Text, txt_Oct2.Text, txt_Oct3.Text, txt_Oct4.Text, txtCidr.Text));
                await Task.WhenAll(ssc.GivenSubnets(Convert.ToInt32(txtNets.Text)));

                sw.Stop();
                TimeSpan ts = sw.Elapsed;
                string elapsedTime = string.Format("{0},{1} sec", ts.Seconds, ts.Milliseconds / 10);
                txtRes.Text += $"Runtime\t\t" + elapsedTime;
            }
            else
            {
                DefaultText();
            }

            btn_Run.IsEnabled = true;
            btn_Run.Content = "RUN";
        }

        private void Cidr_TextChanged(object sender, TextChangedEventArgs e)
        {
            int checkMask = 0;
            int.TryParse(txtCidr.Text, out checkMask);

            if (checkMask > 0 && checkMask < 32)
            {
                dt.Clear();

                int hosts = 32 - checkMask;

                Slds = null;
                Slds = new int[hosts + 1];

                Slds[0] = 0;
                for (int i = 1; i <= hosts; i++)
                    Slds[i] = (int)Math.Pow(2, i);

                sldrHosts.Maximum = hosts;
                sldrNets.Maximum = hosts;
            }
            else
            {
                sldrHosts.Maximum = 0;
                sldrNets.Maximum = 0;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string ip1 = "";
            string ip2 = "";
            int cnt = 0;

            char pad = '0';

            ip1 = BinDecConverter.UmrechnerHin(Convert.ToInt16(txtSM1Oct1.Text)).PadLeft(8, pad);
            ip1 += BinDecConverter.UmrechnerHin(Convert.ToInt16(txtSM1Oct2.Text)).PadLeft(8, pad);
            ip1 += BinDecConverter.UmrechnerHin(Convert.ToInt16(txtSM1Oct3.Text)).PadLeft(8, pad);
            ip1 += BinDecConverter.UmrechnerHin(Convert.ToInt16(txtSM1Oct4.Text)).PadLeft(8, pad);

            ip2 = BinDecConverter.UmrechnerHin(Convert.ToInt16(txtSM2Oct1.Text)).PadLeft(8, pad);
            ip2 += BinDecConverter.UmrechnerHin(Convert.ToInt16(txtSM2Oct2.Text)).PadLeft(8, pad);
            ip2 += BinDecConverter.UmrechnerHin(Convert.ToInt16(txtSM2Oct3.Text)).PadLeft(8, pad);
            ip2 += BinDecConverter.UmrechnerHin(Convert.ToInt16(txtSM2Oct4.Text)).PadLeft(8, pad);

            for (int i = 0; i < 32; i++)
            {
                if (ip1[i] != ip2[i])
                    break;
                cnt++;
            }

            txtSMSubMask.Text = ssc.ShowNewSubMask(cnt, pad);

            string[] oldSubMask = ssc.GetSMSubMask(cnt).Split('.');

            byte[] netzIdBasic = ssc.GetNetzID(
                Convert.ToByte(txtSM1Oct1.Text),
                Convert.ToByte(txtSM1Oct2.Text),
                Convert.ToByte(txtSM1Oct3.Text),
                Convert.ToByte(txtSM1Oct4.Text),
                Convert.ToByte(oldSubMask[0]),
                Convert.ToByte(oldSubMask[1]),
                Convert.ToByte(oldSubMask[2]),
                Convert.ToByte(oldSubMask[3]));

            txtSMNetzID.Text = string.Format("{0}.{1}.{2}.{3}", netzIdBasic[0].ToString(), netzIdBasic[1].ToString(), netzIdBasic[2].ToString(), netzIdBasic[3].ToString());

            byte[] bcIdBasic = ssc.GetBcId(Convert.ToByte(oldSubMask[0]), Convert.ToByte(oldSubMask[1]), Convert.ToByte(oldSubMask[2]), Convert.ToByte(oldSubMask[3]), netzIdBasic[0], netzIdBasic[1], netzIdBasic[2], netzIdBasic[3]);
            txtSMBCID.Text = string.Format("{0}.{1}.{2}.{3}", bcIdBasic[0].ToString(), bcIdBasic[1].ToString(), bcIdBasic[2].ToString(), bcIdBasic[3].ToString());
        }
    }
}
