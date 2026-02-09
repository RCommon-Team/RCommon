using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Emailing
{
    /// <summary>
    /// Event arguments for email-related events, carrying the <see cref="System.Net.Mail.MailMessage"/> that was sent.
    /// </summary>
    public class EmailEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmailEventArgs"/> class.
        /// </summary>
        /// <param name="mailMessage">The mail message associated with this event.</param>
        public EmailEventArgs(MailMessage mailMessage)
        {
            MailMessage=mailMessage;
        }

        /// <summary>
        /// Gets the <see cref="System.Net.Mail.MailMessage"/> associated with this event.
        /// </summary>
        public MailMessage MailMessage { get; }
    }
}
