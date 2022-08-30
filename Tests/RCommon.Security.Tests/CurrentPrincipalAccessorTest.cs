using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using RCommon.Security.Claims;
using Shouldly;
using System.Collections.Generic;
using System.Security.Claims;

namespace RCommon.Security.Tests
{
    [TestFixture()]
    public class CurrentPrincipalAccessorTest : SecurityTestBase
    {
        private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;

        public CurrentPrincipalAccessorTest() :base()
        {
            var services = new ServiceCollection();
            this.InitializeRCommon(services);
            _currentPrincipalAccessor = this.ServiceProvider.GetRequiredService<ICurrentPrincipalAccessor>();
        }

        [Test]
        public void Should_Get_Changed_Principal_If()
        {
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(ClaimTypes.Name,"bob"),
                new Claim(ClaimTypes.NameIdentifier,"123456")
            }));

            var claimsPrincipal2 = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(ClaimTypes.Name,"lee"),
                new Claim(ClaimTypes.NameIdentifier,"654321")
            }));


            _currentPrincipalAccessor.Principal.ShouldBe(null);

            using (_currentPrincipalAccessor.Change(claimsPrincipal))
            {
                _currentPrincipalAccessor.Principal.ShouldBe(claimsPrincipal);

                using (_currentPrincipalAccessor.Change(claimsPrincipal2))
                {
                    _currentPrincipalAccessor.Principal.ShouldBe(claimsPrincipal2);
                }

                _currentPrincipalAccessor.Principal.ShouldBe(claimsPrincipal);
            }
            _currentPrincipalAccessor.Principal.ShouldBeNull();
        }
    }
}
