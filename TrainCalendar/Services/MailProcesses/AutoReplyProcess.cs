using MailKit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TrainCalendar.Data;
using TrainCalendar.Services.TicketParsers;

namespace TrainCalendar.Services.MailProcesses
{
    public class AutoReplyProcess : IMailProcess
    {
        private readonly ApplicationDbContext db;
        private IOptions<AppSettings> options;
        private readonly RailsApiService railsApiService;
        private readonly TrainService trainService;
        private readonly ITicketParser ticketParser;
        private readonly PushMailService pushMailService;
        private readonly ILogger log;
        private const string mail_address = "12306@rails.com.cn";
        private readonly UserService userService;
        private static readonly Regex EmailExpression = new Regex(@"^([0-9a-zA-Z]+[-._+&])*[0-9a-zA-Z]+@([-0-9a-zA-Z]+[.])+[a-zA-Z]{2,6}$", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex regexNoreply = new Regex("no.*reply", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public AutoReplyProcess(ApplicationDbContext db,
            IOptions<AppSettings> options,
            UserService userService,
            RailsApiService railsApiService,
            TrainService trainService,
            ITicketParser ticketParser,
            PushMailService pushMailService,
            ILogger<AutoReplyProcess> log)
        {
            this.db = db;
            this.options = options;
            this.userService = userService;
            this.railsApiService = railsApiService;
            this.trainService = trainService;
            this.ticketParser = ticketParser;
            this.pushMailService = pushMailService;
            this.log = log;
        }

        /// <summary>
        /// Processes the specified MSG.
        /// </summary>
        /// <param name="msg">The MSG.</param>
        /// <param name="mail">The mail.</param>
        /// <param name="stoppingToken">The stopping token.</param>
        /// <returns></returns>
        public async Task Process(IMessageSummary msg, MimeMessage mail, CancellationToken stoppingToken)
        {
            //只处理接收邮件地址为本地址的
            var to_me = mail.To.Mailboxes.Any(o => o.Address.Contains(options.Value.imap.username, StringComparison.CurrentCultureIgnoreCase));

            if (to_me != true)
                return;

            Console.WriteLine("{0}", msg.UniqueId);

            var body = mail?.HtmlBody ?? mail?.TextBody;
            if (string.IsNullOrWhiteSpace(body))
                return;
            //添加邮件中的地址
            var replay_to = mail.From?.Mailboxes?.Where(o => CheckReplyAddress(o.Address)).ToList() ?? new List<MailboxAddress>();
            var email_address = EmailExpression.Matches(body).ToList().Select(o => o.Value.ToLower()).Distinct().Where(o => CheckReplyAddress(o))
                .Select(o => new MailboxAddress(o)).ToList();
            if (email_address?.Any() == true)
            {
                replay_to.AddRange(email_address);
            }

            if (replay_to.Any() == false)
                return;

            var received = mail.Date.DateTime;
            var dt = DateTime.Now;

            foreach (var addr in email_address)
            {
                var reply = new MimeMessage();
                reply.From.Add(new MailboxAddress(options.Value.imap.username));
                reply.To.AddRange(replay_to);

                if (!mail.Subject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase))
                    reply.Subject = "Re:" + mail.Subject;
                else
                    reply.Subject = mail.Subject;

                if (!string.IsNullOrEmpty(mail.MessageId))
                {
                    reply.InReplyTo = mail.MessageId;
                    foreach (var id in mail.References)
                        reply.References.Add(id);
                    reply.References.Add(mail.MessageId);
                }
                var user = await userService.GetOrCreateUserAsync(addr.Address);
                if (user == null)
                    continue;
                var url = $"https://12306.xware.io/ical/{user.token}";
                var html = $@"<p>您的日历访问地址是：<br>
<a href=""url"">{url}<br>
<p>请注意保管。</p>";

                var alternative = new Multipart("alternative");
                alternative.Add(new TextPart("html")
                {
                    Text = html,
                });
                foreach (var b in mail.BodyParts)
                    alternative.Add(b);

                reply.Body = alternative;

                await pushMailService.PushAsync(reply);
            }
        }

        protected bool CheckReplyAddress(string addr) => !(string.IsNullOrWhiteSpace(addr) ||
                EmailExpression.IsMatch(addr) == false ||
                regexNoreply.IsMatch(addr) ||
                string.Compare(addr, options.Value.imap.username, true) == 0 ||
                string.Compare(addr, mail_address, true) == 0);

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