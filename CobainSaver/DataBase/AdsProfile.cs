using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobainSaver.DataBase
{
    internal class AdsProfile
    {
        public int Id { get; set; }
        public long chat_id{ get; set; }
        public string ads_name { get; set; }
        public string message{ get; set; }
        public bool is_active { get; set; }
        public bool is_activeAdmin { get; set; }
        public string start_date { get; set; }
        public string end_date { get; set; }
    }
}
