using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobainSaver.DataBase
{
    internal class UserLanguage
    {
        public int Id { get; set; }
        public long chat_id { get; set; }
        public string language { get; set; }
    }
}
