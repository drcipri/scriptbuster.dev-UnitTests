using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using scriptbuster.dev.Controllers;
using scriptbuster.dev.Infrastructure.ViewModels.AuthenticationController;
using scriptbuster.dev.Services.CookieService;
using scriptbuster.dev.Services.SessionService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace scriptbuster.dev_UnitTests
{
    [TestFixture]
    internal class AccountControlllerTests
    {
        private Mock<UserManager<IdentityUser>> _userManager;
        private Mock<SignInManager<IdentityUser>> _signInManager;
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
            _userManager = new Mock<UserManager<IdentityUser>>(new Mock<IUserStore<IdentityUser>>().Object, null, null, null, null, null, null, null, null);
            _signInManager = new Mock<SignInManager<IdentityUser>>
                (_userManager.Object, _accesor.Object, new Mock<IUserClaimsPrincipalFactory<IdentityUser>>().Object, null, null, null, null);
            _accountController = new AccountController(_userManager.Object, _signInManager.Object, _logger.Object,
                                                       _emailSender.Object, _accesor.Object, _cache.Object, _session.Object,
                                                       _cookieService.Object);
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
            _cache.Setup(x => x.GetAsync(It.IsAny<string>(),default)).ReturnsAsync(logginAttempts);

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
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))!.ReturnsAsync(default(IdentityUser));
            _userManager.Setup(x => x.FindByNameAsync(It.IsAny<string>()))!.ReturnsAsync(default(IdentityUser));

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
            var user = new IdentityUser { UserName = "Test" };
            _userManager.Setup(x => x.FindByNameAsync(It.IsAny<string>()))!.ReturnsAsync(user);
           
            _signInManager.Setup(x => x.PasswordSignInAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), false, false))
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
            _signInManager.Verify(x => x.PasswordSignInAsync(It.Is<IdentityUser>(x => x == user), It.Is<string>(x => x == model.Password),false, false),Times.Once());
        }
        [Test]
        public async Task LoginUser_Succeded_RedirectToAdminPageReturnUrlIsNull()
        {
            //arrange
            var user = new IdentityUser { UserName = "Test" };
            _userManager.Setup(x => x.FindByNameAsync(It.IsAny<string>()))!.ReturnsAsync(user);

            _signInManager.Setup(x => x.PasswordSignInAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), false, false))
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
            _signInManager.Verify(x => x.PasswordSignInAsync(It.Is<IdentityUser>(x => x == user), It.Is<string>(x => x == model.Password), false, false), Times.Once());
        }
        [Test]
        public async Task LoginUser_Succeded_RedirectToReturnUrlBecauseItsNotNull()
        {
            //arrange
            var user = new IdentityUser { UserName = "Test" };
            _userManager.Setup(x => x.FindByNameAsync(It.IsAny<string>()))!.ReturnsAsync(user);

            _signInManager.Setup(x => x.PasswordSignInAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), false, false))
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
            _signInManager.Verify(x => x.PasswordSignInAsync(It.Is<IdentityUser>(x => x == user), It.Is<string>(x => x == model.Password), false, false), Times.Once());
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
    }

    //i could not mock SignInResult so i had to use inheritance to change the succeded value.
    //which could be set only in an inheritance tree.
    public class SignInResultMock: Microsoft.AspNetCore.Identity.SignInResult
    {
        public SignInResultMock(bool succedValue)
        {
            Succeeded = succedValue;
        }
    }
}
