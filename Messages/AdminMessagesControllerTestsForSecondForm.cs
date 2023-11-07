using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using scriptbuster.dev.Controllers;
using scriptbuster.dev.Infrastructure.ViewModels.AdminMessagesController;
using scriptbuster.dev.Models.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scriptbuster.dev_UnitTests.Messages
{
    [TestFixture]
    internal class AdminMessagesControllerTestsForSecondForm
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
        public async IAsyncEnumerable<ProjectMessage> MockGetProjectMessagesByDescending()
        {
            var messagesList = new List<ProjectMessage>
            {
                new ProjectMessage
                {
                    Id = 1,
                    FullName = "Foo",
                    Email = "foo@email.com",
                    ProjectDescription = "Foo message",
                    Budget = "1 Dollar",
                    ProjectDeadLine = DateTime.Now.AddDays(1),
                    ProjectName= "FooProject",
                    PostDate = DateTime.Now,
                    Status = new MessageStatus{Id = 1, StatusName= "Unread" },
                    StatusId = 1
                },
                 new ProjectMessage
                {
                    Id = 2,
                    FullName = "Foo",
                    Email = "foo@email.com",
                    ProjectDescription = "Foo message",
                    Budget = "1 Dollar",
                    ProjectDeadLine = DateTime.Now.AddDays(1),
                    ProjectName= "FooProject",
                    PostDate = DateTime.Now,
                    Status = new MessageStatus{Id = 1, StatusName= "Unread" },
                    StatusId = 1
                },
                 new ProjectMessage
                {
                    Id = 3,
                    FullName = "Foo",
                    Email = "foo@email.com",
                    ProjectDescription = "Foo message",
                    Budget = "1 Dollar",
                    ProjectDeadLine = DateTime.Now.AddDays(1),
                    ProjectName= "FooProject",
                    PostDate = DateTime.Now,
                    Status = new MessageStatus{Id = 1, StatusName= "Unread" },
                    StatusId = 1
                },
                 new ProjectMessage
                {
                    Id = 4,
                    FullName = "Foo4",
                    Email = "foo4@email.com",
                    ProjectDescription = "Foo4 message",
                    Budget = "1 Dollar",
                    ProjectDeadLine = DateTime.Now.AddDays(1),
                    ProjectName= "FooProject",
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
        public async IAsyncEnumerable<ProjectMessage> MockSearch()
        {
            var messagesList = new List<ProjectMessage>
            {
                new ProjectMessage
                {
                    Id = 1,
                    FullName = "Foo",
                    Email = "foo@email.com",
                    ProjectDescription = "Foo message",
                    Budget = "1 Dollar",
                    ProjectDeadLine = DateTime.Now.AddDays(1),
                    ProjectName= "FooProject",
                    PostDate = DateTime.Now,
                    Status = new MessageStatus{Id = 1, StatusName= "Unread" },
                    StatusId = 1
                },
                 new ProjectMessage
                {
                    Id = 2,
                    FullName = "Foo",
                    Email = "foo@email.com",
                    ProjectDescription = "Foo message",
                    Budget = "1 Dollar",
                    ProjectDeadLine = DateTime.Now.AddDays(1),
                    ProjectName= "FooProject",
                    PostDate = DateTime.Now,
                    Status = new MessageStatus{Id = 1, StatusName= "Unread" },
                    StatusId = 1
                },
                 new ProjectMessage
                {
                    Id = 3,
                    FullName = "Foo",
                    Email = "foo@email.com",
                    ProjectDescription = "Foo message",
                    Budget = "1 Dollar",
                    ProjectDeadLine = DateTime.Now.AddDays(1),
                    ProjectName= "FooProject",
                    PostDate = DateTime.Now,
                    Status = new MessageStatus{Id = 1, StatusName= "Unread" },
                    StatusId = 1
                },
                 new ProjectMessage
                {
                    Id = 4,
                    FullName = "Foo4",
                    Email = "foo4@email.com",
                    ProjectDescription = "Foo4 message",
                    Budget = "1 Dollar",
                    ProjectDeadLine = DateTime.Now.AddDays(1),
                    ProjectName= "FooProject",
                    PostDate = DateTime.Now,
                    Status = new MessageStatus{Id = 1, StatusName= "Unread" },
                    StatusId = 1
                },
                  new ProjectMessage
                {
                    Id = 5,
                    FullName = "Foo",
                    Email = "foo@email.com",
                    ProjectDescription = "Foo message",
                    Budget = "1 Dollar",
                    ProjectDeadLine = DateTime.Now.AddDays(1),
                    ProjectName= "FooProject",
                    PostDate = DateTime.Now,
                    Status = new MessageStatus{Id = 1, StatusName= "Unread" },
                    StatusId = 1
                },
                 new ProjectMessage
                {
                    Id = 6,
                    FullName = "Foo",
                    Email = "foo@email.com",
                    ProjectDescription = "Foo message",
                    Budget = "1 Dollar",
                    ProjectDeadLine = DateTime.Now.AddDays(1),
                    ProjectName= "FooProject",
                    PostDate = DateTime.Now,
                    Status = new MessageStatus{Id = 1, StatusName= "Unread" },
                    StatusId = 1
                },
                 new ProjectMessage
                {
                    Id = 7,
                    FullName = "Foo",
                    Email = "foo@email.com",
                    ProjectDescription = "Foo message",
                    Budget = "1 Dollar",
                    ProjectDeadLine = DateTime.Now.AddDays(1),
                    ProjectName= "FooProject",
                    PostDate = DateTime.Now,
                    Status = new MessageStatus{Id = 1, StatusName= "Unread" },
                    StatusId = 1
                },
                 new ProjectMessage
                {
                    Id = 8,
                    FullName = "Foo8",
                    Email = "foo8@email.com",
                    ProjectDescription = "Foo8 message",
                    Budget = "8 Dollar",
                    ProjectDeadLine = DateTime.Now.AddDays(1),
                    ProjectName= "Foo8Project",
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

        //ProjectMessages
        #region ProjectMessagesPanel
        [Test]
        [TestCase(-1)]
        [TestCase(0)]
        public async Task ProjectMessagesPanel_MessagesPageIsLessOrEqual0_ReturnMesagesPanelAction(int page)
        {
            //act
            var result = await _controller.ProjectMessagesPanel(page) as RedirectToActionResult;

            //assert
            Assert.That(result?.ActionName, Is.EqualTo("ProjectMessagesPanel"));
            Assert.That(result?.RouteValues?["messagesPage"], Is.EqualTo(1));
            _mockProjectMessages.Verify(x => x.GetProjectMessagesByDescending(It.IsAny<int>(), It.IsAny<int>()), Times.Never());
        }
        [Test]
        public async Task ProjectMessagesPanel_CanGetMessages_ReturnView()
        {
            //arrange
            _mockProjectMessages.Setup(x => x.GetProjectMessagesByDescending(It.IsAny<int>(), It.IsAny<int>())).Returns(MockGetProjectMessagesByDescending());
            _mockServiceProvider.Setup(x => x.GetService(typeof(LinkGenerator))).Returns(_linkGenerator.Object);
            _mockServiceProvider.Setup(x => x.GetService(typeof(IHttpContextAccessor))).Returns(_mockHttpContextAccessor.Object);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_httpContextMock.Object);
            _httpContextMock.Setup(x => x.Request).Returns(_httpRequestMock.Object);
            //mock get currentURL. It uses GetEncodedUrl that needs Requests.scheme, Host etc..
            _httpRequestMock.Setup(x => x.Scheme).Returns("http");
            _httpRequestMock.Setup(x => x.Host).Returns(new HostString("localhost", 5000));


            //act
            var result = (await _controller.ProjectMessagesPanel(1) as ViewResult)?.ViewData.Model as ProjectMessagesPanelViewModel ?? new();
            var messagesList = result.Messages.ToList();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(messagesList.Count, Is.EqualTo(4));
                Assert.That(messagesList[0].Id, Is.EqualTo(4));
                Assert.That(messagesList[0].FullName, Is.EqualTo("Foo4"));
                Assert.That(messagesList[0].Email, Is.EqualTo("foo4@email.com"));
                Assert.That(messagesList[0].Budget, Is.EqualTo("1 Dollar"));
                Assert.That(messagesList[3].Id, Is.EqualTo(1));
                Assert.That(messagesList[3].FullName, Is.EqualTo("Foo"));
                Assert.That(messagesList[3].Email, Is.EqualTo("foo@email.com"));
                Assert.That(messagesList[3].Budget, Is.EqualTo("1 Dollar"));
            });
        }
        [Test]
        public async Task ProjectMessagesPanel_CanGetUnreadMessagesAndTotalMessagesCount_ReturnView()
        {
            //arrange
            _mockProjectMessages.Setup(x => x.GetProjectMessagesByDescending(It.IsAny<int>(), It.IsAny<int>())).Returns(MockGetProjectMessagesByDescending());
            _mockProjectMessages.Setup(x => x.GetTotalMessages()).ReturnsAsync(4);
            _mockProjectMessages.Setup(x => x.GetUnreadMessages()).ReturnsAsync(4);
            _mockServiceProvider.Setup(x => x.GetService(typeof(LinkGenerator))).Returns(_linkGenerator.Object);
            _mockServiceProvider.Setup(x => x.GetService(typeof(IHttpContextAccessor))).Returns(_mockHttpContextAccessor.Object);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_httpContextMock.Object);
            _httpContextMock.Setup(x => x.Request).Returns(_httpRequestMock.Object);
            //mock get currentURL. It uses GetEncodedUrl that needs Requests.scheme, Host etc..
            _httpRequestMock.Setup(x => x.Scheme).Returns("http");
            _httpRequestMock.Setup(x => x.Host).Returns(new HostString("localhost", 5000));


            //act
            var result = await _controller.ProjectMessagesPanel(1) as ViewResult;

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
            _mockProjectMessages.Setup(x => x.GetProjectMessagesByDescending(It.IsAny<int>(), It.IsAny<int>())).Returns(MockGetProjectMessagesByDescending());
            _mockProjectMessages.Setup(x => x.GetTotalMessages()).ReturnsAsync(49);//49/7(PageSize) = 7 Assert
            _mockServiceProvider.Setup(x => x.GetService(typeof(LinkGenerator))).Returns(_linkGenerator.Object);
            _mockServiceProvider.Setup(x => x.GetService(typeof(IHttpContextAccessor))).Returns(_mockHttpContextAccessor.Object);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_httpContextMock.Object);
            _httpContextMock.Setup(x => x.Request).Returns(_httpRequestMock.Object);
            //mock get currentURL. It uses GetEncodedUrl that needs Requests.scheme, Host etc..
            _httpRequestMock.Setup(x => x.Scheme).Returns("http");
            _httpRequestMock.Setup(x => x.Host).Returns(new HostString("localhost", 5000));


            //act
            var result = (await _controller.ProjectMessagesPanel(1) as ViewResult)?.ViewData.Model as ProjectMessagesPanelViewModel ?? new();
            var paginationsLinks = result.PaginationLinks;
            //assert
            Assert.Multiple(() =>
            {
                Assert.That(paginationsLinks.Count(), Is.EqualTo(7));
                Assert.That(result.CurrentUrl, Is.EqualTo("http://localhost:5000/"));
            });
        }
        #endregion
        #region Project Message
        [Test]
        [TestCase(0)]
        [TestCase(-1)]
        public async Task ProjectMessage_MessageIdIsLessThanZero_RedirectToMEssagesPanel(int messageId)
        {
            //act
            var result = await _controller.ProjectMessage(messageId) as RedirectToActionResult;

            //assert
            Assert.That(result?.ActionName, Is.EqualTo("ProjectMessagesPanel"));
            _mockProjectMessages.Verify(x => x.GetProjectMessage(It.IsAny<int>()), Times.Never());
        }
        [Test]
        public async Task Project_MessageIsNotFound_ReturnErrorInfo()
        {
            //arrange
            _mockProjectMessages.Setup(x => x.GetProjectMessage(It.IsAny<int>())).ReturnsAsync(default(ProjectMessage));

            //act
            var result = await _controller.ProjectMessage(1) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/ErrorInfo"));
            _mockProjectMessages.Verify(x => x.GetProjectMessage(It.Is<int>(x => x == 1)), Times.Once());
        }
        [Test]
        public async Task ProjectMessages_MessageIsFoundAndSetToRead_ReturnsView()
        {
            //arrange
            var message = new ProjectMessage
            {
                Id = 1,
                FullName = "Foo",
                Email = "foo@email.com",
                ProjectDescription = "Foo message",
                PostDate = DateTime.Now,
                Status = new MessageStatus { Id = 1, StatusName = "Unread" },
                StatusId = 1
            };
            _mockProjectMessages.Setup(x => x.GetProjectMessage(It.IsAny<int>())).ReturnsAsync(message);

            //act
            var result = (await _controller.ProjectMessage(1) as ViewResult)?.ViewData.Model as ProjectMessage ?? new();

            //assert
            Assert.That(result.Id, Is.EqualTo(1));
            Assert.That(result.Email, Is.EqualTo("foo@email.com"));
            Assert.That(result.ProjectDescription, Is.EqualTo("Foo message"));
            _mockProjectMessages.Verify(x => x.GetProjectMessage(It.Is<int>(x => x == 1)), Times.Once());
            _mockProjectMessages.Verify(x => x.SetProjectMessageToRead(It.Is<ProjectMessage>(x => x == message)));
        }

        #endregion
        #region DeleteProject Messages
        [Test]
        public async Task DeleteProjectMessage_MessageIsNotFound_ReturnErrorInfo()
        {
            //arrange
            _mockProjectMessages.Setup(x => x.DeleteMessage(It.IsAny<int>())).ReturnsAsync(false);

            //act
            var result = await _controller.DeleteProjectMessage(1) as RedirectToPageResult;

            //assert
            Assert.That(result?.PageName, Is.EqualTo("/ErrorInfo"));
            _mockProjectMessages.Verify(x => x.DeleteMessage(It.Is<int>(x => x == 1)), Times.Once());
        }
        [Test]
        public async Task DeleteMessage_MessageIsFoundMessageIsDeleted_ReturnMessagesPanel()
        {
            //arrange
            _mockProjectMessages.Setup(x => x.DeleteMessage(It.IsAny<int>())).ReturnsAsync(true);

            //act
            var result = await _controller.DeleteProjectMessage(1) as RedirectToActionResult;

            //assert
            Assert.That(result?.ActionName, Is.EqualTo("ProjectMessagesPanel"));
            _mockProjectMessages.Verify(x => x.DeleteMessage(It.Is<int>(x => x == 1)), Times.Once());
        }
        #endregion
        #region Search
        [Test]
        [TestCase("Test", -1)]
        [TestCase("Test", 0)]
        [TestCase("", 1)]
        [TestCase(default, 1)]
        public async Task SearchProjectMessages_MessagesIsLessOrEqualZeroOrSearchCriteriaIsNullOrEmpty_ReturnMessgesPanel(string searchCriteria, int messagesPage)
        {
            //act
            var result = await _controller.SearchProjectMessages(searchCriteria, messagesPage) as RedirectToActionResult;

            //assert
            Assert.That(result?.ActionName, Is.EqualTo("ProjectMessagesPanel"));
            _mockProjectMessages.Verify(x => x.Search(It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task SearchProjectMessages_CanCountUnreadAndTotalMessages_ReturnMessagesPanelView()
        {
            //arrange
            _mockProjectMessages.Setup(x => x.Search(It.IsAny<string>())).Returns(MockSearch());
            _mockProjectMessages.Setup(x => x.GetTotalMessages()).ReturnsAsync(8);
            _mockProjectMessages.Setup(x => x.GetUnreadMessages()).ReturnsAsync(8);
            _mockServiceProvider.Setup(x => x.GetService(typeof(LinkGenerator))).Returns(_linkGenerator.Object);
            _mockServiceProvider.Setup(x => x.GetService(typeof(IHttpContextAccessor))).Returns(_mockHttpContextAccessor.Object);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_httpContextMock.Object);
            _httpContextMock.Setup(x => x.Request).Returns(_httpRequestMock.Object);
            //mock get currentURL. It uses GetEncodedUrl that needs Requests.scheme, Host etc..
            _httpRequestMock.Setup(x => x.Scheme).Returns("http");
            _httpRequestMock.Setup(x => x.Host).Returns(new HostString("localhost", 5000));

            //act
            var result = await _controller.SearchProjectMessages("test") as ViewResult;

            //assert
            Assert.Multiple(() =>
            {
                Assert.That((int)result?.ViewData["UnreadMessages"]!, Is.EqualTo(8));
                Assert.That((int)result?.ViewData["TotalMessages"]!, Is.EqualTo(8));
                Assert.That(_controller.ViewBag.SearchCriteria, Is.EqualTo("test"));
            });
        }
        [Test]
        public async Task SearchProjectMessages_CanPaginatePage2_ReturnMessagesPanelView()
        {
            //arrange
            _mockProjectMessages.Setup(x => x.Search(It.IsAny<string>())).Returns(MockSearch());
            _mockProjectMessages.Setup(x => x.GetTotalMessages()).ReturnsAsync(8);
            _mockProjectMessages.Setup(x => x.GetUnreadMessages()).ReturnsAsync(8);
            _mockServiceProvider.Setup(x => x.GetService(typeof(LinkGenerator))).Returns(_linkGenerator.Object);
            _mockServiceProvider.Setup(x => x.GetService(typeof(IHttpContextAccessor))).Returns(_mockHttpContextAccessor.Object);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_httpContextMock.Object);
            _httpContextMock.Setup(x => x.Request).Returns(_httpRequestMock.Object);
            //mock get currentURL. It uses GetEncodedUrl that needs Requests.scheme, Host etc..
            _httpRequestMock.Setup(x => x.Scheme).Returns("http");
            _httpRequestMock.Setup(x => x.Host).Returns(new HostString("localhost", 5000));

            //act
            var result = (await _controller.SearchProjectMessages("test", 2) as ViewResult)?.ViewData.Model as ProjectMessagesPanelViewModel ?? new();
            var listMessages = result.Messages.ToList();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(listMessages.Count, Is.EqualTo(1));
                Assert.That(listMessages[0].Id, Is.EqualTo(1));
                Assert.That(listMessages[0].FullName, Is.EqualTo("Foo"));
                Assert.That(listMessages[0].Email, Is.EqualTo("foo@email.com"));
                Assert.That(listMessages[0].ProjectDescription, Is.EqualTo("Foo message"));
                Assert.That(listMessages[0].Budget, Is.EqualTo("1 Dollar"));
            });
            _mockProjectMessages.Verify(x => x.Search(It.Is<string>(x => x == "test")), Times.Once());
        }
        [Test]
        public async Task SearchProjectMessages_CanCreatePaginationLinksAndReturnCurrentUrl_ReturnMessagesPanelView()
        {
            //arrange
            _mockProjectMessages.Setup(x => x.Search(It.IsAny<string>())).Returns(MockSearch());
            _mockProjectMessages.Setup(x => x.GetTotalMessages()).ReturnsAsync(8);
            _mockProjectMessages.Setup(x => x.GetUnreadMessages()).ReturnsAsync(8);
            _mockServiceProvider.Setup(x => x.GetService(typeof(LinkGenerator))).Returns(_linkGenerator.Object);
            _mockServiceProvider.Setup(x => x.GetService(typeof(IHttpContextAccessor))).Returns(_mockHttpContextAccessor.Object);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_httpContextMock.Object);
            _httpContextMock.Setup(x => x.Request).Returns(_httpRequestMock.Object);
            //mock get currentURL. It uses GetEncodedUrl that needs Requests.scheme, Host etc..
            _httpRequestMock.Setup(x => x.Scheme).Returns("http");
            _httpRequestMock.Setup(x => x.Host).Returns(new HostString("localhost", 5000));

            //act
            var result = (await _controller.SearchProjectMessages("test", 2) as ViewResult)?.ViewData.Model as ProjectMessagesPanelViewModel ?? new();

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
