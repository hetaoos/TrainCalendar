using Ical.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using TrainCalendar.Data;

namespace TrainCalendar.Controllers
{
    /// <summary>
    /// 日历
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.ControllerBase" />
    [Route("ical")]
    [ApiController]
    public class iCalController : ControllerBase
    {
        private ApplicationDbContext db;
        private ILogger log;

        public iCalController(ApplicationDbContext db, ILogger<iCalController> log)
        {
            this.db = db;
            this.log = log;
        }

        // GET api/ical/5
        [HttpGet("{id}")]
        public ActionResult Get(string id, string name = null, int day = 30)
        {
            if (string.IsNullOrWhiteSpace(id))
                return this.BadRequest("邮箱地址不正确。");
            id = id.ToLower();
            var d = DateTime.Now.Date.AddDays(-day);

            var names = name?.Split("，,|".ToArray(), StringSplitOptions.RemoveEmptyEntries).Distinct().Select(o => o.Trim()).ToList();
            var q = db.GetCollection<Ticket>().Find(o => o.emails.Contains(id) && o.state != TicketStateEnums.refund && o.received >= d);
            if (string.IsNullOrWhiteSpace(name) == false)
            {
                q = q.Where(o => names.Contains(o.name));
            }
            var tickets = q.OrderBy(o => o.from.time).ToList();
            var calendar = tickets.ToCalendar();
            var bytesCalendar = calendar.ToBytes();
            log.LogInformation($"email={id}, name={name}, day={day} ticket={tickets.Count}");
            return File(bytesCalendar, "text/calendar", "event.ics");
        }
    }
}