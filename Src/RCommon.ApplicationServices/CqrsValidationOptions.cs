using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices
{
    public class CqrsValidationOptions
    {
        public CqrsValidationOptions()
        {
            ValidateQueries = false;
            ValidateCommands = false;
        }

        public bool ValidateQueries { get; set; }
        public bool ValidateCommands { get; set; }
    }
}
