using LiteDB;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TrainCalendar.Data
{
    /// <summary>
    /// 车票
    /// </summary>
    public class Ticket
    {
        /// <summary>
        /// id
        /// </summary>
        [BsonId(true)]
        public ObjectId id { get; set; }

        /// <summary>
        /// 邮件id
        /// </summary>
        public long mid { get; set; }

        /// <summary>
        /// 邮箱id
        /// </summary>
        public List<string> emails { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public TicketStateEnums state { get; set; }

        /// <summary>
        /// 订单编号
        /// </summary>
        public string no { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 车次
        /// </summary>
        public string code { get; set; }

        /// <summary>
        /// 出发站
        /// </summary>
        public TicketStation from { get; set; }

        /// <summary>
        /// 到达站
        /// </summary>
        public TicketStation to { get; set; }

        /// <summary>
        /// 日历id，用于退票和改签时候删除日历
        /// </summary>
        public string eventid { get; set; }

        /// <summary>
        /// 座位
        /// </summary>
        public string seat { get; set; }

        /// <summary>
        /// 取消时间
        /// </summary>
        public DateTime? cancelled { get; set; }

        /// <summary>
        /// 邮件接收时间
        /// </summary>
        public DateTime received { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime created { get; set; }

        public override string ToString()
        {
            return $"{name} {from?.time:yyyy-MM-dd HH:mm} {from?.name}-{to?.name} {code} {seat} {no} {state}";
        }
    }

    /// <summary>
    /// 车票的车站信息
    /// </summary>
    public class TicketStation
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 出发或者到达时间
        /// </summary>
        public DateTime time { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"{name} {time:yyyy-MM-dd HH:mm}";
    }

    /// <summary>
    /// 票状态类型
    /// </summary>
    public enum TicketStateEnums
    {
        /// <summary>
        /// 预定
        /// </summary>
        [Description("预定")]
        booking,

        /// <summary>
        /// 改签
        /// </summary>
        [Description("改签")]
        change,

        /// <summary>
        /// 退票
        /// </summary>
        [Description("退票")]
        refund,
    }
}