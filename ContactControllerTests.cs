using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using scriptbuster.dev.Controllers;
using scriptbuster.dev.Models.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using scriptbuster.dev.Services.EmailService;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace scriptbuster.dev_UnitTests
{
    [TestFixture]
    internal class ContactControllerTests
    {
        private Mock<ILogger<ContactController>> _logger;
        private Mock<IEmailSender> _mailSender;  
        private Mock<IRepositoryMessage> _repositoryMessage;
        private Mock<IConfiguration> _configuration;
        private ContactController _controller;
        [SetUp]
        public void SetUp()
        {
            _logger = new Mock<ILogger<ContactController>>();
            _repositoryMessage = new Mock<IRepositoryMessage>();
            _mailSender = new Mock<IEmailSender>();
            _configuration = new Mock<IConfiguration>();
            _controller = new ContactController(_logger.Object, _repositoryMessage.Object, _mailSender.Object);
        }

        [Test]
        public async Task SendMessage_ModelStateIsNotValid_ReturnView()
        {
            //arrange
            _controller.ModelState.AddModelError("error", "error message");
            var message = new Message
            {
                Id = 1,
                FullName = "Test",
                Email = "Test@test",
                ClientMessage = "New client Message"
            };

            //act
            var result = await _controller.SendMessage(message, _configuration.Object) as ViewResult;

            //assert
            Assert.That(result?.ViewName, Is.EqualTo("Index"));
            Assert.That(result.ViewData.ModelState.ContainsKey("error"), Is.True);
        }

        [Test]
        public async Task SendMessage_WorksButSendingEmailThrowsErrorButItWontBreakTheEndpoint_ReturnClientInfo()
        {
            //arrange
            _mailSender.Setup(x => x.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                       .ThrowsAsync(new Exception("Test exception"));
            var message = new Message
            {
                Id = 1,
                FullName = "Test",
                Email = "Test@test",
                ClientMessage = "New client Message"
            };

            //act
           var result = (await _controller.SendMessage(message, _configuration.Object)) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/ClientInfo"));
            _mailSender.Verify(x => x.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            Assert.ThrowsAsync<Exception>(() => _mailSender.Object.SendEmail("test", "test", "test"), "Test exception");
        }
        [Test]
        public async Task SendMessage_EverythingWOrks_ReturnClientInfo()
        {
            var message = new Message
            {
                Id = 1,
                FullName = "Test",
                Email = "Test@test",
                ClientMessage = "New client Message"
            };

            //act
            var result = (await _controller.SendMessage(message, _configuration.Object)) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/ClientInfo"));
            _mailSender.Verify(x => x.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.AtLeast(2));
        }

    }
}
