using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using scriptbuster.dev.Services.CookieService;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace scriptbuster.dev_UnitTests
{
    [TestFixture]
    internal class CookieServiceTests
    {
        private Mock<IHttpContextAccessor> _contextAccessor;
        private Mock<ILogger<CookieService>> _logger;
        private Mock<HttpContext> _mockHttpContext;
        private Mock<HttpRequest> _mockHttpRequest;
        private Mock<HttpResponse> _mockHttpResponse;
        private Mock<IRequestCookieCollection> _mockRequestCookieCollection;
        private Mock<IResponseCookies> _mockResponseCookies;
        private CookieService _cookieService;

        [SetUp]
        public void SetUp()
        {
            _logger = new Mock<ILogger<CookieService>>();
            _contextAccessor = new Mock<IHttpContextAccessor>();
            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpResponse = new Mock<HttpResponse>();
            _mockHttpRequest = new Mock<HttpRequest>();

            _mockRequestCookieCollection = new Mock<IRequestCookieCollection>();
            _mockRequestCookieCollection.Setup(cookies => cookies["TestKey1"]).Returns("ValueTest1");
            _mockRequestCookieCollection.Setup(cookies => cookies["TestKey2"]).Returns("ValueTest2");
            _mockRequestCookieCollection.Setup(cookies => cookies["TestKey3"]).Returns("ValueTest3");

            //get only the enumerator to mock an EMPTY dictionary.
            _mockRequestCookieCollection.Setup(cookies => cookies.GetEnumerator()).Returns(new Dictionary<string, string>().GetEnumerator());

            _mockResponseCookies = new Mock<IResponseCookies>();

            _contextAccessor.SetupGet(x => x.HttpContext).Returns(_mockHttpContext.Object);
            _contextAccessor.SetupGet(x => x.HttpContext!.Response).Returns(_mockHttpResponse.Object);
            _contextAccessor.SetupGet(x => x.HttpContext!.Request).Returns(_mockHttpRequest.Object);
            _contextAccessor.SetupGet(x => x.HttpContext!.Request.Cookies).Returns(_mockRequestCookieCollection.Object);
            _contextAccessor.SetupGet(x => x.HttpContext!.Response.Cookies).Returns(_mockResponseCookies.Object);


            _cookieService = new CookieService(_contextAccessor.Object, _logger.Object);

        }


        #region First AddEssentialCookie first method
        [Test]
        public void AddEssentialCookie_IResponseCookieIsNull_ThrowsException()
        {
            //arrange
            //ResponseCookies are set up in the CookieService constructor so i had to mock everything again in order to return an null object.
            _logger = new Mock<ILogger<CookieService>>();
            _contextAccessor = new Mock<IHttpContextAccessor>();
            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpResponse = new Mock<HttpResponse>();
            _mockHttpRequest = new Mock<HttpRequest>();

            var requestCookies = new Mock<IRequestCookieCollection>();

            _contextAccessor.SetupGet(x => x.HttpContext).Returns(_mockHttpContext.Object);
            _contextAccessor.SetupGet(x => x.HttpContext!.Response).Returns(_mockHttpResponse.Object);
            _contextAccessor.SetupGet(x => x.HttpContext!.Request).Returns(_mockHttpRequest.Object);
            _contextAccessor.SetupGet(x => x.HttpContext!.Request.Cookies).Returns(requestCookies.Object);
            _contextAccessor.SetupGet(x => x.HttpContext!.Response.Cookies).Returns(() => default!);

            _cookieService = new CookieService(_contextAccessor.Object, _logger.Object);


            //assert 
            Assert.Throws<NullReferenceException>(() => _cookieService.AddEssentialCookie("Test", "Test"));
        }
        [Test]
        [TestCase("")]
        [TestCase(null)]
        public void AddEssentialCookie_TheKeyIsNullOREmpty_ThrowsException(string key)
        {
            //assert
            Assert.Throws<ArgumentNullException>(() => _cookieService.AddEssentialCookie(key, "Test"));
        }

        [Test]
        [TestCase("")]
        [TestCase(null)]
        public void AddEssentialCookie_TheValueIsNullOREmpty_ThrowsException(string value)
        {
            //assert
            Assert.Throws<ArgumentNullException>(() => _cookieService.AddEssentialCookie("Test", value));
        }
        [Test]
        [TestCase(-20)]
        [TestCase(0)]
        public void AddEssentialCookie_TheCookieAgeIsZeroOrLess_ThrowsException(int age)
        {
            //assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _cookieService.AddEssentialCookie("Test", "Test", age));
        }

        [Test]
        public void AddEssentialCookie_EverythingWorksCookieWasAdded()
        {
            //act
            _cookieService.AddEssentialCookie("TestKey", "TestValue", 30);

            //assert
            _mockResponseCookies.Verify(x => x.Append("TestKey", "TestValue", It.Is<CookieOptions>(opts =>

                opts.IsEssential == true && opts.MaxAge == TimeSpan.FromMinutes(30)
            )), Times.Once());
        }
        #endregion

        #region AddEssentialCookie Second Method With CookieOptions Parameter
        [Test]
        public void AddEssentialCookie_Second_IResponseCookieIsNull_ThrowsException()
        {
            //arrange
            _logger = new Mock<ILogger<CookieService>>();
            _contextAccessor = new Mock<IHttpContextAccessor>();
            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpResponse = new Mock<HttpResponse>();
            _mockHttpRequest = new Mock<HttpRequest>();

            var requestCookies = new Mock<IRequestCookieCollection>();

            _contextAccessor.SetupGet(x => x.HttpContext).Returns(_mockHttpContext.Object);
            _contextAccessor.SetupGet(x => x.HttpContext!.Response).Returns(_mockHttpResponse.Object);
            _contextAccessor.SetupGet(x => x.HttpContext!.Request).Returns(_mockHttpRequest.Object);
            _contextAccessor.SetupGet(x => x.HttpContext!.Request.Cookies).Returns(requestCookies.Object);
            _contextAccessor.SetupGet(x => x.HttpContext!.Response.Cookies).Returns(() => default!);

            _cookieService = new CookieService(_contextAccessor.Object, _logger.Object);


            //assert 
            Assert.Throws<NullReferenceException>(() => _cookieService.AddEssentialCookie("Test", "Test", new CookieOptions()));
        }
        [Test]
        [TestCase("")]
        [TestCase(null)]
        public void AddEssentialCookie_Second_TheKeyIsNullOREmpty_ThrowsException(string key)
        {
            //assert
            Assert.Throws<ArgumentNullException>(() => _cookieService.AddEssentialCookie(key, "Test", new CookieOptions()));
        }

        [Test]
        [TestCase("")]
        [TestCase(null)]
        public void AddEssentialCookie_Second_TheValueIsNullOREmpty_ThrowsException(string value)
        {
            //assert
            Assert.Throws<ArgumentNullException>(() => _cookieService.AddEssentialCookie("Test", value, new CookieOptions()));
        }
        [Test]
        public void AddEssentialCookie_Second_NoCookiesOptionsIsProvided_ADefaultOneWillBeUsed()
        {
            //act
            _cookieService.AddEssentialCookie("TestKey", "TestValue", default(CookieOptions));

            //assert
            _mockResponseCookies.Verify(x => x.Append("TestKey", "TestValue", It.Is<CookieOptions>(opts =>

                opts.IsEssential == true && opts.MaxAge == TimeSpan.FromMinutes(30)
            )), Times.Once());
        }
        [Test]
        public void AddEssentialCookie_Second_CookiesOptionsIsProvided_ButNoMaxAgeIsProvided_DefaultMaxAgeIsUsed()
        {
            //act
            _cookieService.AddEssentialCookie("TestKey", "TestValue", new CookieOptions { IsEssential = true });

            //assert
            _mockResponseCookies.Verify(x => x.Append("TestKey", "TestValue", It.Is<CookieOptions>(opts =>

                opts.IsEssential == true && opts.MaxAge == TimeSpan.FromMinutes(30)
            )), Times.Once());
        }
        [Test]
        public void AddEssentialCookie_Second_CookiesOptionsIsProvided_IsEssentialIsSetToFalse_ButTheMethodSetEssentialBackToTrue()
        {
            //act
            _cookieService.AddEssentialCookie("TestKey", "TestValue", new CookieOptions { IsEssential = false });

            //assert
            _mockResponseCookies.Verify(x => x.Append("TestKey", "TestValue", It.Is<CookieOptions>(opts =>

                opts.IsEssential == true && opts.MaxAge == TimeSpan.FromMinutes(30)
            )), Times.Once());
        }
        [Test]
        public void AddEssentialCookie_Second_CookiesOptionsIsProvided_EverythingWOrks()
        {
            //act
            _cookieService.AddEssentialCookie("TestKey", "TestValue", new CookieOptions { IsEssential = true, MaxAge = TimeSpan.FromMinutes(10) });

            //assert
            _mockResponseCookies.Verify(x => x.Append("TestKey", "TestValue", It.Is<CookieOptions>(opts =>

                opts.IsEssential == true && opts.MaxAge == TimeSpan.FromMinutes(10)
            )), Times.Once());
        }

        #endregion

        #region GetCookieValue
        [Test]
        [TestCase("")]
        [TestCase(null)]
        public void GetCookieValue_KeyIsNullOrEmpty_ThrowsException(string key)
        {
            Assert.Throws<ArgumentNullException>(() => _cookieService.GetCookieValue(key));
        }
        [Test]
        public void GetCookieValue_IRequestCookieCollectionIsNUll_THrowsException()
        {
            //arrange
            _logger = new Mock<ILogger<CookieService>>();
            _contextAccessor = new Mock<IHttpContextAccessor>();
            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpResponse = new Mock<HttpResponse>();
            _mockHttpRequest = new Mock<HttpRequest>();

            var responseCookies = new Mock<IResponseCookies>();

            _contextAccessor.SetupGet(x => x.HttpContext).Returns(_mockHttpContext.Object);
            _contextAccessor.SetupGet(x => x.HttpContext!.Response).Returns(_mockHttpResponse.Object);
            _contextAccessor.SetupGet(x => x.HttpContext!.Request).Returns(_mockHttpRequest.Object);
            _contextAccessor.SetupGet(x => x.HttpContext!.Request.Cookies).Returns(() => default!);
            _contextAccessor.SetupGet(x => x.HttpContext!.Response.Cookies).Returns(responseCookies.Object);

            _cookieService = new CookieService(_contextAccessor.Object, _logger.Object);


            //assert 
            Assert.Throws<NullReferenceException>(() => _cookieService.GetCookieValue("Test"));
        }

        [Test]
        public void GetCookieValue_EverythingWorks_ReturnValue()
        {
            _cookieService.GetCookieValue("TestKey1");
            string? mockValue;
            _mockRequestCookieCollection.Verify(c => c.TryGetValue("TestKey1", out mockValue), Times.Once());
        }

        #endregion
      
        #region CookieExist
        [Test]
        public void CookieExist_RequestCookieCollectionIsNull_ThrowException()
        {
            //arrange
            _logger = new Mock<ILogger<CookieService>>();
            _contextAccessor = new Mock<IHttpContextAccessor>();
            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpResponse = new Mock<HttpResponse>();
            _mockHttpRequest = new Mock<HttpRequest>();

            _contextAccessor.SetupGet(x => x.HttpContext).Returns(_mockHttpContext.Object);
            _contextAccessor.SetupGet(x => x.HttpContext!.Response).Returns(_mockHttpResponse.Object);
            _contextAccessor.SetupGet(x => x.HttpContext!.Request).Returns(_mockHttpRequest.Object);
            _contextAccessor.SetupGet(x => x.HttpContext!.Request.Cookies).Returns(() => default!);
            _contextAccessor.SetupGet(x => x.HttpContext!.Response.Cookies).Returns(() => default!);

            _cookieService = new CookieService(_contextAccessor.Object, _logger.Object);


            //assert 
            Assert.Throws<NullReferenceException>(() => _cookieService.CookieExist("Test1"));
        }
        [Test]
        [TestCase("")]
        [TestCase(null)]
        public void CookieExist_KeyIsNullOrEmpty_ThrowException(string key)
        {
            Assert.Throws<ArgumentNullException>(() => _cookieService.CookieExist(key));
        }
        [Test]
        public void CookieExist_Works()
        {
            //act
            _cookieService.CookieExist("Test");
            _mockRequestCookieCollection.Verify(x => x.ContainsKey("Test"), Times.Once());
        }
        #endregion

        #region DeleteCookie
        [Test]
        public void DeleteCookie_RequestCookieCollectionIsNull_ThrowException()
        {
            //arrange
            _logger = new Mock<ILogger<CookieService>>();
            _contextAccessor = new Mock<IHttpContextAccessor>();
            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpResponse = new Mock<HttpResponse>();
            _mockHttpRequest = new Mock<HttpRequest>();

            _contextAccessor.SetupGet(x => x.HttpContext).Returns(_mockHttpContext.Object);
            _contextAccessor.SetupGet(x => x.HttpContext!.Response).Returns(_mockHttpResponse.Object);
            _contextAccessor.SetupGet(x => x.HttpContext!.Request).Returns(_mockHttpRequest.Object);
            _contextAccessor.SetupGet(x => x.HttpContext!.Request.Cookies).Returns(() => default!);
            _contextAccessor.SetupGet(x => x.HttpContext!.Response.Cookies).Returns(() => default!);

            _cookieService = new CookieService(_contextAccessor.Object, _logger.Object);


            //assert 
            Assert.Throws<NullReferenceException>(() => _cookieService.DeleteCookie("Test1"));
        }
        [Test]
        [TestCase("")]
        [TestCase(null)]
        public void DeleteCookie_KeyIsNullOrEmpty_ThrowException(string key)
        {
            Assert.Throws<ArgumentNullException>(() => _cookieService.DeleteCookie(key));
        }
        [Test]
        public void DeleteCookie_Works()
        {
            //act
            _cookieService.DeleteCookie("Test");
            _mockResponseCookies.Verify(x => x.Delete("Test"), Times.Once());
        }
        #endregion

        #region GetAllCurentCookies
        [Test]
        public void GetAllCurrentRequestCookies_RequestIsNull_ThrowException()
        {
            //arrange
            _logger = new Mock<ILogger<CookieService>>();
            _contextAccessor = new Mock<IHttpContextAccessor>();
            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpResponse = new Mock<HttpResponse>();
            _mockHttpRequest = new Mock<HttpRequest>();

            _contextAccessor.SetupGet(x => x.HttpContext).Returns(_mockHttpContext.Object);
            _contextAccessor.SetupGet(x => x.HttpContext!.Response).Returns(_mockHttpResponse.Object);
            _contextAccessor.SetupGet(x => x.HttpContext!.Request).Returns(_mockHttpRequest.Object);
            _contextAccessor.SetupGet(x => x.HttpContext!.Request.Cookies).Returns(() => default!);
            _contextAccessor.SetupGet(x => x.HttpContext!.Response.Cookies).Returns(() => default!);

            _cookieService = new CookieService(_contextAccessor.Object, _logger.Object);


            //assert 
            Assert.Throws<NullReferenceException>(() => _cookieService.GetAllCurrentRequestCookies());
        }
        [Test]
        public void GetAllCurrentRequestCookies_ItWorks()
        {
            //act
            var result =_cookieService.GetAllCurrentRequestCookies();
            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }
        #endregion
    }
}
