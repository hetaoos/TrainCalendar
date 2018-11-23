using MailKit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TrainCalendar.Data;
using TrainCalendar.Services.TicketParsers;

namespace TrainCalendar.Services.MailProcesses
{
    public class TicketProcess : IMailProcess
    {
        private readonly ApplicationDbContext db;
        private IOptions<AppSettings> options;
        private readonly RailsApiService railsApiService;
        private readonly TrainService trainService;
        private readonly ITicketParser ticketParser;
        private readonly PushMailService pushMailService;
        private readonly LiteDB.LiteCollection<Ticket> Tickets;
        private readonly ILogger log;
        private const string mail_address = "12306@rails.com.cn";
        private readonly UserService userService;

        public TicketProcess(ApplicationDbContext db,
            IOptions<AppSettings> options,
            UserService userService,
            RailsApiService railsApiService,
            TrainService trainService,
            ITicketParser ticketParser,
            PushMailService pushMailService,
            ILogger<TicketProcess> log)
        {
            this.db = db;
            this.options = options;
            this.userService = userService;
            this.railsApiService = railsApiService;
            this.trainService = trainService;
            this.ticketParser = ticketParser;
            this.pushMailService = pushMailService;
            this.log = log;

            Tickets = db.GetCollection<Ticket>();
            Tickets.EnsureIndex(o => o.no);
            Tickets.EnsureIndex(o => o.emails);
            Tickets.EnsureIndex(o => o.state);
            Tickets.EnsureIndex(o => o.name);
            Tickets.EnsureIndex(o => o.received);
        }

        public async Task Process(IMessageSummary msg, MimeMessage mail, CancellationToken stoppingToken)
        {
            Console.WriteLine("{0}", msg.UniqueId);

            var body = mail?.HtmlBody ?? mail?.TextBody;
            if (string.IsNullOrWhiteSpace(body))
                return;

            var tickets = ticketParser.Parse(body);
            if (tickets?.Any() != true)
                return;

            var from = mail.From.Mailboxes.Select(o => o.Address).Select(o => o.ToLower()).Where(o => o != mail_address && o.Contains('@')).Distinct().ToList();
            var to = mail.To.Mailboxes.Select(o => o.Address).Select(o => o.ToLower()).Where(o => o.Contains('@')).Distinct().ToList();
            to.RemoveAll(o => string.Compare(o, options.Value.imap.username, true) == 0);
            if (from.Any() == true)
                to.AddRange(from);

            if (to.Any() != true)
                return;
            var received = mail.Date.DateTime;
            var dt = DateTime.Now;
            foreach (var ticket in tickets)
            {
                ticket.received = received;
                ticket.created = dt;
                ticket.mid = msg.UniqueId.Id;
                ticket.emails = to;
                if (ticket.state == TicketStateEnums.refund)
                    ticket.cancelled = received;
                var trainSchedule = await trainService.GetTrainScheduleAsync(ticket.code, ticket.from.time, stoppingToken);
                if (trainSchedule != null)
                {
                    var start = trainSchedule.stations.FirstOrDefault(o => o.station_name == ticket.from.name);
                    var end = trainSchedule.stations.FirstOrDefault(o => o.station_name == ticket.to.name);
                    ticket.to.time = (end?.arrive_time ?? end?.start_time) ?? ticket.from.time.AddHours(1);
                }
                log.LogInformation($"id={msg.UniqueId} {string.Join(',', to)} {ticket.ToString()}");
            }
            var first = tickets[0];

            //退票/改签，需要找到之前的记录进行标记
            if (first.state == TicketStateEnums.refund || first.state == TicketStateEnums.change)
            {
                List<Ticket> updates = new List<Ticket>();
                var old_tockets = Tickets.Find(o => o.no == first.no && o.received < first.received && o.state != TicketStateEnums.refund).ToList();
                if (old_tockets.Any())
                {
                    foreach (var ticket in tickets)
                    {
                        var old_tocket = old_tockets.FirstOrDefault(o => o.name == ticket.name && (StationNameComparator(o.from.name, ticket.from.name) || StationNameComparator(o.to.name, ticket.to.name)));
                        if (old_tocket != null)
                        {
                            old_tocket.cancelled = received;
                            updates.Add(old_tocket);
                            log.LogInformation($"cancelled: {old_tocket}");
                        }
                    }
                }
                if (updates.Any())
                    Tickets.Update(updates);
            }
            Tickets.InsertBulk(tickets);

            await pushMailService.PushAsync(tickets);
        }

        protected char[] trim_chars = "站东南西北".ToArray();

        /// <summary>
        /// 判断是不是同一个城市的车站
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected bool StationNameComparator(string a, string b)
        {
            if (a == b)
                return true;
            a = a.Trim(trim_chars);
            b = b.Trim(trim_chars);
            if (a.StartsWith(b) || b.StartsWith(a))
                return true;
            return false;
        }
    }
}