using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Resources;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using System.Security;
using System.Threading;
using System.Security.Principal;
using System.Security.Permissions;
using System.Reflection;

namespace RCommon
{
    /// <summary>
    /// Serves as the base class for application-specific exceptions, automatically capturing
    /// environment information such as machine name, thread identity, and AppDomain name.
    /// </summary>
    /// <remarks>
    /// All constructors invoke <see cref="InitializeEnvironmentInformation"/> to populate
    /// diagnostic properties. Security exceptions during environment probing are silently
    /// caught and the corresponding property is set to "Permission Denied".
    /// </remarks>
    [Serializable]
    public class BaseApplicationException : ApplicationException
    {
        #region Fields

        private string machineName = string.Empty;
        private string appDomainName = string.Empty;
        private string threadIdentity = string.Empty;
        private string windowsIdentity = string.Empty;
        private DateTime createdDateTime = DateTime.Now;

        // Collection provided to store any extra information associated with the exception.
        private NameValueCollection additionalInformation = new NameValueCollection();

        #endregion

        #region Properties

        /// <summary>
        /// Machine name where the exception occurred.
        /// </summary>
        public string MachineName
        {
            get { return machineName; }
        }

        /// <summary>
        /// Date and Time the exception was created.
        /// </summary>
        public DateTime CreatedDateTime
        {
            get { return createdDateTime; }
        }

        /// <summary>
        /// AppDomain name where the exception occurred.
        /// </summary>
        public string AppDomainName
        {
            get { return appDomainName; }
        }

        /// <summary>
        /// Identity of the executing thread on which the exception was created.
        /// </summary>
        public string ThreadIdentityName
        {
            get { return threadIdentity; }
        }

        /// <summary>
        /// Windows identity under which the code was running.
        /// </summary>
        public string WindowsIdentityName
        {
            get { return windowsIdentity; }
        }

        /// <summary>
        /// Collection allowing additional information to be added to the exception.
        /// </summary>
        public NameValueCollection AdditionalInformation
        {
            get { return additionalInformation; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor with no params.
        /// </summary>
        public BaseApplicationException()
            : base()
        {
            InitializeEnvironmentInformation();
        }

        /// <summary>
        /// Constructor allowing the Message property to be set.
        /// </summary>
        /// <param name="message">String setting the message of the exception.</param>
        public BaseApplicationException(string message)
            : base(message)
        {
            InitializeEnvironmentInformation();
        }

        /// <summary>
        /// Constructor allowing the Message and InnerException property to be set.
        /// </summary>
        /// <param name="message">String setting the message of the exception.</param>
        /// <param name="inner">Sets a reference to the InnerException.</param>
        public BaseApplicationException(string message, Exception inner)
            : base(message, inner)
        {
            InitializeEnvironmentInformation();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialization function that gathers environment information safely.
        /// </summary>
        private void InitializeEnvironmentInformation()
        {
            try
            {
                machineName = Environment.MachineName;
            }
            catch (SecurityException)
            {
                machineName = "Permission Denied";

            }
            catch
            {
                machineName = "Permission Denied";
            }

            try
            {
                if (Thread.CurrentPrincipal != null)
                {
                    threadIdentity = Thread.CurrentPrincipal.Identity?.Name ?? string.Empty;
                }
            }
            catch (SecurityException)
            {
                threadIdentity = "Permission Denied";
            }
            catch
            {
                threadIdentity = "Permission Denied";
            }

            try
            {
                if (Thread.CurrentPrincipal != null)
                {
                    windowsIdentity = Thread.CurrentPrincipal.Identity?.Name ?? string.Empty;
                }
                
            }
            catch (SecurityException)
            {
                windowsIdentity = "Permission Denied";
            }
            catch
            {
                windowsIdentity = "Permission Denied";
            }

            try
            {
                appDomainName = AppDomain.CurrentDomain.FriendlyName;
            }
            catch (SecurityException)
            {
                appDomainName = "Permission Denied";
            }
            catch
            {
                appDomainName = "Permission Denied";
            }
        }

        #endregion
    }
}
