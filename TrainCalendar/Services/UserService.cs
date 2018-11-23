using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TrainCalendar.Data;

namespace TrainCalendar.Services
{
    public class UserService
    {
        private readonly ApplicationDbContext db;
        private readonly IMemoryCache cache;
        private readonly LiteDB.LiteCollection<User> Users;
        private readonly ILogger log;
        private static readonly string cache_key = typeof(UserService).FullName;
        private static readonly NamedLockerAsync locker = new NamedLockerAsync();

        public UserService(ApplicationDbContext db,
            IMemoryCache cache,
            ILogger<UserService> log)
        {
            this.db = db;
            this.cache = cache;
            this.log = log;

            Users = db.GetCollection<User>();
            Users.EnsureIndex(o => o.email, true);
            Users.EnsureIndex(o => o.token, true);
        }

        /// <summary>
        /// 获取或创建用户
        /// </summary>
        /// <param name="email">The email.</param>
        /// <returns></returns>
        public async Task<User> GetOrCreateUserAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;
            email = email.ToLower();
            using (await locker.LockAsync(email))
            {
                var user = Users.FindById(email);
                if (user == null)
                {
                    user = new User()
                    {
                        created = DateTime.Now,
                        email = email.ToLower(),
                        token = Guid.NewGuid().ToString("N").ToLower(),
                        reply = true,
                    };
                    Users.Upsert(user);
                }
                return user;
            }
        }
    }
}