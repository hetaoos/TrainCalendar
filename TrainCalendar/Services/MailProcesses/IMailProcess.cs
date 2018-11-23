using MailKit;
using MimeKit;
using System.Threading;
using System.Threading.Tasks;

namespace TrainCalendar.Services.MailProcesses
{
    /// <summary>
    /// 邮件处理流程
    /// </summary>
    public interface IMailProcess
    {
        Task Process(IMessageSummary msg, MimeMessage mail, CancellationToken stoppingToken);
    }
}