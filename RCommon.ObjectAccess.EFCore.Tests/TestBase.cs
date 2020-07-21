using Autofac;
using Autofac.Core;
using Autofac.Extras.CommonServiceLocator;
using CommonServiceLocator;
using Microsoft.Extensions.Configuration;
using RCommon.Configuration;
using RCommon.DependencyInjection;
using RCommon.DependencyInjection.Autofac;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.ObjectAccess.EFCore.Tests
{
    public abstract class TestBase
    {
        static IServiceLocator _serviceLocator;
        
        static object _configureLock = new object();
        AutofacContainerAdapter _containerAdapter;

        private IContainer _autofacContainer;
        
        public TestBase()
        {


            this.InitializeRCommon();
        }

        private void InitializeRCommon()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            this.Configuration = config.Build();

            if (_autofacContainer == null)
            {
                var builder = new ContainerBuilder();

                _autofacContainer = builder.Build();

                _serviceLocator = new AutofacServiceLocator(_autofacContainer);
                ServiceLocator.SetLocatorProvider(() => _serviceLocator);

                _containerAdapter = new AutofacContainerAdapter(builder);
                ConfigureRCommon.Using(_containerAdapter) // By default we'll be using Theadlocal storage since we're not under web request
                .WithStateStorage<DefaultStateStorageConfiguration>()
                .WithUnitOfWork<DefaultUnitOfWorkConfiguration>()
                .WithObjectAccess<EFCoreConfiguration>();

                

            }

            
        }


        /// <summary>
        /// Creates a simple web request so that we can test RCommon in web environment
        /// </summary>
        protected void CreateWebRequest()
        {
            string response = "my test response"; 
            TestWebRequest.RegisterPrefix("test", new TestWebRequestCreate()); 
            TestWebRequest request = TestWebRequestCreate.CreateTestRequest(response); 
            string url = "http://localhost://test"; 
            //ObjectUnderTest myObject = new ObjectUnderTest(); 
            //myObject.Url = url; 
            
            // DoStuff call the url with a request and then processes the 
            // response as set above myObject.DoStuff(); 
            string requestContent = request.ContentAsString(); 
            //Assert.AreEqual(expectedRequestContent, requestContent);
        }

        public IConfigurationRoot Configuration { get; private set; }
        public AutofacContainerAdapter ContainerAdapter { get => _containerAdapter; set => _containerAdapter = value; }
        public IContainer AutofacContainer { get => _autofacContainer; set => _autofacContainer = value; }
    }
}
