using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using scriptbuster.dev.Controllers.VerifyEmailAPI;
using scriptbuster.dev.Infrastructure.ApiModels;
using scriptbuster.dev.Services.CodeGeneratorService;
using scriptbuster.dev.Services.CookieService;
using scriptbuster.dev.Services.SessionService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scriptbuster.dev_UnitTests.Controllers
{
    [TestFixture]
    internal class VerifyEmailControllerTests
    {
        private Mock<ILogger<VerifyEmailController>> _logger;
        private Mock<ISessionService> _session;
        private Mock<ICookieService> _cookieService;
        private Mock<ICodeGenerator> _codeGenerator;
        private Mock<IEmailSender> _emailSender;
        private VerifyEmailController _controller;
        [SetUp]
        public void SetUp()
        {
            _logger = new Mock<ILogger<VerifyEmailController>>();
            _session = new Mock<ISessionService>();
            _cookieService = new Mock<ICookieService>();
            _codeGenerator = new Mock<ICodeGenerator>();
            _emailSender = new Mock<IEmailSender>();

            _controller = new VerifyEmailController(_logger.Object, _session.Object, _codeGenerator.Object, _cookieService.Object, _emailSender.Object);
        }

        #region Get Acces Key
        [Test]
        public async Task GetAccesKey_AccesKeyIsInTheSession_ReturnKeyFromTHeSession()
        {
            //arrange
            _session.Setup(x => x.GetString(It.IsAny<string>())).ReturnsAsync("TestApiKeyValue");

            //act
            var result = await _controller.GetAccesKey() as OkObjectResult;

            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.StatusCode, Is.EqualTo(200));
            //using reflection to get an anonymous object Property
            var value = result.Value?.GetType().GetProperty("AccesKey")?.GetValue(result.Value);
            Assert.That(value, Is.EqualTo("TestApiKeyValue"));
        }
        [Test]
        public async Task GetAccesKey_KeyIsNotInSession_GenerateAndReturnNewKey()
        {
            //arrange
            _codeGenerator.Setup(x => x.GenerateApiKey(It.IsAny<int>())).Returns("TestApiKeyValueNew");

            //act
            var result = await _controller.GetAccesKey() as OkObjectResult;

            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.StatusCode, Is.EqualTo(200));
            //using reflection to get an anonymous object Property
            var value = result.Value?.GetType().GetProperty("AccesKey")?.GetValue(result.Value);
            Assert.That(value, Is.EqualTo("TestApiKeyValueNew"));
        }
        #endregion

        #region  SendMailCode
        [Test]
        [TestCase(null)]
        [TestCase("")]
        public async Task SendEmailCode_AccesKeyIsNullOrEmpty_ReturnUnauthorizedRequest(string accessKey)
        {
            //act
            var result = await _controller.SendEmailCode("Test@test.com", accessKey) as UnauthorizedObjectResult;
            var value = result?.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.StatusCode, Is.EqualTo(401));
            Assert.That(value.Error, Is.EqualTo("You are not authorized"));
            Assert.That(value.Message, Is.EqualTo("Missing accessKey"));

        }
        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("notvalidyahoo.com")]
        public async Task SendEmailCode_EmailIsNullOrEmtpyOrIsNotAValidEmail_BadRequest(string email)
        {
            //act
            var result = await _controller.SendEmailCode(email, "TestAccesKey") as BadRequestObjectResult;
            var value = result?.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(value.Error, Is.EqualTo("Missing Email"));
            Assert.That(value.Message, Is.EqualTo("Please provide an Email!"));
        }

        [Test]
        public async Task SendEmailCode_AbuseCookieExist_ReturnBadRequest()
        {
            //arraange
            _cookieService.Setup(x => x.CookieExist(It.IsAny<string>())).Returns(true);

            //act
            var result = await _controller.SendEmailCode("test@test.com", "TestAccesKey") as BadRequestObjectResult;
            var value = result?.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(value.Error, Is.EqualTo("Code sending abuse"));
            Assert.That(value.Message, Is.EqualTo("You can send again after 2 min"));
        }
        [Test]
        public async Task SendEmailCode_ApiIsAbused_ReturnBadRequest()
        {
            //arrage
            _session.Setup(x => x.GetInt32(It.IsAny<string>())).ReturnsAsync(100);

            //act
            var result = await _controller.SendEmailCode("test@test.com", "TestAccesKey") as BadRequestObjectResult;
            var value = result?.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(value.Error, Is.EqualTo("Api Abuse Detected"));
            Assert.That(value.Message!.Contains("Abuse. Maximum"), Is.EqualTo(true));

        }
        [Test]
        public async Task SendEmailCode_SessionKeyIsNotMatching_ReturnUnauthorized()
        {
            //arrange
            _session.Setup(x => x.GetString(It.IsAny<string>())).ReturnsAsync("OriginalAccesKey");

            //act
            var result = await _controller.SendEmailCode("test@test.com", "NotMatchingAccesKey") as UnauthorizedObjectResult;
            var value = result?.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.StatusCode, Is.EqualTo(401));
            Assert.That(value.Error, Is.EqualTo("You are not authorized"));
            Assert.That(value.Message, Is.EqualTo("Acces key expired or not matching"));
        }
        [Test]
        public async Task SendEmailCode_SendEmailThrowsException_ButResponseIsNotBreaked()
        {
            //arrange
            _emailSender.Setup(x => x.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new Exception("Something went wrong"));
            _session.Setup(x => x.GetString(It.IsAny<string>())).ReturnsAsync("AccesKey");

            //act
            var result = await _controller.SendEmailCode("test@test.com", "AccesKey") as OkObjectResult;
            var value = result?.Value as SuccesResponse ?? new();

            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(value.Message, Is.EqualTo("A verification code has been sent to your email. Please check your inbox."));
            _emailSender.Verify(x => x.SendEmail("test@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }
        [Test]
        public async Task SendEmailCode_WorksSmoth_ReturnOk()
        {
            //arrange
            _session.Setup(x => x.GetString(It.IsAny<string>())).ReturnsAsync("AccesKey");

            //act
            var result = await _controller.SendEmailCode("test@test.com", "AccesKey") as OkObjectResult;
            var value = result?.Value as SuccesResponse ?? new();

            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(value.Message, Is.EqualTo("A verification code has been sent to your email. Please check your inbox."));
            _emailSender.Verify(x => x.SendEmail("test@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }
        #endregion
        #region VerifyEmail
        [Test]
        [TestCase(null)]
        [TestCase("")]
        public async Task VerifyEmail_AccesKEyIsNulLOrEmpty_ReturnUnauthorized(string accesKey)
        {
            //act
            var result = await _controller.VerifyEmail("123456", accesKey) as UnauthorizedObjectResult;
            var value = result?.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.StatusCode, Is.EqualTo(401));
            Assert.That(value.Error, Is.EqualTo("You are not authorized"));
            Assert.That(value.Message, Is.EqualTo("Missing accessKey"));
        }
        [Test]
        [TestCase(null)]
        [TestCase("")]
        public async Task VerifyEmail_VerificationCodeIsNullOrEmptyOr_ReturnBadRequest(string code)
        {
            //act
            var result = await _controller.VerifyEmail(code, "AccesKey") as BadRequestObjectResult;
            var value = result?.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(value.Error, Is.EqualTo("Missing Verification Code"));
            Assert.That(value.Message, Is.EqualTo("Please provide a verification code!"));
        }
        [Test]
        public async Task VerifyEmail_AccesKeyIsNoTMatching_ReturnUnauthorized()
        {
            //arrange
            _session.Setup(x => x.GetString(It.IsAny<string>())).ReturnsAsync("AccesKey");
            //act
            var result = await _controller.VerifyEmail("123456", "NotMatchingAccesKey") as UnauthorizedObjectResult;
            var value = result?.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.StatusCode, Is.EqualTo(401));
            Assert.That(value.Error, Is.EqualTo("You are not authorized"));
            Assert.That(value.Message, Is.EqualTo("Acces key expired or not matching"));
        }

        [Test]
        public async Task VerifyEmail_CookieExpired_ReturnBadRequest()
        {
            //arrange
            _session.Setup(x => x.GetString(It.IsAny<string>())).ReturnsAsync("AccessKey");
            _cookieService.Setup(x => x.CookieExist(It.IsAny<string>())).Returns(false);
            //act
            var result = await _controller.VerifyEmail("123456", "AccessKey") as BadRequestObjectResult;
            var value = result?.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(value.Error, Is.EqualTo("Code is not available"));
            Assert.That(value.Message, Is.EqualTo("Verification Code expired"));
        }

        [Test]
        public async Task VerifyEmail_CodeIsNotMatching_ReturnBadRequest()
        {
            //arrange
            _session.Setup(x => x.GetString(It.IsAny<string>())).ReturnsAsync("AccessKey");
            _cookieService.Setup(x => x.CookieExist(It.IsAny<string>())).Returns(true);
            //act
            var result = await _controller.VerifyEmail("123456", "AccessKey") as BadRequestObjectResult;
            var value = result?.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(value.Error, Is.EqualTo("Wrong Verification Code"));
            Assert.That(value.Message, Is.EqualTo("Verification Code does not match!"));
        }
        [Test]
        public async Task VerifyEmail_WorksCodeIsVerified_ReturnOk()
        {
            //arrange
            _session.SetupSequence(x => x.GetString(It.IsAny<string>())).ReturnsAsync("AccessKey").ReturnsAsync("123456");
            _cookieService.Setup(x => x.CookieExist(It.IsAny<string>())).Returns(true);
            //act
            var result = await _controller.VerifyEmail("123456", "AccessKey") as OkObjectResult;
            var value = result?.Value as SuccesResponse ?? new();

            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(value.Message, Is.EqualTo("Code has been verified with Succes!"));
        }
        #endregion
    }
}
