using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using scriptbuster.dev.Controllers;
using scriptbuster.dev.IdentityModels.Repository;
using scriptbuster.dev.IdentityModels.Tables;
using scriptbuster.dev.Infrastructure;
using scriptbuster.dev.Infrastructure.ApiModels;
using scriptbuster.dev.Services.AutheticationService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace scriptbuster.dev_UnitTests
{
    [TestFixture]
    internal class BlogControllerTests
    {
        private Mock<IAuthStatusService> _statusService;
        private Mock<IBlogRepository> _blogRepository;
        private Mock<ILogger<BlogController>> _logger;
        private Mock<IFormFile> _formFile;
        private BlogController _blogController;
        [SetUp]
        public void SetUp()
        {
            _statusService = new Mock<IAuthStatusService>();
            _blogRepository = new Mock<IBlogRepository>();
            _logger = new Mock<ILogger<BlogController>>();
            _formFile = new Mock<IFormFile>();
            _blogController = new BlogController(_statusService.Object, 
                                                 _blogRepository.Object,
                                                 _logger.Object);
        }
        #region AddAuthor
        [Test]
        public async Task AddAuthor_ModelStateIsNotValid_ReturnBadRequest()
        {
            //arrange 
            var author = new BlogAuthorBindingTarget();
            _blogController.ModelState.AddModelError("error", "Test Error");

            //act
            var result = await _blogController.AddAuthor(default!, author) as BadRequestObjectResult;
            var modelState = result?.Value as ModelStateDictionary ?? new();
            //assert 
            Assert.That(result!.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            _statusService.Verify(x => x.GetUserId(), Times.Never());
        }
        [Test]
        public async Task AddAuthor_UserIsNull_ReturnBadRequest()
        {
            //arrange 
            var author = new BlogAuthorBindingTarget();
            string? nullString = default;
            _statusService.Setup(x => x.GetUserId()).Returns(nullString!);

            //act
            var result = await _blogController.AddAuthor(default!, author) as UnauthorizedObjectResult;
            var model = result?.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result!.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
            Assert.That(model.Error, Is.EqualTo("UserNotAuthenticated"));
            _blogRepository.Verify(x => x.CreateAuthor(It.IsAny<BlogAuthor>()), Times.Never());
        }
        [Test]
        public async Task AddAuthor_ImageContentTypeIsNotAnImage_ReturnBadRequest()
        {
            //arrange
            var author = new BlogAuthorBindingTarget();
            _statusService.Setup(x => x.GetUserId()).Returns("userId");
            _formFile.SetupGet(x => x.ContentType).Returns("application/test");

            //act
            var result = await _blogController.AddAuthor(_formFile.Object, author) as BadRequestObjectResult;
            var model = result?.Value as ErrorResponse ?? new();

            //act
            Assert.That(result!.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(model.Error, Is.EqualTo("PNG-JPEG-Only"));
            _statusService.Verify(x => x.GetUserId(), Times.Once());
            _blogRepository.Verify(x => x.CreateAuthor(It.IsAny<BlogAuthor>()), Times.Never());   
        }
        [Test]
        public async Task AddAuthor_ImageToBig_ReturnBadRequest()
        {
            //arrange
            var author = new BlogAuthorBindingTarget();
            _statusService.Setup(x => x.GetUserId()).Returns("userId");
            _formFile.SetupGet(x => x.ContentType).Returns("image/png");
            long size = 10 * 1024 * 1024;//10mb
            _formFile.SetupGet(x => x.Length).Returns(size);

            //act
            var result = await _blogController.AddAuthor(_formFile.Object, author) as BadRequestObjectResult;
            var model = result?.Value as ErrorResponse ?? new();

            //act
            Assert.That(result!.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(model.Error, Is.EqualTo("BigSize"));
            _statusService.Verify(x => x.GetUserId(), Times.Once());
            _blogRepository.Verify(x => x.CreateAuthor(It.IsAny<BlogAuthor>()), Times.Never());
        }
        [Test]
        public async Task AddAuthor_AuthorExistCannotCreateANewOneThrowsException_ReturnBadRequest()
        {
            //arrange
            var author = new BlogAuthorBindingTarget();
            _statusService.Setup(x => x.GetUserId()).Returns("userId");
            _formFile.SetupGet(x => x.ContentType).Returns("image/jpeg");
            long size = 4 * 1024 * 1024;//4 mb pass the validation
            _formFile.SetupGet(x => x.Length).Returns(size);
            _blogRepository.Setup(x => x.CreateAuthor(It.IsAny<BlogAuthor>())).ThrowsAsync(new InvalidOperationException());

            //act
            var result = await _blogController.AddAuthor(_formFile.Object, author) as BadRequestObjectResult;
            var model = result?.Value as ErrorResponse ?? new();

            //act
            Assert.That(result!.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(model.Error, Is.EqualTo("AuthorExist"));
            _statusService.Verify(x => x.GetUserId(), Times.Once());
            _blogRepository.Verify(x => x.CreateAuthor(It.IsAny<BlogAuthor>()), Times.Once());
        }
        [Test]
        public async Task AddAuthor_AuthorCouldNotBeAddedUnknownReason_ReturnBadRequest()
        {
            //arrange
            var author = new BlogAuthorBindingTarget();
            _statusService.Setup(x => x.GetUserId()).Returns("userId");
            _formFile.SetupGet(x => x.ContentType).Returns("image/jpeg");
            long size = 4 * 1024 * 1024;//4 mb pass the validation
            _formFile.SetupGet(x => x.Length).Returns(size);
            _blogRepository.Setup(x => x.CreateAuthor(It.IsAny<BlogAuthor>())).ThrowsAsync(new Exception());

            //act
            var result = await _blogController.AddAuthor(_formFile.Object, author) as BadRequestObjectResult;
            var model = result?.Value as ErrorResponse ?? new();

            //act
            Assert.That(result!.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(model.Error, Is.EqualTo("SomethingWentWrong"));
            _statusService.Verify(x => x.GetUserId(), Times.Once());
            _blogRepository.Verify(x => x.CreateAuthor(It.IsAny<BlogAuthor>()), Times.Once());
        }
        [Test]
        public async Task AddAuthor_Works_ReturnOk()
        {
            //arrange
            var author = new BlogAuthorBindingTarget()
            {
                Proffesion = "TestProf",
                FullName = "TestName",
                Quote = "TestQuote"
            };

            _statusService.Setup(x => x.GetUserId()).Returns("userId");
            _formFile.SetupGet(x => x.ContentType).Returns("image/jpeg");
            long size = 4 * 1024 * 1024;//4 mb pass the validation
            _formFile.SetupGet(x => x.Length).Returns(size);

            //act
            var result = await _blogController.AddAuthor(_formFile.Object, author) as OkObjectResult;
            var model = result?.Value as BlogAuthorBindingTarget ?? new();

            //act
            Assert.That(result!.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
            Assert.That(model.Proffesion, Is.EqualTo("TestProf"));
            Assert.That(model.FullName, Is.EqualTo("TestName"));
            Assert.That(model.Quote, Is.EqualTo("TestQuote"));
            _statusService.Verify(x => x.GetUserId(), Times.Once());
            _blogRepository.Verify(x => x.CreateAuthor(It.IsAny<BlogAuthor>()), Times.Once());
        }
        #endregion
        #region Update Author
        [Test]
        public async Task UpdateAuthor_ModelStateIsNotValid_ReturnBadRequest()
        {
            //arrange 
            var author = new BlogAuthor();
            _blogController.ModelState.AddModelError("error", "Test Error");

            //act
            var result = await _blogController.UpdateAuthor(default!, author) as BadRequestObjectResult;
            var modelState = result?.Value as ModelStateDictionary ?? new();
            //assert 
            Assert.That(result!.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            _blogRepository.Verify(x => x.UpdateAuthor(It.IsAny<BlogAuthor>()), Times.Never());
        }
        [Test]
        public async Task UpdateAuthor_ImageContentTypeIsNotAnImage_ReturnBadRequest()
        {
            //arrange
            var author = new BlogAuthor();
            _formFile.SetupGet(x => x.ContentType).Returns("application/test");

            //act
            var result = await _blogController.UpdateAuthor(_formFile.Object, author) as BadRequestObjectResult;
            var model = result?.Value as ErrorResponse ?? new();

            //act
            Assert.That(result!.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(model.Error, Is.EqualTo("PNG-JPEG-Only"));
            _blogRepository.Verify(x => x.UpdateAuthor(It.IsAny<BlogAuthor>()), Times.Never());
        }
        [Test]
        public async Task UpdateAuthor_ImageToBig_ReturnBadRequest()
        {
            //arrange
            var author = new BlogAuthor();
            _formFile.SetupGet(x => x.ContentType).Returns("image/png");
            long size = 10 * 1024 * 1024;//10mb
            _formFile.SetupGet(x => x.Length).Returns(size);

            //act
            var result = await _blogController.UpdateAuthor(_formFile.Object, author) as BadRequestObjectResult;
            var model = result?.Value as ErrorResponse ?? new();

            //act
            Assert.That(result!.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(model.Error, Is.EqualTo("BigSize"));
            _blogRepository.Verify(x => x.UpdateAuthor(It.IsAny<BlogAuthor>()), Times.Never());
        }
        [Test]
        public async Task UpdateAuthor_AuthorCouldNotBeUpdatedUnknownReason_ReturnBadRequest()
        {
            //arrange
            var author = new BlogAuthor();
            _formFile.SetupGet(x => x.ContentType).Returns("image/jpeg");
            long size = 4 * 1024 * 1024;//4 mb pass the validation
            _formFile.SetupGet(x => x.Length).Returns(size);
            _blogRepository.Setup(x => x.UpdateAuthor(It.IsAny<BlogAuthor>())).ThrowsAsync(new Exception());

            //act
            var result = await _blogController.UpdateAuthor(_formFile.Object, author) as BadRequestObjectResult;
            var model = result?.Value as ErrorResponse ?? new();

            //act
            Assert.That(result!.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(model.Error, Is.EqualTo("SomethingWentWrong"));
            _blogRepository.Verify(x => x.UpdateAuthor(It.IsAny<BlogAuthor>()), Times.Once());
        }
        [Test]
        public async Task UpdateAuthor_Works_ReturnOk()
        {
            //arrange
            var author = new BlogAuthor()
            {
                Id=1,
                UserId = "userIdTest",
                Proffesion = "TestProf",
                FullName  = "TestFullName",
                Quote = "TestQuote"
            };
            _formFile.SetupGet(x => x.ContentType).Returns("image/jpeg");
            long size = 4 * 1024 * 1024;//4 mb pass the validation
            _formFile.SetupGet(x => x.Length).Returns(size);

            //act
            var result = await _blogController.UpdateAuthor(_formFile.Object, author) as OkObjectResult;
            var model = result?.Value as BlogAuthor ?? new();

            //act
            Assert.That(result!.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
            Assert.That(model.Id, Is.EqualTo(1));
            Assert.That(model.UserId, Is.EqualTo("userIdTest"));
            Assert.That(model.FullName, Is.EqualTo("TestFullName"));
            Assert.That(model.Proffesion, Is.EqualTo("TestProf"));
            Assert.That(model.Quote, Is.EqualTo("TestQuote"));
            _blogRepository.Verify(x => x.UpdateAuthor(It.IsAny<BlogAuthor>()), Times.Once());
        }
        #endregion
    }
}
