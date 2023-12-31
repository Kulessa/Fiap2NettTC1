﻿using TicketNow.Domain.Enums;
using TicketNow.Domain.Interfaces.Entities;

namespace TicketNow.Domain.Entities
{
    public class Order : BaseEntity, IEntity<int>
    {
        public int UserId { get; set; }
        public int EventId { get; set; }
        public int PaymentId { get; set; }
        public OrderStatusEnum Status { get; set; }
        public PaymentStatusEnum PaymentStatus { get; set; }
        public int PaymentMethod { get; set; }
        public long Tickets { get; set; }        
        public decimal Price { get; set; }
        public virtual User User { get; set; }
        public virtual Event Event { get; set; }
        public virtual ICollection<OrderItem> OrderItens { get; set; }
    }
}