using Core.Entities;

namespace WebUI.Models
{
    public class ContactViewModel
    {
        public IncomingMessage IncomingMessage { get; set; } = new IncomingMessage();

        public IEnumerable<Contact>? Contacts { get; set; }
    }
}
