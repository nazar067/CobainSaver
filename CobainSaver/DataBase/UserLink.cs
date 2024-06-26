﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobainSaver.DataBase
{
    internal class UserLink
    {
        public int Id { get; set; }
        public long user_id { get; set; }
        public long chat_id { get; set; }
        public string link { get; set; }
        public long msg_id { get; set; }
        public string date { get; set; }
    }
}
