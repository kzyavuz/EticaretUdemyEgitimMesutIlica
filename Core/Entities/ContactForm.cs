using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Entities
{
    public class ContactForm : IBaseEntity
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
    }
}
