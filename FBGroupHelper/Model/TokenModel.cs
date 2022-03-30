using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBGroupHelper.Model
{
    public class TokenModel
    {
        public bool result { get; set; }
        public string code { get; set; }
        public string message { get; set; }
        public string token { get; set; }
    }
}
