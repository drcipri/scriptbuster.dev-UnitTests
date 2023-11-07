using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NuGet.Frameworks;
using scriptbuster.dev.Controllers;
using scriptbuster.dev.IdentityModels.Tables;
using scriptbuster.dev.Infrastructure.ApiModels;
using scriptbuster.dev.Infrastructure.ViewModels.AccountController;
using scriptbuster.dev.Services.CookieService;
using scriptbuster.dev.Services.SessionService;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace scriptbuster.dev_UnitTests.Controllers
{
    [TestFixture]
    internal class AccountControlllerTests
    {
        private Mock<UserManager<AplicationUser>> _userManager;
        private Mock<SignInManager<AplicationUser>> _signInManager;
        private Mock<ILogger<AccountController>> _logger;
        private Mock<IEmailSender> _emailSender;
        private Mock<IHttpContextAccessor> _accesor;
        private Mock<IDistributedCache> _cache;
        private Mock<ISessionService> _session;
        private Mock<ICookieService> _cookieService;
        private AccountController _accountController;
        private Mock<HttpContext> _context;

        [SetUp]
        public void SetUp()
        {
            _context = new Mock<HttpContext>();
            _logger = new Mock<ILogger<AccountController>>();
            _emailSender = new Mock<IEmailSender>();
            _session = new Mock<ISessionService>();
            _accesor = new Mock<IHttpContextAccessor>();
            _cache = new Mock<IDistributedCache>();
            _cookieService = new Mock<ICookieService>();
            _userManager = new Mock<UserManager<AplicationUser>>(new Mock<IUserStore<AplicationUser>>().Object, null, null, null, null, null, null, null, null);
            _signInManager = new Mock<SignInManager<AplicationUser>>
                (_userManager.Object, _accesor.Object, new Mock<IUserClaimsPrincipalFactory<AplicationUser>>().Object, null, null, null, null);
            _accountController = new AccountController(_userManager.Object, _signInManager.Object, _logger.Object,
                                                       _emailSender.Object, _accesor.Object, _cache.Object, _session.Object,
                                                       _cookieService.Object);
        }
        #region Login Logout
        [Test]
        public void Login_UserIsAuthethicated_RedirectToAdmin()
        {
            //arrange 
            var mockIdentity = new Mock<IIdentity>();
            mockIdentity.SetupGet(x => x.IsAuthenticated).Returns(true);
            var mockPrincipal = new Mock<ClaimsPrincipal>();
            mockPrincipal.SetupGet(x => x.Identity).Returns(mockIdentity.Object);
            _context.SetupGet(x => x.User).Returns(mockPrincipal.Object);
            _accesor.Setup(x => x.HttpContext).Returns(_context.Object);

            //act
            var result = _accountController.Login(default) as RedirectToPageResult;

            //arrange
            Assert.That(result?.PageName, Is.EqualTo("/Admin"));
        }
        [Test]
        public void Login_ReturnUrlIsNull_ReturnView()
        {
            //act
            var result = (_accountController.Login(default) as ViewResult)?.ViewData.Model as LoginViewModel ?? new();

            Assert.IsNull(result?.ReturnUrl);
        }
        [Test]
        public void Login_ReturnIsNotNull_ReturnView()
        {
            //act
            var result = (_accountController.Login("/test") as ViewResult)?.ViewData.Model as LoginViewModel ?? new();

            Assert.That(result?.ReturnUrl, Is.EqualTo("/test"));
        }
        [Test]
        public async Task LoginUser_ModelIsNotValid_ReturnLoginView()
        {
            //arrange
            _accountController.ModelState.AddModelError("error", "custom error");
            var model = new LoginViewModel();

            //act
            var result = await _accountController.LoginUser(model) as ViewResult;
            var viewModel = result?.ViewData.Model as LoginViewModel ?? new();


            //assert
            Assert.That(viewModel, Is.SameAs(model));
            Assert.That(result?.ViewData.ModelState.ContainsKey("error"), Is.True);
            Assert.That(result.ViewName, Is.EqualTo("Login"));
            _userManager.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Never);
        }
        [Test]
        public async Task LoginUser_LoginAttemptsExceededMaxAttemptsAddNewModelError_ReturnLoginView()
        {
            //arrange
            var mockConnection = new Mock<ConnectionInfo>();
            mockConnection.Setup(x => x.RemoteIpAddress).Returns(IPAddress.Parse("192.168.1.1"));
            _accesor.Setup(x => x.HttpContext).Returns(_context.Object);
            _context.Setup(x => x.Connection).Returns(mockConnection.Object);

            byte[] logginAttempts = Encoding.ASCII.GetBytes("7");
            _cache.Setup(x => x.GetAsync(It.IsAny<string>(), default)).ReturnsAsync(logginAttempts);

            var model = new LoginViewModel();
            //act
            var result = await _accountController.LoginUser(model) as ViewResult;
            var viewModel = result?.ViewData.Model as LoginViewModel ?? new();


            //assert
            Assert.That(viewModel, Is.SameAs(model));
            Assert.That(result?.ViewData.ModelState.ContainsKey("AbusiveLogin"), Is.True);
            Assert.That(result.ViewName, Is.EqualTo("Login"));
            _userManager.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Never);
        }
        [Test]
        public async Task LoginUser_UserIsCannotBeFoundInvalidCredentials_ReturnLoginView()
        {
            //arrange
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))!.ReturnsAsync(default(AplicationUser));
            _userManager.Setup(x => x.FindByNameAsync(It.IsAny<string>()))!.ReturnsAsync(default(AplicationUser));

            var mockConnection = new Mock<ConnectionInfo>();
            mockConnection.Setup(x => x.RemoteIpAddress).Returns(IPAddress.Parse("192.168.1.1"));
            _accesor.Setup(x => x.HttpContext).Returns(_context.Object);
            _context.Setup(x => x.Connection).Returns(mockConnection.Object);

            byte[] logginAttempts = Encoding.ASCII.GetBytes("1");
            _cache.Setup(x => x.GetAsync(It.IsAny<string>(), default)).ReturnsAsync(logginAttempts);

            var model = new LoginViewModel
            {
                UserName = "Test",
                Password = "PasswordTest"
            };
            //act
            var result = await _accountController.LoginUser(model) as ViewResult;
            var viewModel = result?.ViewData.Model as LoginViewModel ?? new();


            //assert
            Assert.That(viewModel, Is.SameAs(model));
            Assert.That(result?.ViewData.ModelState.ContainsKey("WrongCredentials"), Is.True);
            Assert.That(result.ViewName, Is.EqualTo("Login"));
            _signInManager.Verify(x => x.SignOutAsync(), Times.Never());
            _userManager.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Once());
        }
        [Test]
        public async Task LoginUser_UserIsFoundButWrongPassword_ReturnLoginView()
        {
            //arrange
            var user = new AplicationUser { UserName = "Test" };
            _userManager.Setup(x => x.FindByNameAsync(It.IsAny<string>()))!.ReturnsAsync(user);

            _signInManager.Setup(x => x.PasswordSignInAsync(It.IsAny<AplicationUser>(), It.IsAny<string>(), false, false))
                          .ReturnsAsync(new SignInResultMock(false));

            var mockConnection = new Mock<ConnectionInfo>();
            mockConnection.Setup(x => x.RemoteIpAddress).Returns(IPAddress.Parse("192.168.1.1"));
            _accesor.Setup(x => x.HttpContext).Returns(_context.Object);
            _context.Setup(x => x.Connection).Returns(mockConnection.Object);

            byte[] logginAttempts = Encoding.ASCII.GetBytes("1");
            _cache.Setup(x => x.GetAsync(It.IsAny<string>(), default)).ReturnsAsync(logginAttempts);

            var model = new LoginViewModel
            {
                UserName = "Test",
                Password = "PasswordTest"
            };
            //act
            var result = await _accountController.LoginUser(model) as ViewResult;
            var viewModel = result?.ViewData.Model as LoginViewModel ?? new();


            //assert
            Assert.That(viewModel, Is.SameAs(model));
            Assert.That(result?.ViewData.ModelState.ContainsKey("InvalidPassword"), Is.True);
            Assert.That(result.ViewName, Is.EqualTo("Login"));
            _signInManager.Verify(x => x.SignOutAsync(), Times.Once());
            _userManager.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Once());
            _signInManager.Verify(x => x.PasswordSignInAsync(It.Is<AplicationUser>(x => x == user), It.Is<string>(x => x == model.Password), false, false), Times.Once());
        }
        [Test]
        public async Task LoginUser_Succeded_RedirectToAdminPageReturnUrlIsNull()
        {
            //arrange
            var user = new AplicationUser { UserName = "Test" };
            _userManager.Setup(x => x.FindByNameAsync(It.IsAny<string>()))!.ReturnsAsync(user);

            _signInManager.Setup(x => x.PasswordSignInAsync(It.IsAny<AplicationUser>(), It.IsAny<string>(), false, false))
                          .ReturnsAsync(new SignInResultMock(true));

            var mockConnection = new Mock<ConnectionInfo>();
            mockConnection.Setup(x => x.RemoteIpAddress).Returns(IPAddress.Parse("192.168.1.1"));
            _accesor.Setup(x => x.HttpContext).Returns(_context.Object);
            _context.Setup(x => x.Connection).Returns(mockConnection.Object);

            byte[] logginAttempts = Encoding.ASCII.GetBytes("1");
            _cache.Setup(x => x.GetAsync(It.IsAny<string>(), default)).ReturnsAsync(logginAttempts);

            var model = new LoginViewModel
            {
                UserName = "Test",
                Password = "PasswordTest",
                ReturnUrl = default
            };
            //act
            var result = await _accountController.LoginUser(model) as RedirectToPageResult;

            //assert
            Assert.That(result!.PageName, Is.EqualTo("/Admin"));
            _signInManager.Verify(x => x.SignOutAsync(), Times.Once());
            _userManager.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Once());
            _signInManager.Verify(x => x.PasswordSignInAsync(It.Is<AplicationUser>(x => x == user), It.Is<string>(x => x == model.Password), false, false), Times.Once());
        }
        [Test]
        public async Task LoginUser_Succeded_RedirectToReturnUrlBecauseItsNotNull()
        {
            //arrange
            var user = new AplicationUser { UserName = "Test" };
            _userManager.Setup(x => x.FindByNameAsync(It.IsAny<string>()))!.ReturnsAsync(user);

            _signInManager.Setup(x => x.PasswordSignInAsync(It.IsAny<AplicationUser>(), It.IsAny<string>(), false, false))
                          .ReturnsAsync(new SignInResultMock(true));

            var mockConnection = new Mock<ConnectionInfo>();
            mockConnection.Setup(x => x.RemoteIpAddress).Returns(IPAddress.Parse("192.168.1.1"));
            _accesor.Setup(x => x.HttpContext).Returns(_context.Object);
            _context.Setup(x => x.Connection).Returns(mockConnection.Object);

            byte[] logginAttempts = Encoding.ASCII.GetBytes("1");
            _cache.Setup(x => x.GetAsync(It.IsAny<string>(), default)).ReturnsAsync(logginAttempts);

            var model = new LoginViewModel
            {
                UserName = "Test",
                Password = "PasswordTest",
                ReturnUrl = "/test/url"
            };
            //act
            var result = await _accountController.LoginUser(model) as RedirectResult;

            //assert
            Assert.That(result!.Url, Is.EqualTo("/test/url"));
            _signInManager.Verify(x => x.SignOutAsync(), Times.Once());
            _userManager.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Once());
            _signInManager.Verify(x => x.PasswordSignInAsync(It.Is<AplicationUser>(x => x == user), It.Is<string>(x => x == model.Password), false, false), Times.Once());
        }
        [Test]
        public async Task Logout_ReturnLoginPage()
        {
            //act
            var result = await _accountController.Logout() as RedirectToActionResult;

            //assert
            Assert.That(result!.ActionName, Is.EqualTo("Login"));
            _signInManager.Verify(x => x.SignOutAsync(), Times.Once());
        }
        #endregion
        #region Reset Password Endpoints
        [Test]
        public void ForgotPassword_CanCreateTheSendResetPasswordLinkForAjaxRequestForTHeFrontEnd_ReturnItsView()
        {
            //arrange
            var mockIUrlHelper = new Mock<IUrlHelper>();
            _accountController.Url = mockIUrlHelper.Object;
            mockIUrlHelper.Setup(x => x.Action(It.IsAny<UrlActionContext>())).Returns("testHost/test-controller/test-action");
            //act
            _accountController.ForgotPassword();
            string ajaxLink = _accountController.ViewBag.AjaxLink;
            //assert 
            //check if contains the route of the target endpoint
            StringAssert.Contains("test-controller", ajaxLink);
            StringAssert.Contains("test-action", ajaxLink);
        }
        [Test]
        public async Task SendResetPasswordLink_ModelStateIsNotValid_ReturnBadRequest()
        {
            //arrange
            _accountController.ModelState.AddModelError("Error", "Test error");

            //act
            var result = await _accountController.SendResetPasswordLink(default!) as BadRequestObjectResult;
            var model = result?.Value as ModelStateDictionary ?? new();

            //assert
            Assert.That(result?.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            _session.Verify(x => x.GetInt32(It.IsAny<string>()), Times.Never());
            _emailSender.Verify(x => x.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }
        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("emailtest.com")]//without @
        public async Task SendResetPasswordLink_StringIsNullOREmptyOrIsNotValid_ReturnBadRequest(string email)
        {
            ///act
            var result = await _accountController.SendResetPasswordLink(email) as BadRequestObjectResult;
            var model = result?.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result?.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(model.Error, Is.EqualTo("Empty email"));
            Assert.That(model.Message, Is.EqualTo("A valid email is required!(@)"));
            _emailSender.Verify(x => x.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            _session.Verify(x => x.GetInt32(It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task SendResetPasswordLink_ResetPassswordAttemptsAreBiggerThanMaxAttempts_ReturnBadRequest()
        {
            //arrange
            int maxAttempts = 7;
            _session.Setup(x => x.GetInt32(It.IsAny<string>())).ReturnsAsync(maxAttempts);

            //act
            var result = await _accountController.SendResetPasswordLink("test@email.com") as BadRequestObjectResult;
            var model = result?.Value as ErrorResponse ?? new();


            //assert
            Assert.That(result?.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(model.Error, Is.EqualTo("ResetPasswordAbuse"));
            Assert.That(model.Message, Is.EqualTo("Reset Password Abuse, try again later"));
            _session.Verify(x => x.GetInt32(It.IsAny<string>()), Times.Once());

            _emailSender.Verify(x => x.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task SendResetPaswordLink_UserIsNotFound_ReturnOkResult()
        {
            //arrange not needed the User going to be null by default because of moq

            //act
            var result = await _accountController.SendResetPasswordLink("test@email.com") as OkObjectResult;
            var model = result?.Value as SuccesResponse ?? new();

            //arrange
            Assert.That(result?.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
            StringAssert.Contains("a message was sent to this email address", model.Message);
            _session.Verify(x => x.GetInt32(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.FindByEmailAsync("test@email.com"), Times.Once());

            _userManager.Verify(x => x.GeneratePasswordResetTokenAsync(It.IsAny<AplicationUser>()), Times.Never());
            _emailSender.Verify(x => x.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task SendeResetPasswordLink_SendEmailThrowsExceptionButWontBreakTheResponse_ReturnOkResult()
        {
            //arrange
            // 1.return a user
            var user = new AplicationUser();
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            //2.Moq IUrlHelper
            var mockIUrlHelepr = new Mock<IUrlHelper>();
            _accountController.Url = mockIUrlHelepr.Object;
            //mock to get the Scheme
            var mockRequest = new Mock<HttpRequest>();
            mockRequest.SetupGet(x => x.Scheme).Returns("http");
            _accesor.Setup(x => x.HttpContext).Returns(_context.Object);
            _context.Setup(x => x.Request).Returns(mockRequest.Object);

            //3/.Make SendEmail throw an exception
            _emailSender.Setup(x => x.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                        .ThrowsAsync(new Exception("test exception"));

            //act
            var result = await _accountController.SendResetPasswordLink("test@email.com") as OkObjectResult;
            var model = result?.Value as SuccesResponse ?? new();

            //assert
            Assert.That(result?.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
            StringAssert.Contains("a message was sent to this email address", model.Message);

            _userManager.Verify(x => x.GeneratePasswordResetTokenAsync(It.Is<AplicationUser>(x => x == user)), Times.Once());
            Assert.ThrowsAsync<Exception>(() => _emailSender.Object.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), "test exception");
        }
        [Test]
        public async Task SendeResetPasswordLink_EverythingWorks_ReturnOkResult()
        {
            //arrange
            // 1.return a user
            var user = new AplicationUser();
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            //2.Moq IUrlHelper
            _accountController.Url = new ManualMockUrlHelper();
            //mock scheme
            var mockRequest = new Mock<HttpRequest>();
            mockRequest.SetupGet(x => x.Scheme).Returns("http");
            _accesor.Setup(x => x.HttpContext).Returns(_context.Object);
            _context.Setup(x => x.Request).Returns(mockRequest.Object);


            //act
            var result = await _accountController.SendResetPasswordLink("test@email.com") as OkObjectResult;
            var model = result?.Value as SuccesResponse ?? new();

            //assert
            Assert.That(result?.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
            StringAssert.Contains("a message was sent to this email address", model.Message);

            _userManager.Verify(x => x.GeneratePasswordResetTokenAsync(It.Is<AplicationUser>(x => x == user)), Times.Once());
            _emailSender.Verify(x => x.SendEmail("test@email.com", "Reset Password", It.Is<string>(x => x.Contains("localTest/test/link"))));
        }

        //ResetPassword
        [Test]
        public async Task ResetPassword_ModelStateIsNotvalid_ReturRedirectToPage()
        {
            //arramge
            _accountController.ModelState.AddModelError("error", "Test Message");

            //act
            var result = await _accountController.ResetPassword("", "") as RedirectToPageResult;

            //assert
            Assert.That(result!.PageName, Is.EqualTo("/ClientInfo"));
            _userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never());
        }
        [Test]
        [TestCase("", "test@email.com")]
        [TestCase(null, "test@email.com")]
        [TestCase("test", "")]
        [TestCase("test", null)]
        [TestCase("test", "testemail.com")]
        public async Task ResetPassword_TokenOrEmailIsNullOrEmpty_ReturnRedirectToPage(string token, string email)
        {
            //act
            var result = await _accountController.ResetPassword(token, email) as RedirectToPageResult;

            //assert
            Assert.That(result!.PageName, Is.EqualTo("/ClientInfo"));
            _userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task ResetPassword_UserIsNotFound_ReturnClientInfo()
        {
            //act
            var result = await _accountController.ResetPassword("token", "test@email.com") as RedirectToPageResult;

            //assert
            Assert.That(result!.PageName, Is.EqualTo("/ClientInfo"));
            _userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.VerifyUserTokenAsync(It.IsAny<AplicationUser>(),
                                                            It.IsAny<string>(), It.IsAny<string>(),
                                                            It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task ResetPassword_TokenIsExpired_ReturnClientInfoPage()
        {
            //arrange
            _userManager.Setup(x => x.VerifyUserTokenAsync(It.IsAny<AplicationUser>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                        .ReturnsAsync(false);
            AplicationUser user = new();
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            //act
            var result = await _accountController.ResetPassword("token", "test@email.com") as RedirectToPageResult;

            //assert
            Assert.That(result!.PageName, Is.EqualTo("/ClientInfo"));
            _userManager.Verify(x => x.FindByEmailAsync("test@email.com"), Times.Once());
            _userManager.Verify(x => x.VerifyUserTokenAsync(It.IsAny<AplicationUser>(),
                                                            It.IsAny<string>(), It.IsAny<string>(),
                                                            It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.GeneratePasswordResetTokenAsync(It.IsAny<AplicationUser>()), Times.Never());
        }
        [Test]
        public async Task ResetPassword_EverythingWorksNewPasswordResetTokeHasBeenGenerated_ReturnItsview()
        {
            //arrange
            _userManager.Setup(x => x.VerifyUserTokenAsync(It.IsAny<AplicationUser>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                        .ReturnsAsync(true);//is valid
            AplicationUser user = new();
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.GeneratePasswordResetTokenAsync(It.IsAny<AplicationUser>())).ReturnsAsync("testToken");
            //act
            var model = (await _accountController.ResetPassword("token", "test@email.com") as ViewResult)?.ViewData.Model as ChangePasswordViewModel ?? new();

            //assert
            Assert.That(model.Email, Is.EqualTo("test@email.com"));
            Assert.That(model.Token, Is.EqualTo("testToken"));
            _userManager.Verify(x => x.FindByEmailAsync("test@email.com"), Times.Once());
            _userManager.Verify(x => x.VerifyUserTokenAsync(It.IsAny<AplicationUser>(),
                                                            It.IsAny<string>(), It.IsAny<string>(),
                                                            It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.GeneratePasswordResetTokenAsync(user), Times.Once());
        }

        //ChangePassword
        [Test]
        [TestCase("", "test@email.com")]
        [TestCase(null, "test@email.com")]
        [TestCase("test", "")]
        [TestCase("test", null)]
        [TestCase("test", "testemail.com")]
        public async Task ChangePassword_EmailOrTokenAreNullOREmpty_RedirectToPageClientInfo(string token, string email)
        {
            //arrange
            var viewModel = new ChangePasswordViewModel
            {
                Email = email,
                Token = token
            };
            //add model state error to make sure ModelState is not called first
            _accountController.ModelState.AddModelError("error", "ErrorTest");

            //act
            var result = await _accountController.ChangePassword(viewModel) as RedirectToPageResult;

            //assert
            Assert.That(result!.PageName, Is.EqualTo("/ClientInfo"));
            _userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task ChangePassword_ModelStateIsNotValid_ReturnResetPasswordView()
        {
            //arrange
            var viewModel = new ChangePasswordViewModel
            {
                Email = "test@email",
                Token = "testToken"
            };
            _accountController.ModelState.AddModelError("error", "ErrorTest");

            //act
            var result = await _accountController.ChangePassword(viewModel) as ViewResult;
            var model = result?.ViewData.Model as ChangePasswordViewModel ?? new();

            //assert
            Assert.That(result!.ViewName, Is.EqualTo("ResetPassword"));
            Assert.That(model, Is.SameAs(viewModel));
            _userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task ChangePassword_UserIsNull_RedirectToClientInfo()
        {

            //arrange
            var viewModel = new ChangePasswordViewModel
            {
                Email = "test@email",
                Token = "testToken",
                Password = "password",
                RepeatPassword = "password"
            };

            //act
            var result = await _accountController.ChangePassword(viewModel) as RedirectToPageResult;

            //assert
            Assert.That(result!.PageName, Is.EqualTo("/ClientInfo"));
            _userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.VerifyUserTokenAsync(It.IsAny<AplicationUser>(), It.IsAny<string>(),
                                                            It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task ChangePassword_TokenIsExpired_RedirectToClientInfo()
        {
            //arrange
            var viewModel = new ChangePasswordViewModel
            {
                Email = "test@email",
                Token = "testToken",
                Password = "password",
                RepeatPassword = "password"
            };
            var user = new AplicationUser();
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.VerifyUserTokenAsync(It.IsAny<AplicationUser>(),
                         It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                        .ReturnsAsync(false);
            //act
            var result = await _accountController.ChangePassword(viewModel) as RedirectToPageResult;

            //assert
            Assert.That(result!.PageName, Is.EqualTo("/ClientInfo"));
            _userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.VerifyUserTokenAsync(user,
                                                            It.IsAny<string>(), It.IsAny<string>(),
                                                            It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.ResetPasswordAsync(It.IsAny<AplicationUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task ChangePassword_PasswordIsNotValid_ReturnViewResult()
        {
            //arrange
            var viewModel = new ChangePasswordViewModel
            {
                Email = "test@email",
                Token = "testToken",
                Password = "password",
                RepeatPassword = "password"
            };
            var user = new AplicationUser();
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.VerifyUserTokenAsync(It.IsAny<AplicationUser>(),
                         It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                        .ReturnsAsync(true);
            //act
            var result = await _accountController.ChangePassword(viewModel) as ViewResult;
            var model = result?.ViewData.Model as ChangePasswordViewModel ?? new();

            //assert
            Assert.That(result!.ViewName, Is.EqualTo("ResetPassword"));
            Assert.That(model.Email, Is.EqualTo("test@email"));
            Assert.That(model.Token, Is.EqualTo("testToken"));
            Assert.That(result.ViewData.ModelState.ContainsKey("SoftPassword"));

            _userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.VerifyUserTokenAsync(user,
                                                            It.IsAny<string>(), It.IsAny<string>(),
                                                            It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.ResetPasswordAsync(It.IsAny<AplicationUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task ChangePassword_PasswordsDontMatch_ReturnViewResult()
        {
            //arrange
            var viewModel = new ChangePasswordViewModel
            {
                Email = "test@email",
                Token = "testToken",
                Password = "passworD1@",
                RepeatPassword = "passworD1a"
            };
            var user = new AplicationUser();
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.VerifyUserTokenAsync(It.IsAny<AplicationUser>(),
                         It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                        .ReturnsAsync(true);
            //act
            var result = await _accountController.ChangePassword(viewModel) as ViewResult;
            var model = result?.ViewData.Model as ChangePasswordViewModel ?? new();

            //assert
            Assert.That(result!.ViewName, Is.EqualTo("ResetPassword"));
            Assert.That(model.Email, Is.EqualTo("test@email"));
            Assert.That(model.Token, Is.EqualTo("testToken"));
            Assert.That(result.ViewData.ModelState.ContainsKey("PasswordDoNotMatch"));

            _userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.VerifyUserTokenAsync(user,
                                                            It.IsAny<string>(), It.IsAny<string>(),
                                                            It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.ResetPasswordAsync(It.IsAny<AplicationUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task ChangePassword_ResetingPasswordWasUnsuccesfull_ReturnClientInfo()
        {
            //arrange
            var viewModel = new ChangePasswordViewModel
            {
                Email = "test@email",
                Token = "testToken",
                Password = "passworD1@",
                RepeatPassword = "passworD1@"
            };
            var user = new AplicationUser();
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.VerifyUserTokenAsync(It.IsAny<AplicationUser>(),
                         It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                        .ReturnsAsync(true);
            _userManager.Setup(x => x.ResetPasswordAsync(It.IsAny<AplicationUser>(), It.IsAny<string>(), It.IsAny<string>()))
                        .ReturnsAsync(new IdentityResultMock(false));
            //act
            var result = await _accountController.ChangePassword(viewModel) as RedirectToPageResult;

            //assert
            Assert.That(result!.PageName, Is.EqualTo("/ClientInfo"));
            Assert.That(result!.RouteValues!["info"] as string, Does.Contain("Something went wrong while changing your password."));

            _userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.VerifyUserTokenAsync(user,
                                                            It.IsAny<string>(), It.IsAny<string>(),
                                                            It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.ResetPasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }
        [Test]
        public async Task ChangePassword_ResetingPasswordWasSuccesfull_ReturnClientInfo()
        {
            //arrange
            var viewModel = new ChangePasswordViewModel
            {
                Email = "test@email",
                Token = "testToken",
                Password = "passworD1@",
                RepeatPassword = "passworD1@"
            };
            var user = new AplicationUser();
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.VerifyUserTokenAsync(It.IsAny<AplicationUser>(),
                         It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                        .ReturnsAsync(true);
            _userManager.Setup(x => x.ResetPasswordAsync(It.IsAny<AplicationUser>(), It.IsAny<string>(), It.IsAny<string>()))
                        .ReturnsAsync(new IdentityResultMock(true));
            //act
            var result = await _accountController.ChangePassword(viewModel) as RedirectToPageResult;

            //assert
            Assert.That(result!.PageName, Is.EqualTo("/ClientInfo"));
            Assert.That(result!.RouteValues!["info"] as string, Does.Contain("password has been changed"));

            _userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.VerifyUserTokenAsync(user,
                                                            It.IsAny<string>(), It.IsAny<string>(),
                                                            It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.ResetPasswordAsync(user, viewModel.Token, viewModel.Password), Times.Once());
        }
        #endregion
        #region Profile Endpoints
        [Test]
        public async Task Profile_UserIsNull_RedirectToLogin()
        {
            //assert 
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))!.ReturnsAsync(default(AplicationUser));

            //act
            var result = await _accountController.Profile() as RedirectToActionResult;

            //Assert
            Assert.That(result?.ActionName, Is.EqualTo("Login"));
            _userManager.Verify(x => x.GetRolesAsync(It.IsAny<AplicationUser>()), Times.Never());
        }
        [Test]
        public async Task Profile_ViewBagHasAJaxLink_ReturnView()
        {
            //arrange
            AplicationUser user = new AplicationUser();

            _userManager.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(user);

            var url = new Mock<IUrlHelper>();
            url.Setup(x => x.Action(It.IsAny<UrlActionContext>())).Returns("/test/testAction/dude");
            _accountController.Url = url.Object;

            //act
            await _accountController.Profile();
            var viewBagValue = _accountController.ViewBag.AjaxLink;

            //assert 
            Assert.That(viewBagValue, Is.EqualTo("/test/testAction/dude"));
            url.Verify(x => x.Action(It.IsAny<UrlActionContext>()), Times.Once());
        }
        [Test]
        public async Task Profile_EverythingWorksSmoth_ReturnView()
        {
            //arrange
            AplicationUser user = new AplicationUser()
            {
                UserName = "TestName",
                Email = "TestEmail",
                PhoneNumber = "TestPhone"
            };
            _userManager.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.GetRolesAsync(It.IsAny<AplicationUser>())).ReturnsAsync(new List<string> { "Admin" });
            var url = new Mock<IUrlHelper>();
            _accountController.Url = url.Object;

            //act
            var result = (await _accountController.Profile() as ViewResult)?.ViewData.Model as ProfileViewModel ?? new();

            //Assert
            Assert.That(result.UserName, Is.EqualTo("TestName"));
            Assert.That(result.Email, Is.EqualTo("TestEmail"));
            Assert.That(result.Phone, Is.EqualTo("TestPhone"));
            Assert.That(result.UserRole, Is.EqualTo("Admin"));
        }
        [Test]
        public async Task Profile_RoleAdminListIsEmpty_ReturnView()
        {
            //arrange
            AplicationUser user = new AplicationUser()
            {
                UserName = "TestName",
                Email = "TestEmail",
                PhoneNumber = "TestPhone"
            };
            _userManager.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.GetRolesAsync(It.IsAny<AplicationUser>())).ReturnsAsync(new List<string>());
            var url = new Mock<IUrlHelper>();
            _accountController.Url = url.Object;

            //act
            var result = (await _accountController.Profile() as ViewResult)?.ViewData.Model as ProfileViewModel ?? new();

            //Assert
            Assert.That(result.UserRole, Is.EqualTo("RoleNotFound"));
        }
        [Test]
        public async Task UserChangePassword_ModelIsNotValid_ReturnBadRequest()
        {
            //assert
            _accountController.ModelState.AddModelError("error", "custom error");

            //act
            var result = await _accountController.UserChangePassword(new UserChangePassword()) as BadRequestObjectResult;

            //assert
            Assert.That(result?.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
            _userManager.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Never());
        }
        [Test]
        [TestCase("")]
        [TestCase(null)]
        public async Task UserChangePassword_ModelPropertiesAreNullOrEmpty_ReturnBadRequest(string password)
        {
            //assert
            UserChangePassword model = new UserChangePassword()
            {
                OldPassword = password,
                NewPassword = password,
                ConfirmPassword = password
            };

            //act
            var result = await _accountController.UserChangePassword(new UserChangePassword()) as BadRequestObjectResult;
            var obj = result?.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result?.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
            Assert.That(obj.Error, Is.EqualTo("FieldsRequired"));
            _userManager.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task UserChangePassword_UserIsNull_ReturnNotFound()
        {
            //assert
            UserChangePassword model = new UserChangePassword()
            {
                OldPassword = "test",
                NewPassword = "TestTest",
                ConfirmPassword = "TestTest"
            };
            //act
            var result = await _accountController.UserChangePassword(model) as NotFoundObjectResult;
            var obj = result?.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result?.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
            Assert.That(obj.Error, Is.EqualTo("UserNotFound"));
            _userManager.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.CheckPasswordAsync(It.IsAny<AplicationUser>(), It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task UserChangePassword_OldPasswordNotMatching_ReturnBadRequest()
        {
            //assert
            UserChangePassword model = new UserChangePassword()
            {
                OldPassword = "test",
                NewPassword = "TestTest",
                ConfirmPassword = "TestTest"
            };
            AplicationUser user = new AplicationUser()
            {
                UserName = "TestName",
                Email = "TestEmail",
                PhoneNumber = "TestPhone"
            };
            _userManager.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.CheckPasswordAsync(It.IsAny<AplicationUser>(), It.IsAny<string>())).ReturnsAsync(false);

            //act
            var result = await _accountController.UserChangePassword(model) as BadRequestObjectResult;
            var obj = result?.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result?.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
            Assert.That(obj.Error, Is.EqualTo("NotMatch"));
            _userManager.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.CheckPasswordAsync(user, "test"), Times.Once());
            _userManager.Verify(x => x.ChangePasswordAsync(It.IsAny<AplicationUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task UserChangePassword_WeakPassword_ReturnBadRequest()
        {
            //assert
            UserChangePassword model = new UserChangePassword()
            {
                OldPassword = "test",
                NewPassword = "TestTest",
                ConfirmPassword = "TestTest"
            };
            AplicationUser user = new AplicationUser()
            {
                UserName = "TestName",
                Email = "TestEmail",
                PhoneNumber = "TestPhone"
            };
            _userManager.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.CheckPasswordAsync(It.IsAny<AplicationUser>(), It.IsAny<string>())).ReturnsAsync(true);

            //act
            var result = await _accountController.UserChangePassword(model) as BadRequestObjectResult;
            var obj = result?.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result?.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
            Assert.That(obj.Error, Is.EqualTo("WeakPassword"));
            _userManager.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.CheckPasswordAsync(user, "test"), Times.Once());
            _userManager.Verify(x => x.ChangePasswordAsync(It.IsAny<AplicationUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task UserChangePassword_PasswordsDontMatch_ReturnBadRequest()
        {
            //assert
            UserChangePassword model = new UserChangePassword()
            {
                OldPassword = "test",
                NewPassword = "Test@2",//pass the integrity test
                ConfirmPassword = "TestTest"//not matching
            };
            AplicationUser user = new AplicationUser()
            {
                UserName = "TestName",
                Email = "TestEmail",
                PhoneNumber = "TestPhone"
            };
            _userManager.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.CheckPasswordAsync(It.IsAny<AplicationUser>(), It.IsAny<string>())).ReturnsAsync(true);

            //act
            var result = await _accountController.UserChangePassword(model) as BadRequestObjectResult;
            var obj = result?.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result?.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
            Assert.That(obj.Error, Is.EqualTo("NotMatch"));
            Assert.That(obj.Message, Is.EqualTo("Passwords don't match"));
            _userManager.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.CheckPasswordAsync(user, "test"), Times.Once());
            _userManager.Verify(x => x.ChangePasswordAsync(It.IsAny<AplicationUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task UserChangePassword_ChangingPasswordDidNotSuccededForUnknownReasons_ReturnConflict()
        {
            //assert
            UserChangePassword model = new UserChangePassword()
            {
                OldPassword = "test",
                NewPassword = "Test@2",//pass the integrity test
                ConfirmPassword = "Test@2"//pass passwords matching test
            };
            AplicationUser user = new AplicationUser()
            {
                UserName = "TestName",
                Email = "TestEmail",
                PhoneNumber = "TestPhone"
            };
            _userManager.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.CheckPasswordAsync(It.IsAny<AplicationUser>(), It.IsAny<string>())).ReturnsAsync(true);
            _userManager.Setup(x => x.ChangePasswordAsync(It.IsAny<AplicationUser>(), It.IsAny<string>(), It.IsAny<string>()))
                        .ReturnsAsync(new IdentityResultMock(false));

            //act
            var result = await _accountController.UserChangePassword(model) as ConflictObjectResult;
            var obj = result?.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result?.StatusCode, Is.EqualTo(StatusCodes.Status409Conflict));
            Assert.That(obj.Error, Is.EqualTo("UnknownReason"));
            _userManager.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.CheckPasswordAsync(user, "test"), Times.Once());
            _userManager.Verify(x => x.ChangePasswordAsync(user, model.OldPassword, model.NewPassword), Times.Once());
        }
        [Test]
        public async Task UserChangePassword_Succeded_ReturnOk()
        {
            //assert
            UserChangePassword model = new UserChangePassword()
            {
                OldPassword = "test",
                NewPassword = "Test@2",//pass the integrity test
                ConfirmPassword = "Test@2"//pass passwords matching test
            };
            AplicationUser user = new AplicationUser()
            {
                UserName = "TestName",
                Email = "TestEmail",
                PhoneNumber = "TestPhone"
            };
            _userManager.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.CheckPasswordAsync(It.IsAny<AplicationUser>(), It.IsAny<string>())).ReturnsAsync(true);
            _userManager.Setup(x => x.ChangePasswordAsync(It.IsAny<AplicationUser>(), It.IsAny<string>(), It.IsAny<string>()))
                        .ReturnsAsync(new IdentityResultMock(true));

            //act
            var result = await _accountController.UserChangePassword(model) as OkObjectResult;
            var obj = result?.Value as SuccesResponse ?? new();

            //assert
            Assert.That(result?.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
            Assert.That(obj.Message, Is.EqualTo("Password changed succesfully!"));
            _userManager.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Once());
            _userManager.Verify(x => x.CheckPasswordAsync(user, "test"), Times.Once());
            _userManager.Verify(x => x.ChangePasswordAsync(user, model.OldPassword, model.NewPassword), Times.Once());
        }
        #endregion
    }

    //i could not mock SignInResult so i had to use inheritance to change the succeded value.
    //which could be set only in an inheritance tree.
    public class SignInResultMock : Microsoft.AspNetCore.Identity.SignInResult
    {
        public SignInResultMock(bool succedValue)
        {
            Succeeded = succedValue;
        }
    }
    public class IdentityResultMock : IdentityResult
    {
        public IdentityResultMock(bool succedValue)
        {
            Succeeded = succedValue;
        }
    }

    //WrapIURLHelper. In One test method i use an Extension method that i cannot mock. I have to mock it manually.
    public class ManualMockUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext => throw new NotImplementedException();

        public string? Action(UrlActionContext actionContext)
        {
            return "localTest/test/link";
        }

        [return: NotNullIfNotNull("contentPath")]
        public string? Content(string? contentPath)
        {
            throw new NotImplementedException();
        }

        public bool IsLocalUrl([NotNullWhen(true)] string? url)
        {
            throw new NotImplementedException();
        }

        public string? Link(string? routeName, object? values)
        {
            throw new NotImplementedException();
        }

        public string? RouteUrl(UrlRouteContext routeContext)
        {
            throw new NotImplementedException();
        }
    }
}
