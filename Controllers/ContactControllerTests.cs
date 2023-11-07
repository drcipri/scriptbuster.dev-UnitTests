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

namespace scriptbuster.dev_UnitTests.Controllers
{
    [TestFixture]
    internal class ContactControllerTests
    {
        private Mock<ILogger<ContactController>> _logger;
        private Mock<IEmailSender> _mailSender;
        private Mock<IRepositoryMessage> _repositoryMessage;
        private Mock<IServiceProvider> _serviceProvider;
        private Mock<IRepositoryProjectMessage> _repositoryProjectMessage;
        private ContactController _controller;

        [SetUp]
        public void SetUp()
        {
            _logger = new Mock<ILogger<ContactController>>();
            _repositoryMessage = new Mock<IRepositoryMessage>();
            _mailSender = new Mock<IEmailSender>();
            _serviceProvider = new Mock<IServiceProvider>();
            _repositoryProjectMessage = new Mock<IRepositoryProjectMessage>();
            _controller = new ContactController(_logger.Object, _repositoryMessage.Object, _repositoryProjectMessage.Object,
                                                _mailSender.Object, _serviceProvider.Object);
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
            var result = await _controller.SendMessage(message) as ViewResult;

            //assert
            Assert.That(result?.ViewName, Is.EqualTo("Index"));
            Assert.That(result.ViewData.ModelState.ContainsKey("error"), Is.True);
            Assert.That((bool)result.ViewData["DisplayErrors"]!, Is.True);
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
            var result = await _controller.SendMessage(message) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/ClientInfo"));
        }

        [Test]
        public async Task SendProjectMessage_ModelStateIsNotValid_ReturnView()
        {
            //arrange
            _controller.ModelState.AddModelError("error", "error message");
            var message = new ProjectMessage
            {
                Id = 1,
                FullName = "Test",
                Email = "Test@test",
                ProjectDescription = "New client Message"
            };

            //act
            var result = await _controller.SendProjectMessage(message) as ViewResult;

            //assert
            Assert.That(result?.ViewName, Is.EqualTo("Index"));
            Assert.That(result.ViewData.ModelState.ContainsKey("error"), Is.True);
            Assert.That((bool)result.ViewData["DisplayErrors"]!, Is.True);
        }

        [Test]
        public async Task SendProjectMessage_EverythingWOrks_ReturnClientInfo()
        {
            var message = new ProjectMessage
            {
                Id = 1,
                FullName = "Test",
                Email = "Test@test",
                ProjectDescription = "New client Message"
            };

            //act
            var result = await _controller.SendProjectMessage(message) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/ClientInfo"));
        }

    }
}
