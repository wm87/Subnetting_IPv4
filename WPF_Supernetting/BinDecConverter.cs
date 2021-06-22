using System;

namespace Subnetting
{
    static class BinDecConverter
    {
        public static string UmrechnerHin(int zahl)
        {
            return Convert.ToString(zahl, 2);
        }

        public static int UmrechnerRueck(string zahl)
        {
            return Convert.ToInt32(zahl, 2);
        }
    }
}
