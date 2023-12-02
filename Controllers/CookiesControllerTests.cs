using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using scriptbuster.dev.Controllers.CookieConsentAPI;
using scriptbuster.dev.Services.CookieService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace scriptbuster.dev_UnitTests.Controllers
{
    internal class CookiesControllerTests
    {
        [Test]
        public void Consent_ModelIsNotValid_ReturnBadRequest()
        {
            var mockCookieConsent = new Mock<ICookieConsent>();
            var mockLogger = new Mock<ILogger<CookiesController>>();
            var controller = new CookiesController(mockLogger.Object, mockCookieConsent.Object);

            var track = new UserTrackingCookies
            {
                HasConsent = false,
                NonEssentialCookies = default
            };

            controller.ModelState.AddModelError("error", "customError");
            //act
            var result =  controller.Consent(track) as BadRequestObjectResult;

            //assert
            Assert.That(result?.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            mockCookieConsent.Verify(x => x.SetUpCookies(It.IsAny<UserTrackingCookies>()),Times.Never());
        }
        [Test]
        public void Consent_CanSetUpCookies_ReturnOkResult()
        {
            var mockCookieConsent = new Mock<ICookieConsent>();
            var mockLogger = new Mock<ILogger<CookiesController>>();
            var controller = new CookiesController(mockLogger.Object, mockCookieConsent.Object);

            var track = new UserTrackingCookies
            {
                HasConsent = false,
                NonEssentialCookies = new()
            };

            //act
            var result = controller.Consent(track) as OkObjectResult;

            //assert
            Assert.That(result?.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
            mockCookieConsent.Verify(x => x.SetUpCookies(It.IsAny<UserTrackingCookies>()), Times.Once());
        }
    }
}
