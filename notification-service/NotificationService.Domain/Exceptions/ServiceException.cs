﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Domain.Exceptions
{
    public class ServiceException : NotificationDomainException
    {
        public ServiceException(string message,Exception ex) : base(message+ex) 
        {
            
        }
    }
}
