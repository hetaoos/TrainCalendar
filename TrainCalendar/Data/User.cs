using LiteDB;
using System;

namespace TrainCalendar.Data
{
    /// <summary>
    /// 账户
    /// </summary>
    public class User
    {
        /// <summary>
        /// 邮箱 唯一
        /// </summary>
        [BsonId(false)]
        public string email { get; set; }

        /// <summary>
        /// 访问令牌
        /// </summary>
        public string token { get; set; } = Guid.NewGuid().ToString("N").ToLower();

        /// <summary>
        /// 是否回复邮件
        /// </summary>
        public bool reply { get; set; } = true;

        /// <summary>
        /// 创建日期
        /// </summary>
        public DateTime created { get; set; } = DateTime.Now;
    }
}