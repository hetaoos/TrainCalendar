namespace TrainCalendar
{
    public class AppSettings
    {
        /// <summary>
        /// 默认令牌
        /// </summary>
        public string token { get; set; } = "81f5bd225bee439fa961435991686955";

        /// <summary>
        /// imap服务
        /// </summary>
        public MailServerSettings imap { get; set; } = new MailServerSettings();

        /// <summary>
        /// smtp服务
        /// </summary>
        public MailServerSettings smtp { get; set; } = new MailServerSettings() { host = "smtp.exmail.qq.com", port = 465 };
    }

    public class MailServerSettings
    {
        public string host { get; set; } = "imap.exmail.qq.com";

        public int port { get; set; } = 993;

        public bool ssl { get; set; } = true;

        public string username { get; set; }

        public string password { get; set; }

    }
}