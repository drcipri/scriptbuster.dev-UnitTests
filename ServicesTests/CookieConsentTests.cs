using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using scriptbuster.dev.Services.CookieService;
using scriptbuster.dev.Services.SessionService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scriptbuster.dev_UnitTests.ServicesTests
{
    internal class CookieConsentTests
    {
        private  Mock<ISessionService> _sessionService;
        private  Mock<ILogger<CookieConsent>> _logger;
        private  Mock<IHttpContextAccessor> _httpContextAccessor;
        private  NonEssentialCookies? _nonEssentialCookies;
        private  ICookieConsent _cookieConsent;

        [SetUp]
        public void SetUp()
        {
            _sessionService = new Mock<ISessionService>();
            _logger = new Mock<ILogger<CookieConsent>>();
            _httpContextAccessor = new Mock<IHttpContextAccessor>();

            var mockIOptions = new Mock<IOptions<NonEssentialCookies>>();
            var nonEssentialCookies = new NonEssentialCookies
            {
                Cookies = new Dictionary<string, bool>
                {
                    { "Cookie1FromAppSettings", true },
                    { "Cookie2FromAppSettings", false }
                }
            };
            mockIOptions.SetupGet(x => x.Value).Returns(nonEssentialCookies);


            _cookieConsent = new CookieConsent(_sessionService.Object,
                                               mockIOptions.Object,
                                               _logger.Object,
                                               _httpContextAccessor.Object);
        }

        [Test]
        public void SetUpCookies_UserCookieTrackIsNull_ThrowsArgumentNullException()
        {
            //arrange
            UserTrackingCookies userTrackingCookies = default!;

            //skip act it gonna trow exception

            //assert
            Assert.Throws<ArgumentNullException>(() => _cookieConsent.SetUpCookies(userTrackingCookies), "No tracking cookies provided");
            _httpContextAccessor.Verify(x => x.HttpContext, Times.Never());
        }

        [Test]
        public void SetUpCookies_HasGlobalConsentButTheCookiesAreNull_SoTheSessionWOntHaveTheCookiesConsent_AndDefaultSetupIsGoingToBeUsedWhenRetrieveingTheNonEssentialCookiesConsent()
        {
            //arrange
            UserTrackingCookies userTrackingCookies = new UserTrackingCookies 
            {
                HasConsent = true 
            };
            
            var mockHttpContext = new Mock<HttpContext>();
            var mockFeatures = new Mock<IFeatureCollection>();
            var mockConsent = new Mock<ITrackingConsentFeature>();
            mockHttpContext.SetupGet(x => x.Features).Returns(mockFeatures.Object);
            mockFeatures.Setup(x => x.Get<ITrackingConsentFeature>()).Returns(mockConsent.Object);
            _httpContextAccessor.SetupGet(x => x.HttpContext).Returns(mockHttpContext.Object);

            //act
            _cookieConsent.SetUpCookies(userTrackingCookies);

            //assert
            
            _httpContextAccessor.Verify(x => x.HttpContext, Times.Once());
            _sessionService.Verify(x => x.AddObject(It.IsAny<string>(),It.IsAny<Dictionary<string,bool>>()), Times.Never());
            mockConsent.Verify(x => x.GrantConsent(), Times.Once());
        }
        [Test]
        public void SetUpCookies_HasGlobalConsent_AndNonEssentialCookiesConsentAreAddedToSession()
        {
            //arrange
            UserTrackingCookies userTrackingCookies = new UserTrackingCookies
            {
                 HasConsent = true,
                 NonEssentialCookies = new Dictionary<string, bool>
                 {
                     { "CookieFromFrontEnd1", true },
                     { "CookieFromFrontEnd2", false }
                 }
            };
            var mockHttpContext = new Mock<HttpContext>();
            var mockFeatures = new Mock<IFeatureCollection>();
            var mockConsent = new Mock<ITrackingConsentFeature>();
            mockHttpContext.SetupGet(x => x.Features).Returns(mockFeatures.Object);
            mockFeatures.Setup(x => x.Get<ITrackingConsentFeature>()).Returns(mockConsent.Object);
            _httpContextAccessor.SetupGet(x => x.HttpContext).Returns(mockHttpContext.Object);

            //act
            _cookieConsent.SetUpCookies(userTrackingCookies);

            //assert

            _httpContextAccessor.Verify(x => x.HttpContext, Times.Once());
            _sessionService.Verify(x => x.AddObject(It.IsAny<string>(), userTrackingCookies.NonEssentialCookies),Times.Once());
            mockConsent.Verify(x => x.GrantConsent(), Times.Once());
        }
        [Test]
        public void SetUpCookies_DoesNotHaveConsent_ConsentIsWidrawn()
        {
            //i set up the fetch in JS so you wont have this option, global consent will always be granted, but the client can choose what cookies can be used
            //as you can se in the tests above.
            //so this is unlikely to happen but the server is ready for this option as well
            //arrange
            UserTrackingCookies userTrackingCookies = new UserTrackingCookies
            {
                HasConsent = false,
                NonEssentialCookies = new Dictionary<string, bool>
                 {
                     { "CookieFromFrontEnd1", true },
                     { "CookieFromFrontEnd2", false }
                 }
            };
            var mockHttpContext = new Mock<HttpContext>();
            var mockFeatures = new Mock<IFeatureCollection>();
            var mockConsent = new Mock<ITrackingConsentFeature>();
            mockHttpContext.SetupGet(x => x.Features).Returns(mockFeatures.Object);
            mockFeatures.Setup(x => x.Get<ITrackingConsentFeature>()).Returns(mockConsent.Object);
            _httpContextAccessor.SetupGet(x => x.HttpContext).Returns(mockHttpContext.Object);

            //act
            _cookieConsent.SetUpCookies(userTrackingCookies);

            //assert

            _httpContextAccessor.Verify(x => x.HttpContext, Times.Once());
            _sessionService.Verify(x => x.AddObject(It.IsAny<string>(), userTrackingCookies.NonEssentialCookies), Times.Never());
            mockConsent.Verify(x => x.GrantConsent(), Times.Never());
            mockConsent.Verify(x => x.WithdrawConsent(), Times.Once());
        }

        [Test]
        public async Task GetConsentedCookiesAsync_NonEssentialCookiesAreFoundInSession_ReturnClientConsentedCookies()
        {
            //arrange
            var nonEssentialCookies = new Dictionary<string, bool>
                 {
                     { "CookieFromFrontEnd1", true },
                     { "CookieFromFrontEnd2", false }
                 };
            _sessionService.Setup(x => x.FindKey("NonEssential_Cookies_Key")).Returns(true);
            _sessionService.Setup(x => x.GetObject<Dictionary<string, bool>>("NonEssential_Cookies_Key"))
                           .Returns(async () => await Task.FromResult(nonEssentialCookies));

            //act
            var result = await _cookieConsent.GetConsentedCookiesAsync();

            //assert
            Assert.That(result["CookieFromFrontEnd1"], Is.True);
            Assert.That(result["CookieFromFrontEnd2"], Is.False);
            _sessionService.Verify(x => x.GetObject<Dictionary<string, bool>>(It.IsAny<string>()), Times.Once());
        }
        [Test]
        public async Task GetConsentedCookiesAsync_NonEssentialCookiesAreNotFoundInSession_ReturnDefaultSetupFromAppSettings()
        {
            //arrange
            _sessionService.Setup(x => x.FindKey("NonEssential_Cookies_Key")).Returns(false); 

            //act
            var result = await _cookieConsent.GetConsentedCookiesAsync();

            //assert
            Assert.That(result["Cookie1FromAppSettings"], Is.True);
            Assert.That(result["Cookie2FromAppSettings"], Is.False);
            _sessionService.Verify(x => x.GetObject<Dictionary<string, bool>>(It.IsAny<string>()), Times.Never());
            _sessionService.Verify(x => x.FindKey(It.IsAny<string>()), Times.Once());
        }
        [Test]
        public void HasConsent_FeatureIsNUll_ReturnFalse()
        {
            //assert
            var mockHttpContext = new Mock<HttpContext>();
            var mockFeatures = new Mock<IFeatureCollection>();
            mockHttpContext.SetupGet(x => x.Features).Returns(mockFeatures.Object);
          
            ITrackingConsentFeature nullObj = default!;
            mockFeatures.Setup(x => x.Get<ITrackingConsentFeature>()).Returns(nullObj!);
            
            _httpContextAccessor.SetupGet(x => x.HttpContext).Returns(mockHttpContext.Object);

            //act
            var result = _cookieConsent.HasConsent();

            //assert
            Assert.That(result, Is.False);

        }
        [Test]
        public void HasConsent_ConsentIsTrue_ReturnTrue()
        {
            //assert
            var mockHttpContext = new Mock<HttpContext>();
            var mockFeatures = new Mock<IFeatureCollection>();
            mockHttpContext.SetupGet(x => x.Features).Returns(mockFeatures.Object);

            var mockTrack = new Mock<ITrackingConsentFeature>();
            mockFeatures.Setup(x => x.Get<ITrackingConsentFeature>()).Returns(mockTrack.Object);
            mockTrack.SetupGet(x => x.HasConsent).Returns(true);

            _httpContextAccessor.SetupGet(x => x.HttpContext).Returns(mockHttpContext.Object);

            //act
            var result = _cookieConsent.HasConsent();

            //assert
            Assert.That(result, Is.True);

        }
        [Test]
        public void HasConsent_ConsentIsFalse_ReturnFalse()
        {
            //assert
            var mockHttpContext = new Mock<HttpContext>();
            var mockFeatures = new Mock<IFeatureCollection>();
            mockHttpContext.SetupGet(x => x.Features).Returns(mockFeatures.Object);

            var mockTrack = new Mock<ITrackingConsentFeature>();
            mockFeatures.Setup(x => x.Get<ITrackingConsentFeature>()).Returns(mockTrack.Object);
            mockTrack.SetupGet(x => x.HasConsent).Returns(false);

            _httpContextAccessor.SetupGet(x => x.HttpContext).Returns(mockHttpContext.Object);

            //act
            var result = _cookieConsent.HasConsent();

            //assert
            Assert.That(result, Is.False);

        }
    }
}
