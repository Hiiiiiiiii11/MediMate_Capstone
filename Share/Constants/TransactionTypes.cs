using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Share.Constants
{
    public static class TransactionTypes
    {
        public const string MoneyReceived = "IN";
        public const string InPackagePurchase = "IN_PACKAGE";
        public const string InSessionPayment = "IN_SESSION";
        public const string MoneySent = "OUT";
    };
}
