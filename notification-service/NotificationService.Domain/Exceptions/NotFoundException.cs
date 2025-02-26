using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Domain.Exceptions
{
    public class NotFoundException : NotificationDomainException
    {
        public NotFoundException(string message) :base(message) { }
    }
}
