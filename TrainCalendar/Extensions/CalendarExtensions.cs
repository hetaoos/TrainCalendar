using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TrainCalendar.Data;

namespace Ical.Net
{
    public static class CalendarExtensions
    {
        public static CalendarSerializer serializer = new CalendarSerializer(new SerializationContext());

        public static CalendarEvent ToCalendarEvent(this Ticket ticket)
        {
            var e = new CalendarEvent
            {
                Start = new CalDateTime(ticket.from.time),
                End = new CalDateTime(ticket.to.time),
                Created = new CalDateTime(ticket.received),
                Summary = $"{ticket.code} {ticket.from.name}-{ticket.to.name} {ticket.seat}",
                Description = $" {ticket.no} {ticket.name} {ticket.from?.time:yyyy-MM-dd HH:mm} {ticket.from?.name}-{ticket.to?.name} {ticket.code} {ticket.seat}",
                Location = ticket.from.name,
                LastModified = new CalDateTime(ticket.cancelled ?? ticket.received),
                Uid = $"{ticket.id}@xware.io",
            };
            if (ticket.cancelled != null)
                e.Status = "CANCELLED";
            e.Properties.Add(new CalendarProperty(nameof(ticket.no), ticket.no));
            e.Properties.Add(new CalendarProperty(nameof(ticket.name), ticket.name));
            e.Properties.Add(new CalendarProperty(nameof(ticket.from), ticket.from.name));
            e.Properties.Add(new CalendarProperty(nameof(ticket.to), ticket.to.name));
            return e;
        }

        public static Calendar ToCalendar(this IEnumerable<Ticket> tickets)
        {
            var calendar = new Calendar();
            calendar.ProductId = "-//xware.io//Train Calendar 1.0//EN";
            calendar.AddProperty("X-WR-CALNAME", "铁路行程");
            calendar.AddTimeZone(new VTimeZone("Asia/Shanghai"));

            foreach (var ticket in tickets.OrderBy(o => o.from.time))
            {
                var e = ticket.ToCalendarEvent();
                calendar.Events.Add(e);
            }

            return calendar;
        }

        public static byte[] ToBytes(this Calendar calendar) => Encoding.UTF8.GetBytes(serializer.SerializeToString(calendar));
    }
}