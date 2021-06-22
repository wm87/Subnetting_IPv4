using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Subnetting
{
    public class SubnettingScanner
    {
        private SubnetInformation _SubnetInformation;
        private string[] _IpString;
        private static string[] rangesNetzId;
        private static string[] rangesBcId;

        private List<Task<string[]>> listOfTasks = new List<Task<string[]>>();
        private int threads = Environment.ProcessorCount;

        public void SetIp(string ip)
        {
            string[] stringSeparators = new string[] { ".", "/" };
            string[] ipString = ip.Split(stringSeparators, StringSplitOptions.None);

            _IpString = ipString;
        }

        private string[] GetIp()
        {
            return this._IpString;
        }

        private int GetIpMaske()
        {
            return Convert.ToInt32(GetIp()[4]);
        }

        private string GetOldSubMask()
        {
            string subMask = "";
            string ones = "";
            char pad = '0';

            for (int i = 0; i < GetIpMaske(); i++)
                ones += 1;

            ones = ones.PadRight(32, pad);
            subMask += Convert.ToByte(BinDecConverter.UmrechnerRueck(ones.Substring(0, 8))) + ".";
            subMask += Convert.ToByte(BinDecConverter.UmrechnerRueck(ones.Substring(8, 8))) + ".";
            subMask += Convert.ToByte(BinDecConverter.UmrechnerRueck(ones.Substring(16, 8))) + ".";
            subMask += Convert.ToByte(BinDecConverter.UmrechnerRueck(ones.Substring(24, 8)));
            return subMask;
        }

        private byte[] GetIpBasic()
        {
            byte[] ipBasic = new byte[4];
            string[] delim;

            string subMask = GetOldSubMask();

            byte okt1Ip = Convert.ToByte(GetIp()[0]);
            byte okt2Ip = Convert.ToByte(GetIp()[1]);
            byte okt3Ip = Convert.ToByte(GetIp()[2]);
            byte okt4Ip = Convert.ToByte(GetIp()[3]);

            delim = subMask.Split('.');
            ipBasic[0] = (byte)(Convert.ToByte(delim[0]) & okt1Ip);
            ipBasic[1] = (byte)(Convert.ToByte(delim[1]) & okt2Ip);
            ipBasic[2] = (byte)(Convert.ToByte(delim[2]) & okt3Ip);
            ipBasic[3] = (byte)(Convert.ToByte(delim[3]) & okt4Ip);

            return ipBasic;
        }

        private byte[] GetNetzID(byte okt1Ip, byte okt2Ip, byte okt3Ip, byte okt4Ip, byte okt1SubMask, byte okt2SubMask, byte okt3SubMask, byte okt4SubMask)
        {
            byte[] netzID = new byte[4];

            netzID[0] = Convert.ToByte(okt1Ip & okt1SubMask);
            netzID[1] = Convert.ToByte(okt2Ip & okt2SubMask);
            netzID[2] = Convert.ToByte(okt3Ip & okt3SubMask);
            netzID[3] = Convert.ToByte(okt4Ip & okt4SubMask);

            return netzID;
        }

        private byte[] GetBcId(byte okt1SubMask, byte okt2SubMask, byte okt3SubMask, byte okt4SubMask, byte okt1NetzId, byte okt2NetzId, byte okt3NetzId, byte okt4NetzId)
        {
            byte[] bcID = new byte[4];

            bcID[0] = Convert.ToByte(okt1NetzId + 255 - okt1SubMask);
            bcID[1] = Convert.ToByte(okt2NetzId + 255 - okt2SubMask);
            bcID[2] = Convert.ToByte(okt3NetzId + 255 - okt3SubMask);
            bcID[3] = Convert.ToByte(okt4NetzId + 255 - okt4SubMask);

            return bcID;
        }

        public async Task<SubnetInformation> GivenHosts(int anzahlHosts)
        {
            int hostanzahl;
            int cidr_neu;
            int anzIter;
            char pad = '0';
            string tmp;
            string[] oldSubMask = GetOldSubMask().Split('.');

            byte[] netzIdBasic = GetNetzID(Convert.ToByte(GetIp()[0]), Convert.ToByte(GetIp()[1]), Convert.ToByte(GetIp()[2]), Convert.ToByte(GetIp()[3]), Convert.ToByte(oldSubMask[0]), Convert.ToByte(oldSubMask[1]), Convert.ToByte(oldSubMask[2]), Convert.ToByte(oldSubMask[3]));
            byte[] bcIdBasic = GetBcId(Convert.ToByte(oldSubMask[0]), Convert.ToByte(oldSubMask[1]), Convert.ToByte(oldSubMask[2]), Convert.ToByte(oldSubMask[3]), netzIdBasic[0], netzIdBasic[1], netzIdBasic[2], netzIdBasic[3]);

            // Anzahl Hosts
            int anzahlReal = (int)Math.Pow(2, Math.Ceiling(Math.Log(anzahlHosts, 2)));
            hostanzahl = anzahlReal;

            // Submaske neu
            cidr_neu = 32 - (int)Math.Log(anzahlReal, 2);
            string newSubMask = ShowNewSubMask(cidr_neu, pad);

            // Iterationen
            anzIter = (int)Math.Floor(Math.Pow(2, 32 - GetIpMaske())) / (int)(Math.Pow(2, 32 - cidr_neu));

            int h = (int)Math.Pow(2, (32 - cidr_neu));
            tmp = BinDecConverter.UmrechnerHin(h);
            tmp = tmp.Remove(0, 1);

            listOfTasks.Clear();
            rangesNetzId = null;
            rangesBcId = null;
            rangesNetzId = new string[anzIter];
            rangesBcId = new string[anzIter];

            for (int i = 0; i < threads; i++)
            {
                listOfTasks.Add(SubnettingNetzId(tmp, Convert.ToInt16(GetIpBasic()[0]), Convert.ToInt16(GetIpBasic()[1]), Convert.ToInt16(GetIpBasic()[2]), Convert.ToInt16(GetIpBasic()[3]), anzIter * i / threads, anzIter * (i + 1) / threads));
                listOfTasks.Add(SubnettingBcId(tmp, Convert.ToInt16(GetIpBasic()[0]), Convert.ToInt16(GetIpBasic()[1]), Convert.ToInt16(GetIpBasic()[2]), Convert.ToInt16(GetIpBasic()[3]), anzIter * i / threads, anzIter * (i + 1) / threads));
            }

            await Task.WhenAll(listOfTasks);

            this._SubnetInformation = new SubnetInformation(hostanzahl, newSubMask, anzIter, netzIdBasic, bcIdBasic,
                rangesNetzId, rangesBcId);

            this.OnSubnetFound(new SubnetFoundEventArgs(this._SubnetInformation));

            return this._SubnetInformation;
        }

        public async Task<SubnetInformation> GivenSubnets(int anzahlSubNetze)
        {
            int hostanzahl;
            int cidr_neu;
            int anzIter;
            string tmp;
            char pad = '0';
            string[] oldSubMask = GetOldSubMask().Split('.');

            byte[] netzIdBasic = GetNetzID(Convert.ToByte(GetIp()[0]), Convert.ToByte(GetIp()[1]), Convert.ToByte(GetIp()[2]), Convert.ToByte(GetIp()[3]), Convert.ToByte(oldSubMask[0]), Convert.ToByte(oldSubMask[1]), Convert.ToByte(oldSubMask[2]), Convert.ToByte(oldSubMask[3]));
            byte[] bcIdBasic = GetBcId(Convert.ToByte(oldSubMask[0]), Convert.ToByte(oldSubMask[1]), Convert.ToByte(oldSubMask[2]), Convert.ToByte(oldSubMask[3]), netzIdBasic[0], netzIdBasic[1], netzIdBasic[2], netzIdBasic[3]);

            // Anzahl Sub-Netze
            anzIter = (int)Math.Pow(2, Math.Ceiling(Math.Log(anzahlSubNetze, 2)));

            // Submaske neu
            cidr_neu = (int)(GetIpMaske() + Math.Ceiling(Math.Log(anzahlSubNetze, 2)));
            string newSubMask = ShowNewSubMask(cidr_neu, pad);

            // Hostanzahl für Netzmaske alt
            hostanzahl = (int)Math.Pow(2, 32 - cidr_neu);

            int s = (int)Math.Pow(2, (32 - cidr_neu));
            tmp = BinDecConverter.UmrechnerHin(s);
            tmp = tmp.Remove(0, 1);

            listOfTasks.Clear();
            rangesNetzId = null;
            rangesBcId = null;
            rangesNetzId = new string[anzIter];
            rangesBcId = new string[anzIter];

            for (int i = 0; i < threads; i++)
            {
                listOfTasks.Add(SubnettingNetzId(tmp, Convert.ToInt16(GetIpBasic()[0]), Convert.ToInt16(GetIpBasic()[1]), Convert.ToInt16(GetIpBasic()[2]), Convert.ToInt16(GetIpBasic()[3]), anzIter * i / threads, anzIter * (i + 1) / threads));
                listOfTasks.Add(SubnettingBcId(tmp, Convert.ToInt16(GetIpBasic()[0]), Convert.ToInt16(GetIpBasic()[1]), Convert.ToInt16(GetIpBasic()[2]), Convert.ToInt16(GetIpBasic()[3]), anzIter * i / threads, anzIter * (i + 1) / threads));
            }

            await Task.WhenAll(listOfTasks);

            this._SubnetInformation = new SubnetInformation(hostanzahl, newSubMask, anzIter, netzIdBasic, bcIdBasic,
                rangesNetzId, rangesBcId);

            this.OnSubnetFound(new SubnetFoundEventArgs(this._SubnetInformation));

            return this._SubnetInformation;
        }

        private static async Task<string[]> SubnettingNetzId(string tmp, int ipBasic1, int ipBasic2, int ipBasic3, int ipBasic4, int lowerInt, int upperInt)
        {
            char pad = '0';

            await Task.Run(() =>
            {
                for (int i = lowerInt; i < upperInt; i++)
                {
                    rangesNetzId[i] = (String.Format("{0}.{1}.{2}.{3}",
                        ipBasic1 + BinDecConverter.UmrechnerRueck((BinDecConverter.UmrechnerHin(i) + tmp).PadLeft(32, pad).Substring(0, 8)),
                        ipBasic2 + BinDecConverter.UmrechnerRueck((BinDecConverter.UmrechnerHin(i) + tmp).PadLeft(32, pad).Substring(8, 8)),
                        ipBasic3 + BinDecConverter.UmrechnerRueck((BinDecConverter.UmrechnerHin(i) + tmp).PadLeft(32, pad).Substring(16, 8)),
                        ipBasic4 + BinDecConverter.UmrechnerRueck((BinDecConverter.UmrechnerHin(i) + tmp).PadLeft(32, pad).Substring(24, 8))));
                }
            });

            return rangesNetzId;
        }

        private static async Task<string[]> SubnettingBcId(string tmp, int ipBasic1, int ipBasic2, int ipBasic3, int ipBasic4, int lowerInt, int upperInt)
        {
            string max = "";
            char pad = '0';

            foreach (char item in tmp)
                max += 1;

            await Task.Run(() =>
            {
                for (int i = lowerInt; i < upperInt; i++)
                {
                    rangesBcId[i] = (String.Format("{0}.{1}.{2}.{3}",
                        ipBasic1 + BinDecConverter.UmrechnerRueck((BinDecConverter.UmrechnerHin(i) + max).PadLeft(32, pad).Substring(0, 8)),
                        ipBasic2 + BinDecConverter.UmrechnerRueck((BinDecConverter.UmrechnerHin(i) + max).PadLeft(32, pad).Substring(8, 8)),
                        ipBasic3 + BinDecConverter.UmrechnerRueck((BinDecConverter.UmrechnerHin(i) + max).PadLeft(32, pad).Substring(16, 8)),
                        ipBasic4 + BinDecConverter.UmrechnerRueck((BinDecConverter.UmrechnerHin(i) + max).PadLeft(32, pad).Substring(24, 8))));
                }
            });

            return rangesBcId;
        }

        private string ShowNewSubMask(int cidr_neu, char pad)
        {
            string ones = "";

            for (int i = 0; i < cidr_neu; i++)
                ones += 1;

            ones = ones.PadRight(32, pad);
            byte okt1SubMask = Convert.ToByte(BinDecConverter.UmrechnerRueck(ones.Substring(0, 8)));
            byte okt2SubMask = Convert.ToByte(BinDecConverter.UmrechnerRueck(ones.Substring(8, 8)));
            byte okt3SubMask = Convert.ToByte(BinDecConverter.UmrechnerRueck(ones.Substring(16, 8)));
            byte okt4SubMask = Convert.ToByte(BinDecConverter.UmrechnerRueck(ones.Substring(24, 8)));

            return $"{okt1SubMask}.{okt2SubMask}.{okt3SubMask}.{okt4SubMask}/{cidr_neu}";
        }

        // Events
        public delegate void SubnetFoundEventHandler(object sender, SubnetFoundEventArgs e);

        public event SubnetFoundEventHandler SubnetFound;

        protected void OnSubnetFound(SubnetFoundEventArgs e)
        {
            this.SubnetFound?.Invoke(this, e);
        }
    }

    public class SubnetFoundEventArgs : EventArgs
    {
        public SubnetFoundEventArgs(SubnetInformation SubnetInformation)
        {
            this.SubnetInformation = SubnetInformation;
        }

        public SubnetInformation SubnetInformation { get; }
    }

    public class SubnetInformation
    {
        public SubnetInformation(int Hostanzahl, string Submask, int Iterationen, byte[] NetzID, byte[] BCID, string[] RangesNetzId, string[] RangesBcId)
        {
            this.Hostanzahl = Hostanzahl;
            this.Submask = Submask;
            this.Iterationen = Iterationen;
            this.NetzID = NetzID;
            this.BCID = BCID;
            this.RangesNetzId = RangesNetzId;
            this.RangesBcId = RangesBcId;
        }

        public int Hostanzahl { get; }

        public string Submask { get; }

        public int Iterationen { get; }

        public byte[] NetzID { get; }

        public byte[] BCID { get; }

        public string[] RangesNetzId { get; }

        public string[] RangesBcId { get; }
    }
}