using MailKit;
using MailKit.Net.Imap;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TrainCalendar.Data;
using TrainCalendar.Services.MailProcesses;

namespace TrainCalendar.Services
{
    public class MailboxMonitoringService : BackgroundServiceEx
    {
        private readonly ApplicationDbContext db;
        private IOptions<AppSettings> options;
        private ImapClient client;
        private Config cfg;
        private CancellationTokenSource cancellationTokenSourceIdle;
        private IEnumerable<IMailProcess> mailProcesses;

        public MailboxMonitoringService(ApplicationDbContext db,
            IEnumerable<IMailProcess> mailProcesses,
            IOptions<AppSettings> options,
            ILogger<MailboxMonitoringService> log)
            : base(180, log)
        {
            this.db = db;
            this.mailProcesses = mailProcesses;
            this.options = options;
            cfg = db.GetConfig<Config>(() => new Config());
            //cfg.max_mail_id = 0;
        }

        private CancellationToken stoppingToken;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            client?.Dispose();
            client = new ImapClient();
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            var isOk = await Connect(stoppingToken);
            if (isOk == false)
                return;
            this.stoppingToken = stoppingToken;
            var inbox = client.Inbox;
            inbox.Open(FolderAccess.ReadWrite);
            inbox.Subscribe();
            inbox.CountChanged += (sender, e) =>
            {
                var folder = (ImapFolder)sender;
                log.LogInformation($"The number of messages in {folder?.Name} has changed.");
                cancellationTokenSourceIdle?.Cancel(false);
            };

            var has = client.Capabilities.HasFlag(ImapCapabilities.Idle);
            log.LogInformation("waiting for mail.");
            while (stoppingToken.IsCancellationRequested == false)
            {
                await FetchNewMails();
                cancellationTokenSourceIdle?.Dispose();
                cancellationTokenSourceIdle = new CancellationTokenSource(TimeSpan.FromMinutes(3));
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSourceIdle.Token, stoppingToken))
                {
                    if (has)
                        await client.IdleAsync(linkedCts.Token, stoppingToken);
                    else
                    {
                        client.NoOp(linkedCts.Token);
                        await Task.Delay(TimeSpan.FromMinutes(3), linkedCts.Token);
                    }
                }
            }

            cancellationTokenSourceIdle?.Dispose();
            inbox.Close();
            client.Disconnect(true);
        }

        /// <summary>
        /// 获取新的邮件
        /// </summary>
        /// <returns></returns>
        private async Task FetchNewMails()
        {
            var inbox = client.Inbox;
            var uids = new UniqueIdRange(new UniqueId((uint)cfg.max_mail_id + 1), UniqueId.MaxValue);
            var old_max_mail_id = cfg.max_mail_id;

            foreach (var msg in await inbox.FetchAsync(uids, MessageSummaryItems.UniqueId | MessageSummaryItems.InternalDate | MessageSummaryItems.Headers, stoppingToken))
            {
                var mail = await inbox.GetMessageAsync(msg.UniqueId, stoppingToken);
                log.LogInformation($"{msg.UniqueId}: {mail.Subject}");
                if (mailProcesses?.Any() == true)
                    await Task.WhenAll(mailProcesses.Select(o => o.Process(msg, mail, stoppingToken)).ToArray());

                if (msg.Flags?.HasFlag(MessageFlags.Seen) != true)
                    await inbox.SetFlagsAsync(msg.UniqueId, MessageFlags.Seen, true, stoppingToken);
                cfg.max_mail_id = msg.UniqueId.Id;
            }

            if (cfg.max_mail_id > old_max_mail_id) //保存
                db.SetConfig(cfg);
        }

        public async Task<bool> Connect(CancellationToken stoppingToken)
        {
            var cfg = options.Value.imap;
            if (cfg == null)
            {
                log.LogCritical("imap服务未配置。");
                return false;
            }

            try
            {
                await client.ConnectAsync(cfg.host, cfg.port, cfg.ssl, stoppingToken);
                client.AuthenticationMechanisms?.Remove("XOAUTH");
                await client.AuthenticateAsync(cfg.username, cfg.password, stoppingToken);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                return false;
            }

            if (client.Capabilities.HasFlag(ImapCapabilities.Id))
            {
                var clientImplementation = new ImapImplementation
                {
                    Name = "com.tencent.foxmail",
                    Version = "7.2.9.79",
                    OSVersion = RuntimeInformation.OSDescription,
                    OS = RuntimeInformation.OSDescription,
                };
                var serverImplementation = client.Identify(clientImplementation);
            }

            return true;
        }

        public class Config
        {
            /// <summary>
            /// 最大邮件id
            /// </summary>
            public long max_mail_id { get; set; }
        }
    }
}