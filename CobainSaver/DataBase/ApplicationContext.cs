using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobainSaver.DataBase
{
    internal class ApplicationContext:DbContext
    {
        static string jsonString = System.IO.File.ReadAllText("source.json");
        static JObject jsonObjectAPI = JObject.Parse(jsonString);
        public DbSet<UserLink> UserLinks { get; set; } = null!;
        public DbSet<UserCommand> UserCommands { get; set; } = null!;
        public DbSet<UserReview> UserReviews { get; set; } = null!;
        public DbSet<BotCommand> BotCommands { get; set; } = null!;
        public DbSet<UserLanguage> UserLanguages { get; set; } = null!;
        public DbSet<ChatReview> ChatReviews { get; set; } = null!;
        public DbSet<AdsProfile> AdsProfiles { get; set; } = null!;
        public ApplicationContext()
        {
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(jsonObjectAPI["SqlConnection"][0].ToString());
        }
    }
}
