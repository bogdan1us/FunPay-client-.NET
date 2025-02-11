
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunPay_for_.net
{
    public class Config
    {
        public string GoldenKey { get; set; }
        public Dictionary<string, string> Commands { get; set; } = new Dictionary<string, string>();
    }
}
