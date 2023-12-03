using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.Options;
using scriptbuster.dev.Services.CookieService;
using scriptbuster.dev.ViewComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scriptbuster.dev_UnitTests.ViewComponents
{
    internal class CookieBannerVCTests
    {
        [Test]
        public void Invoke_CanCreateConsentLinkAndReturnNonEssentialCookies_ReturnView()
        {
            //arrange
            var nonEssentialCookies = new NonEssentialCookies
            {
                Cookies = new Dictionary<string, bool>
                {
                    { "Cookie1", true },
                    { "Cookie2", false }
                }
            };
            var mockOptions = new Mock<IOptions<NonEssentialCookies>>();
            mockOptions.SetupGet(x => x.Value).Returns(nonEssentialCookies);
            var mockHelper = new Mock<IUrlHelper>();
            mockHelper.Setup(x => x.Action(It.IsAny<UrlActionContext>())).Returns("/consent/test");

            var vc = new CookieBannerVC(mockOptions.Object);
            vc.Url = mockHelper.Object;

            //act
            var result = vc.Invoke() as ViewViewComponentResult;
            var model = result?.ViewData?.Model as Dictionary<string,bool> ?? new();

            //assert
            Assert.That(result?.ViewData?["ConsentLink"], Is.EqualTo("/consent/test"));
            Assert.That(model["Cookie1"], Is.True);
            Assert.That(model["Cookie2"], Is.False);
        }



    }
}
