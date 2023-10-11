﻿using TicketNow.Domain.Interfaces.Entities;

namespace TicketNow.Domain.Entities
{
    public class SeedHistory : IEntity<int>
    {
        public SeedHistory(Seed seed)
        {
            SeedId = seed.GetType().Name;
            RunDate = DateTime.Now;
        }

        public SeedHistory()
        {
        }

        public string SeedId { get; set; }
        public DateTime RunDate { get; private set; }
        public int Id { get; set; }
    }
}
