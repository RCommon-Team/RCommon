using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using RCommon.DependencyInjection.Microsoft;
using RCommon.TestBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using System.Text;

namespace RCommon.ExceptionHandling.EnterpriseLibraryCore.Tests
{
    [TestFixture()]
    public class EhabExceptionManagerIntegration : EhabTestBase
    {

        public EhabExceptionManagerIntegration() : base()
        {
            var services = new ServiceCollection();

            this.InitializeRCommon(services);
        }



        [OneTimeSetUp]
        public void InitialSetup()
        {
            this.Logger.LogInformation("Beginning Onetime setup", null);
            

        }

        [SetUp]
        public void Setup()
        {
            
            this.Logger.LogInformation("Beginning New Test Setup", null);


        }

        [TearDown]
        public void TearDown()
        {
            this.Logger.LogInformation("Tearing down Test", null);

        }

        [Test]
        public void Can_Run_Tests_In_Web_Environment()
        {
            this.CreateWebRequest();
            this.Can_run_BasePolicy();
        }

        private TException ThrowTestException<TException>()
        {
            var ex = Activator.CreateInstance<TException>();
            return ex;
        }


        [Test]
        public void Can_run_BasePolicy()
        {
            var manager = this.ServiceProvider.GetService<IExceptionManager>();
            Assert.Throws<GeneralException>(() => 
                manager.HandleException(this.ThrowTestException<ApplicationException>(), DefaultExceptionPolicies.BasePolicy)
            );
           
        }

        [Test]
        public void Can_run_BusinessWrapPolicy()
        {
            var manager = this.ServiceProvider.GetService<IExceptionManager>();
            Assert.Throws<BusinessException>(() =>
                manager.HandleException(this.ThrowTestException<ApplicationException>(), DefaultExceptionPolicies.BusinessWrapPolicy)
            );

        }

        [Test]
        public void Can_run_BusinessReplacePolicy()
        {
            var manager = this.ServiceProvider.GetService<IExceptionManager>();
            Assert.Throws<FriendlyBusinessException>(() =>
                manager.HandleException(this.ThrowTestException<BusinessException>(), DefaultExceptionPolicies.BusinessReplacePolicy)
            );

        }

        [Test]
        public void Can_run_ApplicationWrapPolicy()
        {
            var manager = this.ServiceProvider.GetService<IExceptionManager>();
            Assert.Throws<ApplicationTierException>(() =>
                manager.HandleException(this.ThrowTestException<ApplicationException>(), DefaultExceptionPolicies.ApplicationWrapPolicy)
            );

        }

        [Test]
        public void Can_run_ApplicationReplacePolicy()
        {
            var manager = this.ServiceProvider.GetService<IExceptionManager>();
            Assert.Throws<FriendlyApplicationException>(() =>
                manager.HandleException(this.ThrowTestException<ApplicationTierException>(), DefaultExceptionPolicies.ApplicationReplacePolicy)
            );

        }

        [Test]
        public void Can_run_SecurityReplacePolicy()
        {
            var manager = this.ServiceProvider.GetService<IExceptionManager>();
            Assert.Throws<FriendlySecurityException>(() =>
                manager.HandleException(this.ThrowTestException<SecurityException>(), DefaultExceptionPolicies.SecurityReplacePolicy)
            );

        }

        [Test]
        public void Can_run_PresentationReplacePolicy()
        {
            var manager = this.ServiceProvider.GetService<IExceptionManager>();
            Assert.DoesNotThrow(() =>
                manager.HandleException(this.ThrowTestException<FriendlyApplicationException>(), DefaultExceptionPolicies.PresentationReplacePolicy)
            );

        }
    }
}
