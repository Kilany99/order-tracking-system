

namespace OrderService.Domain.Exceptions;


    public class OrderDomainException : Exception
    {
        public OrderDomainException(string message) : base(message) { }
    }
