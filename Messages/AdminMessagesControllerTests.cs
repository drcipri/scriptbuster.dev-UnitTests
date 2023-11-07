using scriptbuster.dev.Controllers;
using scriptbuster.dev.Models.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using scriptbuster.dev.Infrastructure.ViewModels.AdminMessagesController;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http.Extensions;

namespace scriptbuster.dev_UnitTests.Messages
{
    [TestFixture]
    internal class AdminMessagesControllerTests
    {
        private Mock<IRepositoryMessage> _mockRepMessages;
        private Mock<IRepositoryProjectMessage> _mockProjectMessages;
        private Mock<ILogger<AdminMessagesController>> _mockLogger;
        private Mock<IServiceProvider> _mockServiceProvider;
        private Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private Mock<LinkGenerator> _linkGenerator;
        private Mock<HttpContext> _httpContextMock;
        private Mock<HttpRequest> _httpRequestMock;
        private AdminMessagesController _controller;
        [SetUp]
        public void SetUp()
        {
            _mockRepMessages = new Mock<IRepositoryMessage>();
            _mockLogger = new Mock<ILogger<AdminMessagesController>>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockProjectMessages = new Mock<IRepositoryProjectMessage>();
            _httpRequestMock = new Mock<HttpRequest>();
            _httpContextMock = new Mock<HttpContext>();
            _linkGenerator = new Mock<LinkGenerator>();
            _controller = new AdminMessagesController(_mockRepMessages.Object, _mockLogger.Object,
                                                      _mockServiceProvider.Object, _mockProjectMessages.Object);
        }
        public async IAsyncEnumerable<Message> MockGetMessagesByDescending()
        {
            var messagesList = new List<Message>
            {
                new Message
                {
                    Id = 1,
                    FullName = "Foo",
                    Email = "foo@email.com",
                    ClientMessage = "Foo message",
                    PostDate = DateTime.Now,
                    Status = new MessageStatus{Id = 1, StatusName= "Unread" },
                    StatusId = 1
                },
                new Message
                {
                    Id = 2,
                    FullName = "Foo2",
                    Email = "foo2@email.com",
                    ClientMessage = "Foo2 message",
                    PostDate = DateTime.Now,
                    Status = new MessageStatus{Id = 1, StatusName= "Unread" },
                    StatusId = 1
                },
                new Message
                {
                    Id = 3,
                    FullName = "Foo3",
                    Email = "foo3@email.com",
                    ClientMessage = "Foo3 message",
                    PostDate = DateTime.Now,
                    Status = new MessageStatus{Id = 1, StatusName= "Unread" },
                    StatusId = 1
                },
                new Message
                {
                    Id = 4,
                    FullName = "Foo4",
                    Email = "foo4@email.com",
                    ClientMessage = "Foo4 message",
                    PostDate = DateTime.Now,
                    Status = new MessageStatus{Id = 1, StatusName= "Unread" },
                    StatusId = 1
                },
            };

            foreach (var message in messagesList.OrderByDescending(x => x.Id))
            {
                yield return message;
            }
            await Task.CompletedTask;
        }

        public async IAsyncEnumerable<Message> MockSearch()
        {
            var messagesList = new List<Message>
            {
                new Message
                {
                    Id = 1,
                    FullName = "Foo",
                    Email = "foo@email.com",
                    ClientMessage = "Foo message",
                    PostDate = DateTime.Now,
                    Status = new MessageStatus{Id = 1, StatusName= "Unread" },
                    StatusId = 1
                },
                new Message
                {
                    Id = 2,
                    FullName = "Foo2",
                    Email = "foo2@email.com",
                    ClientMessage = "Foo2 message",
                    PostDate = DateTime.Now,
                    Status = new MessageStatus{Id = 1, StatusName= "Unread" },
                    StatusId = 1
                },
                new Message
                {
                    Id = 3,
                    FullName = "Foo3",
                    Email = "foo3@email.com",
                    ClientMessage = "Foo3 message",
                    PostDate = DateTime.Now,
                    Status = new MessageStatus{Id = 1, StatusName= "Unread" },
                    StatusId = 1
                },
                new Message
                {
                    Id = 4,
                    FullName = "Foo4",
                    Email = "foo4@email.com",
                    ClientMessage = "Foo4 message",
                    PostDate = DateTime.Now,
                    Status = new MessageStatus{Id = 1, StatusName= "Unread" },
                    StatusId = 1
                },
                new Message
                {
                    Id = 5,
                    FullName = "Foo",
                    Email = "foo@email.com",
                    ClientMessage = "Foo message",
                    PostDate = DateTime.Now,
                    Status = new MessageStatus{Id = 1, StatusName= "Unread" },
                    StatusId = 1
                },
                new Message
                {
                    Id = 6,
                    FullName = "Foo2",
                    Email = "foo2@email.com",
                    ClientMessage = "Foo2 message",
                    PostDate = DateTime.Now,
                    Status = new MessageStatus{Id = 1, StatusName= "Unread" },
                    StatusId = 1
                },
                new Message
                {
                    Id = 7,
                    FullName = "Foo3",
                    Email = "foo3@email.com",
                    ClientMessage = "Foo3 message",
                    PostDate = DateTime.Now,
                    Status = new MessageStatus{Id = 1, StatusName= "Unread" },
                    StatusId = 1
                },
                new Message
                {
                    Id = 8,
                    FullName = "Foo8",
                    Email = "foo4@email.com",
                    ClientMessage = "Foo8 message",
                    PostDate = DateTime.Now,
                    Status = new MessageStatus{Id = 1, StatusName= "Unread" },
                    StatusId = 1
                },
            };

            foreach (var message in messagesList)
            {
                yield return message;
            }
            await Task.CompletedTask;
        }

        //Messages
        #region MessagesPanel
        [Test]
        [TestCase(-1)]
        [TestCase(0)]
        public async Task MessagesPanel_MessagesPageIsLessOrEqual0_ReturnMesagesPanelAction(int page)
        {
            //act
            var result = await _controller.MessagesPanel(page) as RedirectToActionResult;

            //assert
            Assert.That(result?.ActionName, Is.EqualTo("MessagesPanel"));
            Assert.That(result?.RouteValues?["messagesPage"], Is.EqualTo(1));
            _mockRepMessages.Verify(x => x.GetMessagesByDescending(It.IsAny<int>(), It.IsAny<int>()), Times.Never());
        }
        [Test]
        public async Task MessagesPanel_CanGetMessages_ReturnView()
        {
            //arrange
            _mockRepMessages.Setup(x => x.GetMessagesByDescending(It.IsAny<int>(), It.IsAny<int>())).Returns(MockGetMessagesByDescending());
            _mockServiceProvider.Setup(x => x.GetService(typeof(LinkGenerator))).Returns(_linkGenerator.Object);
            _mockServiceProvider.Setup(x => x.GetService(typeof(IHttpContextAccessor))).Returns(_mockHttpContextAccessor.Object);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_httpContextMock.Object);
            _httpContextMock.Setup(x => x.Request).Returns(_httpRequestMock.Object);
            //mock get currentURL. It uses GetEncodedUrl that needs Requests.scheme, Host etc..
            _httpRequestMock.Setup(x => x.Scheme).Returns("http");
            _httpRequestMock.Setup(x => x.Host).Returns(new HostString("localhost", 5000));


            //act
            var result = (await _controller.MessagesPanel(1) as ViewResult)?.ViewData.Model as MessagesPanelViewModel ?? new();
            var messagesList = result.Messages.ToList();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(messagesList.Count, Is.EqualTo(4));
                Assert.That(messagesList[0].Id, Is.EqualTo(4));
                Assert.That(messagesList[0].FullName, Is.EqualTo("Foo4"));
                Assert.That(messagesList[0].Email, Is.EqualTo("foo4@email.com"));
                Assert.That(messagesList[3].Id, Is.EqualTo(1));
                Assert.That(messagesList[3].FullName, Is.EqualTo("Foo"));
                Assert.That(messagesList[3].Email, Is.EqualTo("foo@email.com"));
            });
        }

        [Test]
        public async Task MessagesPanel_CanGetUnreadMessagesAndTotalMessagesCount_ReturnView()
        {
            //arrange
            _mockRepMessages.Setup(x => x.GetMessagesByDescending(It.IsAny<int>(), It.IsAny<int>())).Returns(MockGetMessagesByDescending());
            _mockRepMessages.Setup(x => x.GetTotalMessages()).ReturnsAsync(4);
            _mockRepMessages.Setup(x => x.GetUnreadMessages()).ReturnsAsync(4);
            _mockServiceProvider.Setup(x => x.GetService(typeof(LinkGenerator))).Returns(_linkGenerator.Object);
            _mockServiceProvider.Setup(x => x.GetService(typeof(IHttpContextAccessor))).Returns(_mockHttpContextAccessor.Object);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_httpContextMock.Object);
            _httpContextMock.Setup(x => x.Request).Returns(_httpRequestMock.Object);
            //mock get currentURL. It uses GetEncodedUrl that needs Requests.scheme, Host etc..
            _httpRequestMock.Setup(x => x.Scheme).Returns("http");
            _httpRequestMock.Setup(x => x.Host).Returns(new HostString("localhost", 5000));


            //act
            var result = await _controller.MessagesPanel(1) as ViewResult;

            //assert
            Assert.Multiple(() =>
            {
                Assert.That((int)result?.ViewData["UnreadMessages"]!, Is.EqualTo(4));
                Assert.That((int)result?.ViewData["TotalMessages"]!, Is.EqualTo(4));
            });
        }
        [Test]
        public async Task MessagesPanel_CanCreatePaginationLinksAndReturnCurrentRequestLink_ReturnView()
        {
            //arrange
            _mockRepMessages.Setup(x => x.GetMessagesByDescending(It.IsAny<int>(), It.IsAny<int>())).Returns(MockGetMessagesByDescending());
            _mockRepMessages.Setup(x => x.GetTotalMessages()).ReturnsAsync(49);//49/7(PageSize) = 7 Assert
            _mockServiceProvider.Setup(x => x.GetService(typeof(LinkGenerator))).Returns(_linkGenerator.Object);
            _mockServiceProvider.Setup(x => x.GetService(typeof(IHttpContextAccessor))).Returns(_mockHttpContextAccessor.Object);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_httpContextMock.Object);
            _httpContextMock.Setup(x => x.Request).Returns(_httpRequestMock.Object);
            //mock get currentURL. It uses GetEncodedUrl that needs Requests.scheme, Host etc..
            _httpRequestMock.Setup(x => x.Scheme).Returns("http");
            _httpRequestMock.Setup(x => x.Host).Returns(new HostString("localhost", 5000));


            //act
            var result = (await _controller.MessagesPanel(1) as ViewResult)?.ViewData.Model as MessagesPanelViewModel ?? new();
            var paginationsLinks = result.PaginationLinks;
            //assert
            Assert.Multiple(() =>
            {
                Assert.That(paginationsLinks.Count(), Is.EqualTo(7));
                Assert.That(result.CurrentUrl, Is.EqualTo("http://localhost:5000/"));
            });
        }
        #endregion
        #region Message
        [Test]
        [TestCase(0)]
        [TestCase(-1)]
        public async Task Message_MessageIdIsLessThanZero_RedirectToMEssagesPanel(int messageId)
        {
            //act
            var result = await _controller.Message(messageId) as RedirectToActionResult;

            //assert
            Assert.That(result?.ActionName, Is.EqualTo("MessagesPanel"));
            _mockRepMessages.Verify(x => x.GetMessage(It.IsAny<int>()), Times.Never());
        }
        [Test]
        public async Task Message_MessageIsNotFound_ReturnErrorInfo()
        {
            //arrange
            _mockRepMessages.Setup(x => x.GetMessage(It.IsAny<int>())).ReturnsAsync(default(Message));

            //act
            var result = await _controller.Message(1) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/ErrorInfo"));
            _mockRepMessages.Verify(x => x.GetMessage(It.Is<int>(x => x == 1)), Times.Once());
        }
        [Test]
        public async Task Message_MessageIsFoundAndSetToRead_ReturnsView()
        {
            //arrange
            var message = new Message
            {
                Id = 1,
                FullName = "Foo",
                Email = "foo@email.com",
                ClientMessage = "Foo message",
                PostDate = DateTime.Now,
                Status = new MessageStatus { Id = 1, StatusName = "Unread" },
                StatusId = 1
            };
            _mockRepMessages.Setup(x => x.GetMessage(It.IsAny<int>())).ReturnsAsync(message);

            //act
            var result = (await _controller.Message(1) as ViewResult)?.ViewData.Model as Message ?? new();

            //assert
            Assert.That(result.Id, Is.EqualTo(1));
            Assert.That(result.Email, Is.EqualTo("foo@email.com"));
            _mockRepMessages.Verify(x => x.GetMessage(It.Is<int>(x => x == 1)), Times.Once());
            _mockRepMessages.Verify(x => x.SetMessageToRead(It.Is<Message>(x => x == message)));
        }
        #endregion
        #region DeleteMessage
        [Test]
        public async Task DeleteMessage_MessageIsNotFound_ReturnErrorInfo()
        {
            //arrange
            _mockRepMessages.Setup(x => x.DeleteMessage(It.IsAny<int>())).ReturnsAsync(false);

            //act
            var result = await _controller.DeleteMessage(1) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/ErrorInfo"));
            _mockRepMessages.Verify(x => x.DeleteMessage(It.Is<int>(x => x == 1)), Times.Once());
        }
        [Test]
        public async Task DeleteMessage_MessageIsFoundMessageIsDeleted_ReturnMessagesPanel()
        {
            //arrange
            _mockRepMessages.Setup(x => x.DeleteMessage(It.IsAny<int>())).ReturnsAsync(true);

            //act
            var result = await _controller.DeleteMessage(1) as RedirectToActionResult;

            //assert
            Assert.That(result?.ActionName, Is.EqualTo("MessagesPanel"));
            _mockRepMessages.Verify(x => x.DeleteMessage(It.Is<int>(x => x == 1)), Times.Once());
        }
        #endregion
        #region SearchMessages
        [Test]
        [TestCase("Test", -1)]
        [TestCase("Test", 0)]
        [TestCase("", 1)]
        [TestCase(default, 1)]
        public async Task SearchMessages_MessagesIsLessOrEqualZeroOrSearchCriteriaIsNullOrEmpty_ReturnMessgesPanel(string searchCriteria, int messagesPage)
        {
            //act
            var result = await _controller.SearchMessages(searchCriteria, messagesPage) as RedirectToActionResult;

            //assert
            Assert.That(result?.ActionName, Is.EqualTo("MessagesPanel"));
            _mockRepMessages.Verify(x => x.Search(It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task SearchMessages_CanCountUnreadAndTotalMessages_ReturnMessagesPanelView()
        {
            //arrange
            _mockRepMessages.Setup(x => x.Search(It.IsAny<string>())).Returns(MockSearch());
            _mockRepMessages.Setup(x => x.GetTotalMessages()).ReturnsAsync(8);
            _mockRepMessages.Setup(x => x.GetUnreadMessages()).ReturnsAsync(8);
            _mockServiceProvider.Setup(x => x.GetService(typeof(LinkGenerator))).Returns(_linkGenerator.Object);
            _mockServiceProvider.Setup(x => x.GetService(typeof(IHttpContextAccessor))).Returns(_mockHttpContextAccessor.Object);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_httpContextMock.Object);
            _httpContextMock.Setup(x => x.Request).Returns(_httpRequestMock.Object);
            //mock get currentURL. It uses GetEncodedUrl that needs Requests.scheme, Host etc..
            _httpRequestMock.Setup(x => x.Scheme).Returns("http");
            _httpRequestMock.Setup(x => x.Host).Returns(new HostString("localhost", 5000));

            //act
            var result = await _controller.SearchMessages("test") as ViewResult;

            //assert
            Assert.Multiple(() =>
            {
                Assert.That((int)result?.ViewData["UnreadMessages"]!, Is.EqualTo(8));
                Assert.That((int)result?.ViewData["TotalMessages"]!, Is.EqualTo(8));
                Assert.That(_controller.ViewBag.SearchCriteria, Is.EqualTo("test"));
            });
        }

        [Test]
        public async Task SearchMessages_CanPaginatePage2_ReturnMessagesPanelView()
        {
            //arrange
            _mockRepMessages.Setup(x => x.Search(It.IsAny<string>())).Returns(MockSearch());
            _mockRepMessages.Setup(x => x.GetTotalMessages()).ReturnsAsync(8);
            _mockRepMessages.Setup(x => x.GetUnreadMessages()).ReturnsAsync(8);
            _mockServiceProvider.Setup(x => x.GetService(typeof(LinkGenerator))).Returns(_linkGenerator.Object);
            _mockServiceProvider.Setup(x => x.GetService(typeof(IHttpContextAccessor))).Returns(_mockHttpContextAccessor.Object);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_httpContextMock.Object);
            _httpContextMock.Setup(x => x.Request).Returns(_httpRequestMock.Object);
            //mock get currentURL. It uses GetEncodedUrl that needs Requests.scheme, Host etc..
            _httpRequestMock.Setup(x => x.Scheme).Returns("http");
            _httpRequestMock.Setup(x => x.Host).Returns(new HostString("localhost", 5000));

            //act
            var result = (await _controller.SearchMessages("test", 2) as ViewResult)?.ViewData.Model as MessagesPanelViewModel ?? new();
            var listMessages = result.Messages.ToList();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(listMessages.Count, Is.EqualTo(1));
                Assert.That(listMessages[0].Id, Is.EqualTo(1));
                Assert.That(listMessages[0].FullName, Is.EqualTo("Foo"));
                Assert.That(listMessages[0].Email, Is.EqualTo("foo@email.com"));
                Assert.That(listMessages[0].ClientMessage, Is.EqualTo("Foo message"));
            });
            _mockRepMessages.Verify(x => x.Search(It.Is<string>(x => x == "test")), Times.Once());
        }
        [Test]
        public async Task SearchMessages_CanCreatePaginationLinksAndReturnCurrentUrl_ReturnMessagesPanelView()
        {
            //arrange
            _mockRepMessages.Setup(x => x.Search(It.IsAny<string>())).Returns(MockSearch());
            _mockRepMessages.Setup(x => x.GetTotalMessages()).ReturnsAsync(8);
            _mockRepMessages.Setup(x => x.GetUnreadMessages()).ReturnsAsync(8);
            _mockServiceProvider.Setup(x => x.GetService(typeof(LinkGenerator))).Returns(_linkGenerator.Object);
            _mockServiceProvider.Setup(x => x.GetService(typeof(IHttpContextAccessor))).Returns(_mockHttpContextAccessor.Object);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_httpContextMock.Object);
            _httpContextMock.Setup(x => x.Request).Returns(_httpRequestMock.Object);
            //mock get currentURL. It uses GetEncodedUrl that needs Requests.scheme, Host etc..
            _httpRequestMock.Setup(x => x.Scheme).Returns("http");
            _httpRequestMock.Setup(x => x.Host).Returns(new HostString("localhost", 5000));

            //act
            var result = (await _controller.SearchMessages("test", 2) as ViewResult)?.ViewData.Model as MessagesPanelViewModel ?? new();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result.PaginationLinks.Count(), Is.EqualTo(2));
                Assert.That(result.CurrentUrl, Is.EqualTo("http://localhost:5000/"));
                Assert.That(result.SearchRequest, Is.True);
            });
        }
        #endregion


    }
}
