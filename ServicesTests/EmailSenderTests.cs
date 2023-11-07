using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using scriptbuster.dev.Services.EmailService;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using scriptbuster.dev.Infrastructure.Services;
using System.Net.Mail;
using System.Net;

namespace scriptbuster.dev_UnitTests.ServicesTests
{
    [TestFixture]
    internal class EmailSenderTests
    {
        private Mock<ILogger<EmailSender>> _logger;
        private Mock<IOptions<EmailSettings>> _settings;
        private Mock<ISmtpClientWrapper> _smtpClient;
        private EmailSender _sender;
        [SetUp]
        public void SetUp()
        {
            _logger = new Mock<ILogger<EmailSender>>();
            _settings = new Mock<IOptions<EmailSettings>>();
            _settings.SetupGet(x => x.Value).Returns(new EmailSettings
            {
                Host = "host",
                UserName = "admin@test.com",
                Password = "passwordTest",
            });
            _smtpClient = new Mock<ISmtpClientWrapper>();
            _sender = new EmailSender(_logger.Object, _settings.Object, _smtpClient.Object);
        }
        [Test]
        [TestCase(null)]
        [TestCase("")]
        public void SendEmail_ToEmailIsNullOrEmpty_ThrowsException(string toEmail)
        {
            //assert
            _smtpClient.Verify(x => x.SendMailAsync(It.IsAny<MailMessage>()), Times.Never());
            Assert.ThrowsAsync<ArgumentNullException>(() => _sender.SendEmail(toEmail, "Test", "Test"));
        }

        [Test]
        public async Task SendEmail_CanSendEmptyMessage_MessageAndSubjectAreNull()
        {

            //act -- it will throw a format exception if there is no valid email (miss @ or .)
            await _sender.SendEmail("client@test.com", null, null);

            //assert
            _smtpClient.Verify(x => x.SendMailAsync(It.IsAny<MailMessage>()), Times.Once());
        }

        [Test]
        public async Task SendEmail_AdminCredentialsAreOk()
        {
            _smtpClient.SetupGet(x => x.Client).Returns(new SmtpClient { Credentials = new NetworkCredential("admin@test.com", "passwordTest") });
            //act -- it will throw a format exception if there is no valid email (miss @ or .)
            await _sender.SendEmail("client@test.com", "Test", "Test");

            var credentials = _smtpClient.Object.Client.Credentials as NetworkCredential ?? new();

            //assert
            _smtpClient.Verify(x => x.SendMailAsync(It.IsAny<MailMessage>()), Times.Once());
            Assert.That(credentials.UserName, Is.EqualTo("admin@test.com"));
            Assert.That(credentials.Password, Is.EqualTo("passwordTest"));
        }

    }
}
