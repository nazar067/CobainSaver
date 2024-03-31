using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobainSaver.DataBase
{
    internal class BotCommand
    {
        public int Id { get; set; }
        public long chat_id { get; set; }
        public string command_type { get; set; }
        public string date { get; set; }
    }
}
