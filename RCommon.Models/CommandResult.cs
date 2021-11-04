
using RCommon.Validation;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.Models
{
    /// <summary>
    /// This class encapsulates a series of results stemming from validation, exception handling, and general
    /// result presentation. The concept for this is that we seek a friendly (and serializable) way to present error messages,
    /// exception messages, or method/command results while still allowing the presentation layer to decide what to do with the information.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CommandResult<TResult>
    {
        private ApplicationException _exception = null;
        private bool _hasException = false;

        public CommandResult()
        {
            this.ValidationResult = new ValidationResult();
            this.Message = string.Empty;
        }
        public CommandResult(TResult result, ValidationResult validationResult)
        {
            Guard.Against<ArgumentNullException>(result != null, "result cannot be a null value");
            Guard.Against<ArgumentNullException>(validationResult != null, "validationResult cannot be a null value");
            this.DataResult = result;
            this.ValidationResult = validationResult;
        }

        public TResult DataResult { get; set; }
        public ValidationResult ValidationResult { get; set; }
        public ApplicationException Exception
        {
            get
            {
                return _exception;
            }
            set
            {
                _exception = value;
                this.HasException = true;
                //this.ValidationResult.IsValid = false;
            }
        }
        public bool HasException { get => _hasException; set { _hasException = value; } }
        public string Message { get; set; }
    }

}
