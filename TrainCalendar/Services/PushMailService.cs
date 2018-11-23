using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Collections.Generic;
using System.Threading.Tasks;
using TrainCalendar.Data;

namespace TrainCalendar.Services
{
    public class PushMailService
    {
        private readonly ApplicationDbContext db;
        private readonly IMemoryCache cache;
        private readonly LiteDB.LiteCollection<User> Users;
        private readonly ILogger log;
        private static readonly string cache_key = typeof(PushMailService).FullName;
        private static readonly NamedLockerAsync locker = new NamedLockerAsync();

        public PushMailService(ApplicationDbContext db,
            IMemoryCache cache,
            ILogger<PushMailService> log)
        {
            this.db = db;
            this.cache = cache;
            this.log = log;

            Users = db.GetCollection<User>();
        }

        /// <summary>
        /// 推送邮件提醒
        /// </summary>
        /// <param name="mail">The m.</param>
        /// <returns></returns>
        public Task PushAsync(List<Ticket> tickets)
        {
            return Task.CompletedTask;
        }

        internal Task PushAsync(MimeMessage reply)
        {
            return Task.CompletedTask;
        }
    }
}