using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobainSaver.DataBase
{
    internal class UserDonate
    {
        public int Id { get; set; }
        public long user_id { get; set; }
        public int stars { get; set; }
        public string date { get; set; }
    }
}
