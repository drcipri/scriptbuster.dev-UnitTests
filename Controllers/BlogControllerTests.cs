using HtmlAgilityPack;
using Microsoft.AspNetCore.Hosting;
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
using scriptbuster.dev.Services.HtmlService;
using scriptbuster.dev.Services.FileSystemService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Microsoft.AspNetCore.Mvc.Routing;
using scriptbuster.dev.Services.SessionService;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;
using scriptbuster.dev.Infrastructure.ViewModels.BlogController;

namespace scriptbuster.dev_UnitTests.Controllers
{
    [TestFixture]
    internal class BlogControllerTests
    {
        private Mock<IAuthStatusService> _statusService;
        private Mock<IBlogRepository> _blogRepository;
        private Mock<ILogger<BlogController>> _logger;
        private Mock<IWebHostEnvironment> _webHost;
        private Mock<IFileSystemService> _fileSystemService;
        private Mock<IHtmlDocumentService> _htmlDocumentService;
        private Mock<IDirectoryInfoWrapper> _directoryInfoWrapper;
        private BlogController _blogController;

        //likeArticle
        private Mock<ISessionService> _sessionMock;
        private Mock<IEmailSender> _emailSenderMock;
        private Mock<IConfiguration> _configurationMock;

        //blog panel
        private Mock<IHttpContextAccessor> _httpContextAccesor;

        private IFormFile[]? _formFileArray;
        private Mock<IFormFile> _formFile;


        [SetUp]
        public void SetUp()
        {
            _statusService = new Mock<IAuthStatusService>();
            _blogRepository = new Mock<IBlogRepository>();
            _logger = new Mock<ILogger<BlogController>>();
            _formFile = new Mock<IFormFile>();
            _webHost = new Mock<IWebHostEnvironment>();
            _fileSystemService = new Mock<IFileSystemService>();
            _htmlDocumentService = new Mock<IHtmlDocumentService>();
            _directoryInfoWrapper = new Mock<IDirectoryInfoWrapper>();

            _sessionMock = new Mock<ISessionService>();
            _emailSenderMock = new Mock<IEmailSender>();
            _configurationMock = new Mock<IConfiguration>();
            _httpContextAccesor = new Mock<IHttpContextAccessor>();

            _blogController = new BlogController(_statusService.Object,
                                                 _blogRepository.Object,
                                                 _logger.Object,
                                                 _webHost.Object,
                                                 _htmlDocumentService.Object,
                                                 _fileSystemService.Object,
                                                 _directoryInfoWrapper.Object);
        }
        public async IAsyncEnumerable<Tag> MockGetAllTags()
        {
            var tags = new List<Tag>
            {
                new Tag { Id = 1, Name= "Test" },
                new Tag { Id = 2, Name = "Test2"},
                new Tag { Id = 3, Name = "Test3"}
            };

            foreach(var tag in tags)
            {
                yield return tag;
            }
            await Task.CompletedTask;
        }
        public async IAsyncEnumerable<BlogArticle> MockGetUserBlogArticlesByDescending()
        {
            var list = new List<BlogArticle>
            {
                new BlogArticle
                {
                    Id = 1,
                    Title = "TestArticle1",
                },
                new BlogArticle
                {
                    Id = 2,
                    Title = "TestArticle2"
                },
                new BlogArticle
                {
                    Id = 3,
                    Title = "TestArticle3"
                },
                new BlogArticle
                {
                    Id = 4,
                    Title = "TestArticle4"
                }
            };
            foreach (var article in list)
            {
                yield return article;
            }
            await Task.CompletedTask;
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
                Id = 1,
                UserId = "userIdTest",
                Proffesion = "TestProf",
                FullName = "TestFullName",
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
        #region PostArticle
        [Test]
        public async Task PostArticle_ModelStateIsNotValid_ReturnBadRequest()
        {
            //arrange
            _blogController.ModelState.AddModelError("error", "An Error occured");
            var model = new BlogArticleBindingTarget();

            //act
            var result = await _blogController.PostArticle(default!, model, default!, Enumerable.Empty<int>()) as BadRequestObjectResult;

            //assert
            Assert.That(result!.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            _statusService.Verify(x => x.GetUserId(), Times.Never());
        }
        [Test]
        public async Task PostArticle_AuthorIsNull_ReturnBadRequest()
        {
            //arrange
            //by default mock will return null on GetUserId and GetAuthor
            var article = new BlogArticleBindingTarget();

            //act
            var result = await _blogController.PostArticle(default!, article, default!, Enumerable.Empty<int>()) as BadRequestObjectResult;
            var model = result!.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(model.Error, Is.EqualTo("AuthorNotFound"));
            _statusService.Verify(x => x.GetUserId(), Times.Once());
            _blogRepository.Verify(x => x.GetUserAuthor(It.IsAny<string>()), Times.Once());
        }
        [Test]
        public async Task PostArticle_TitlePictureContentTypeIsNotJpegOrPng_ReturnBadRequest()
        {
            //arrange
            var article = new BlogArticleBindingTarget();
            _statusService.Setup(x => x.GetUserId()).Returns("user");
            _blogRepository.Setup(x => x.GetUserAuthor("user")).ReturnsAsync(new BlogAuthor());

            _formFile.SetupGet(x => x.ContentType).Returns("application/test");

            //act
            var result = await _blogController.PostArticle(_formFile.Object, article, default!, Enumerable.Empty<int>()) as BadRequestObjectResult;
            var model = result!.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(model.Error, Is.EqualTo("PNG-JPEG-Only"));
            _statusService.Verify(x => x.GetUserId(), Times.Once());
            _blogRepository.Verify(x => x.GetUserAuthor(It.IsAny<string>()), Times.Once());
            _blogRepository.Verify(x => x.AddArticle(It.IsAny<BlogArticle>()), Times.Never());
        }
        [Test]
        public async Task PostArticle_TitlePictureSizeIsBiggerTHanSizeLimit_ReturnBadRequest()
        {
            //arrange
            var article = new BlogArticleBindingTarget();
            _statusService.Setup(x => x.GetUserId()).Returns("user");
            _blogRepository.Setup(x => x.GetUserAuthor("user")).ReturnsAsync(new BlogAuthor());

            _formFile.SetupGet(x => x.ContentType).Returns("image/png");
            int size = 10 * 1024 * 1024; //10mb
            _formFile.SetupGet(x => x.Length).Returns(size);

            //act
            var result = await _blogController.PostArticle(_formFile.Object, article, default!, Enumerable.Empty<int>()) as BadRequestObjectResult;
            var model = result!.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(model.Error, Is.EqualTo("BigSize"));
            _statusService.Verify(x => x.GetUserId(), Times.Once());
            _blogRepository.Verify(x => x.GetUserAuthor(It.IsAny<string>()), Times.Once());
            _blogRepository.Verify(x => x.AddArticle(It.IsAny<BlogArticle>()), Times.Never());
        }
        [Test]
        public async Task PostArticle_BlogPicturesContetTypeIsNotJpegOrPNG_ReturnBadRequest()
        {
            //arrange
            _statusService.Setup(x => x.GetUserId()).Returns("user");
            _blogRepository.Setup(x => x.GetUserAuthor("user")).ReturnsAsync(new BlogAuthor());//pass authorValidation

            var article = new BlogArticleBindingTarget();
            _formFile.SetupGet(x => x.ContentType).Returns("application/test");
            var mockSecondFormFile = new Mock<IFormFile>();
            mockSecondFormFile.SetupGet(x => x.ContentType).Returns("image/jpeg");

            _formFileArray = new IFormFile[] { mockSecondFormFile.Object, _formFile.Object };

            //act
            var result = await _blogController.PostArticle(default!, article, _formFileArray!, Enumerable.Empty<int>()) as BadRequestObjectResult;
            var model = result!.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(model.Error, Is.EqualTo("PNG-JPEG-Only"));
            _statusService.Verify(x => x.GetUserId(), Times.Once());
            _blogRepository.Verify(x => x.GetUserAuthor(It.IsAny<string>()), Times.Once());
            _blogRepository.Verify(x => x.AddArticle(It.IsAny<BlogArticle>()), Times.Never());
        }
        [Test]
        public async Task PostArticle_BlogPicturesSizeIsBiggerThanSizeLimit_ReturnBadRequest()
        {
            //arrange
            _statusService.Setup(x => x.GetUserId()).Returns("user");
            _blogRepository.Setup(x => x.GetUserAuthor("user")).ReturnsAsync(new BlogAuthor());//pass authorValidation
            int size = 10 * 1024 * 1024; //10mb
            var article = new BlogArticleBindingTarget();
            _formFile.SetupGet(x => x.ContentType).Returns("image/png");
            var mockSecondFormFile = new Mock<IFormFile>();
            mockSecondFormFile.SetupGet(x => x.ContentType).Returns("image/jpeg");
            _formFile.SetupGet(x => x.Length).Returns(size);

            _formFileArray = new IFormFile[] { mockSecondFormFile.Object, _formFile.Object };

            //act
            var result = await _blogController.PostArticle(default!, article, _formFileArray!, Enumerable.Empty<int>()) as BadRequestObjectResult;
            var model = result!.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(model.Error, Is.EqualTo("BigSize"));
            _statusService.Verify(x => x.GetUserId(), Times.Once());
            _blogRepository.Verify(x => x.GetUserAuthor(It.IsAny<string>()), Times.Once());
            _blogRepository.Verify(x => x.AddArticle(It.IsAny<BlogArticle>()), Times.Never());
        }

        //tags validation TESTS HERE
        [Test]
        public async Task PostArticle_TagsAreEmpty_ReturnBadRequest()
        {
            _statusService.Setup(x => x.GetUserId()).Returns("user");
            _blogRepository.Setup(x => x.GetUserAuthor("user")).ReturnsAsync(new BlogAuthor());//pass authorValidation

            var article = new BlogArticleBindingTarget()
            {
                Title = "TestTitle",
                HtmlContent = "HtmlContentTest"
            };

            var emptyTagCollection = new List<int>();

            //act
            var result = await _blogController.PostArticle(default!, article, default!, emptyTagCollection) as BadRequestObjectResult;
            var model = result!.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(model.Error, Is.EqualTo("TagRequired"));

            _blogRepository.Verify(x => x.TagExist(It.IsAny<int>()), Times.Never());
            _htmlDocumentService.Verify(x => x.LoadHtml(It.IsAny<string>()), Times.Never());
            _blogRepository.Verify(x => x.AddArticle(It.IsAny<BlogArticle>()), Times.Never());
        }
        [Test]
        public async Task PostArticle_TagsDoesNotExistsInTheDatabase_ReturnBadRequest()
        //unlikely to happen , only if the ids were altered on the client side
        {
            _statusService.Setup(x => x.GetUserId()).Returns("user");
            _blogRepository.Setup(x => x.GetUserAuthor("user")).ReturnsAsync(new BlogAuthor());//pass authorValidation

            var article = new BlogArticleBindingTarget()
            {
                Title = "TestTitle",
                HtmlContent = "HtmlContentTest"
            };

            var tagsCollection = new List<int> { 1, 2, 3 };

            //act
            var result = await _blogController.PostArticle(default!, article, default!, tagsCollection) as BadRequestObjectResult;
            var model = result!.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(model.Error, Is.EqualTo("InvalidTags"));

            _blogRepository.Verify(x => x.TagExist(It.IsAny<int>()), Times.AtLeast(3));//number of tags collection
            _htmlDocumentService.Verify(x => x.LoadHtml(It.IsAny<string>()), Times.Never());
            _blogRepository.Verify(x => x.AddArticle(It.IsAny<BlogArticle>()), Times.Never());
        }
        [Test]
        public async Task PostArticle_NumberOfImagesAndNumberOfImgNodesDontMatch_ImagesAreNotSavedOnTheServer_ReturnOkResult()
        //in theory this should never happen in a real environment Only if HtmlDoc Was altered on ClientSide
        {
            //arrange
            _statusService.Setup(x => x.GetUserId()).Returns("user");
            _blogRepository.Setup(x => x.GetUserAuthor("user")).ReturnsAsync(new BlogAuthor());//pass authorValidation

            //pass tags validation
            var mockTags = new List<int> { 1 };
            _blogRepository.Setup(x => x.TagExist(It.IsAny<int>())).ReturnsAsync(true);

            var article = new BlogArticleBindingTarget();

            _formFile.SetupGet(x => x.ContentType).Returns("image/png"); //pass blogPictures Validation
            var mockSecondFormFile = new Mock<IFormFile>();
            mockSecondFormFile.SetupGet(x => x.ContentType).Returns("image/jpeg");

            _formFileArray = new IFormFile[] { mockSecondFormFile.Object, _formFile.Object };

            //html
            var htmlDoc = new HtmlDocument();
            var imgNodes = new HtmlNodeCollection(htmlDoc.DocumentNode);//empty collection
            _htmlDocumentService.Setup(x => x.SelectNodes(It.IsAny<string>())).Returns(imgNodes);

            //act
            var result = await _blogController.PostArticle(default!, article, _formFileArray!, mockTags) as OkObjectResult;
            var model = result!.Value as SuccesResponse ?? new();

            //assert
            Assert.That(result.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
            StringAssert.Contains("succes", model.Message);

            _blogRepository.Verify(x => x.AddArticle(It.IsAny<BlogArticle>()), Times.Once());
            _htmlDocumentService.Verify(x => x.SelectNodes(It.IsAny<string>()), Times.Once());

            _fileSystemService.Verify(x => x.WriteAllBytes(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never());
            _directoryInfoWrapper.Verify(x => x.SetDirectory(It.IsAny<string>()), Times.Never());
            _directoryInfoWrapper.Verify(x => x.CreateSubdirectory(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public async Task PostArticle_WriteAllBytesThrowsException_ReturnInternalServerError()
        {
            //arrange
            _statusService.Setup(x => x.GetUserId()).Returns("user");
            _blogRepository.Setup(x => x.GetUserAuthor("user")).ReturnsAsync(new BlogAuthor());//pass authorValidation

            var article = new BlogArticleBindingTarget()
            {
                Title = "TestTitle",
                HtmlContent = "HtmlContentTest"
            };
            //pass tags validation
            var mockTags = new List<int> { 1 };
            _blogRepository.Setup(x => x.TagExist(It.IsAny<int>())).ReturnsAsync(true);

            //pass title validation
            _formFile.SetupGet(x => x.ContentType).Returns("image/png");
            var mockSecondFormFile = new Mock<IFormFile>();
            mockSecondFormFile.SetupGet(x => x.ContentType).Returns("image/jpeg");
            _formFileArray = new IFormFile[] { mockSecondFormFile.Object, _formFile.Object };

            //web rooth path for Path.Combine
            _webHost.SetupGet(x => x.WebRootPath).Returns("/testPath");

            //arrange html doc
            var htmlDoc = new HtmlDocument();
            var imgNodes = new HtmlNodeCollection(htmlDoc.DocumentNode)
            {
                HtmlNode.CreateNode("<img src='image1.jpg'>"),
                HtmlNode.CreateNode("<img src='image2.jpg'>")
            };
            _htmlDocumentService.Setup(x => x.SelectNodes(It.IsAny<string>())).Returns(imgNodes); //returns count of two like in the form file array.
                                                                                                  //pass nodes check validation

            _fileSystemService.Setup(x => x.WriteAllBytes(It.IsAny<string>(), It.IsAny<byte[]>(), default)).ThrowsAsync(new Exception("Test Error"));

            //directory info return a new IDirectoryInfoWrapper
            var subdirectoryMock = new Mock<IDirectoryInfoWrapper>();
            subdirectoryMock.SetupGet(x => x.Name).Returns("testName");
            _directoryInfoWrapper.Setup(x => x.CreateSubdirectory(It.IsAny<string>())).Returns(subdirectoryMock.Object);

            //act
            var result = await _blogController.PostArticle(default!, article, _formFileArray!, mockTags) as ObjectResult;
            var model = result!.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result.StatusCode, Is.EqualTo((int)HttpStatusCode.InternalServerError));
            Assert.That(model.Error, Is.EqualTo("ServerError"));

            _blogRepository.Verify(x => x.AddArticle(It.IsAny<BlogArticle>()), Times.Never());
            _htmlDocumentService.Verify(x => x.OuterHtml(), Times.Never());

            _htmlDocumentService.Verify(x => x.SelectNodes(It.IsAny<string>()), Times.Once());
            _fileSystemService.Verify(x => x.WriteAllBytes(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once());
            _directoryInfoWrapper.Verify(x => x.SetDirectory(It.IsAny<string>()), Times.Once());
            _directoryInfoWrapper.Verify(x => x.CreateSubdirectory(It.IsAny<string>()), Times.Once());

            _fileSystemService.Verify(x => x.DeleteFile(It.IsAny<string>()), Times.Once());
            subdirectoryMock.Verify(x => x.Delete(), Times.Once());
        }

        [Test]
        public async Task PostArticle_WriteAllBytesThrowsExceptionAndRollBackDeleteingCoruptedFilesAndTheCreatedFolderThrowsException_ReturnInternalServerError()
        {
            //arrange
            _statusService.Setup(x => x.GetUserId()).Returns("user");
            _blogRepository.Setup(x => x.GetUserAuthor("user")).ReturnsAsync(new BlogAuthor());//pass authorValidation

            var article = new BlogArticleBindingTarget()
            {
                Title = "TestTitle",
                HtmlContent = "HtmlContentTest"
            };

            //pass tags validation
            var mockTags = new List<int> { 1 };
            _blogRepository.Setup(x => x.TagExist(It.IsAny<int>())).ReturnsAsync(true);

            //pass title validation
            _formFile.SetupGet(x => x.ContentType).Returns("image/png");
            var mockSecondFormFile = new Mock<IFormFile>();
            mockSecondFormFile.SetupGet(x => x.ContentType).Returns("image/jpeg");
            _formFileArray = new IFormFile[] { mockSecondFormFile.Object, _formFile.Object };

            //web rooth path for Path.Combine
            _webHost.SetupGet(x => x.WebRootPath).Returns("/testPath");

            //arrange html doc
            var htmlDoc = new HtmlDocument();
            var imgNodes = new HtmlNodeCollection(htmlDoc.DocumentNode)
            {
                HtmlNode.CreateNode("<img src='image1.jpg'>"),
                HtmlNode.CreateNode("<img src='image2.jpg'>")
            };
            _htmlDocumentService.Setup(x => x.SelectNodes(It.IsAny<string>())).Returns(imgNodes); //returns count of two like in the form file array.
                                                                                                  //pass nodes check validation

            _fileSystemService.Setup(x => x.WriteAllBytes(It.IsAny<string>(), It.IsAny<byte[]>(), default)).ThrowsAsync(new Exception("Test Error"));

            //directory info return a new IDirectoryInfoWrapper
            var subdirectoryMock = new Mock<IDirectoryInfoWrapper>();
            subdirectoryMock.SetupGet(x => x.Name).Returns("testName");
            _directoryInfoWrapper.Setup(x => x.CreateSubdirectory(It.IsAny<string>())).Returns(subdirectoryMock.Object);
            subdirectoryMock.Setup(x => x.Delete()).Throws(new Exception("error"));//throws another exception 


            //act
            var result = await _blogController.PostArticle(default!, article, _formFileArray!, mockTags) as ObjectResult;
            var model = result!.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result.StatusCode, Is.EqualTo((int)HttpStatusCode.InternalServerError));
            Assert.That(model.Error, Is.EqualTo("ServerError"));

            _blogRepository.Verify(x => x.AddArticle(It.IsAny<BlogArticle>()), Times.Never());
            _htmlDocumentService.Verify(x => x.OuterHtml(), Times.Never());

            _htmlDocumentService.Verify(x => x.SelectNodes(It.IsAny<string>()), Times.Once());
            _fileSystemService.Verify(x => x.WriteAllBytes(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once());
            _directoryInfoWrapper.Verify(x => x.SetDirectory(It.IsAny<string>()), Times.Once());
            _directoryInfoWrapper.Verify(x => x.CreateSubdirectory(It.IsAny<string>()), Times.Once());

            _fileSystemService.Verify(x => x.DeleteFile(It.IsAny<string>()), Times.Once());
        }

        [Test]
        public async Task PostArticle_ImagesAreSavedToTheServer_ReturnOkResult()
        {
            //arrange
            _statusService.Setup(x => x.GetUserId()).Returns("user");
            _blogRepository.Setup(x => x.GetUserAuthor("user")).ReturnsAsync(new BlogAuthor());//pass authorValidation

            var article = new BlogArticleBindingTarget()
            {
                Title = "TestTitle",
                HtmlContent = "HtmlContentTest"
            };

            //pass tags validation
            var mockTags = new List<int> { 1 };
            _blogRepository.Setup(x => x.TagExist(It.IsAny<int>())).ReturnsAsync(true);

            //pass title validation
            _formFile.SetupGet(x => x.ContentType).Returns("image/png");
            var mockSecondFormFile = new Mock<IFormFile>();
            mockSecondFormFile.SetupGet(x => x.ContentType).Returns("image/jpeg");
            _formFileArray = new IFormFile[] { mockSecondFormFile.Object, _formFile.Object };

            //web rooth path for Path.Combine
            _webHost.SetupGet(x => x.WebRootPath).Returns("/testPath");

            //arrange html doc
            var htmlDoc = new HtmlDocument();
            var imgNodes = new HtmlNodeCollection(htmlDoc.DocumentNode)
            {
                HtmlNode.CreateNode("<img src='image1.jpg'>"),
                HtmlNode.CreateNode("<img src='image2.jpg'>")
            };
            _htmlDocumentService.Setup(x => x.SelectNodes(It.IsAny<string>())).Returns(imgNodes); //returns count of two like in the form file array.
                                                                                                  //pass nodes check validation

            //directory info return a new IDirectoryInfoWrapper
            var subdirectoryMock = new Mock<IDirectoryInfoWrapper>();
            subdirectoryMock.SetupGet(x => x.Name).Returns("testName");
            _directoryInfoWrapper.Setup(x => x.CreateSubdirectory(It.IsAny<string>())).Returns(subdirectoryMock.Object);

            //act
            var result = await _blogController.PostArticle(default!, article, _formFileArray!, mockTags) as OkObjectResult;
            var model = result!.Value as SuccesResponse ?? new();

            //assert
            Assert.That(result.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
            StringAssert.Contains("succes", model.Message);

            _blogRepository.Verify(x => x.AddArticle(It.IsAny<BlogArticle>()), Times.Once());
            _htmlDocumentService.Verify(x => x.OuterHtml(), Times.Once());

            _htmlDocumentService.Verify(x => x.SelectNodes(It.IsAny<string>()), Times.Once());
            _fileSystemService.Verify(x => x.WriteAllBytes(It.IsAny<string>(), It.IsAny<byte[]>(), default), Times.AtLeast(1));
            _directoryInfoWrapper.Verify(x => x.SetDirectory(It.IsAny<string>()), Times.Once());
            _directoryInfoWrapper.Verify(x => x.CreateSubdirectory(It.IsAny<string>()), Times.Once());

            _fileSystemService.Verify(x => x.DeleteFile(It.IsAny<string>()), Times.Never());
            subdirectoryMock.Verify(x => x.Delete(), Times.Never());
        }

        [Test]
        public async Task PostArticle_AddArticleThrowsInvalidOperationException_ReturnBadRequest()
        {
            //arrange
            _statusService.Setup(x => x.GetUserId()).Returns("user");
            _blogRepository.Setup(x => x.GetUserAuthor("user")).ReturnsAsync(new BlogAuthor());//pass authorValidation

            //pass tags validation
            var mockTags = new List<int> { 1 };
            _blogRepository.Setup(x => x.TagExist(It.IsAny<int>())).ReturnsAsync(true);

            var article = new BlogArticleBindingTarget()
            {
                Title = "TestTitle",
                HtmlContent = "HtmlContentTest"
            };

            _blogRepository.Setup(x => x.AddArticle(It.IsAny<BlogArticle>())).ThrowsAsync(new InvalidOperationException("error"));

            //act
            var result = await _blogController.PostArticle(default!, article, default!, mockTags) as BadRequestObjectResult;
            var model = result!.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(model.Error, Is.EqualTo("NoAuthor"));

            _blogRepository.Verify(x => x.AddArticle(It.IsAny<BlogArticle>()), Times.Once());

            _fileSystemService.Verify(x => x.DeleteFile(It.IsAny<string>()), Times.Never());
            _htmlDocumentService.Verify(x => x.OuterHtml(), Times.Never());
            _directoryInfoWrapper.Verify(x => x.SetDirectory(It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task PostArticle_AddArticleThrowsExceptionForUnknownReason_ReturnBadRequest()
        {
            //arrange
            _statusService.Setup(x => x.GetUserId()).Returns("user");
            _blogRepository.Setup(x => x.GetUserAuthor("user")).ReturnsAsync(new BlogAuthor());//pass authorValidation

            //pass tags validation
            var mockTags = new List<int> { 1 };
            _blogRepository.Setup(x => x.TagExist(It.IsAny<int>())).ReturnsAsync(true);

            var article = new BlogArticleBindingTarget()
            {
                Title = "TestTitle",
                HtmlContent = "HtmlContentTest"
            };

            _blogRepository.Setup(x => x.AddArticle(It.IsAny<BlogArticle>())).ThrowsAsync(new Exception("error"));

            //act
            var result = await _blogController.PostArticle(default!, article, default!, mockTags) as BadRequestObjectResult;
            var model = result!.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(model.Error, Is.EqualTo("SomethingWentWrong"));

            _blogRepository.Verify(x => x.AddArticle(It.IsAny<BlogArticle>()), Times.Once());

            _fileSystemService.Verify(x => x.DeleteFile(It.IsAny<string>()), Times.Never());
            _htmlDocumentService.Verify(x => x.OuterHtml(), Times.Never());
            _directoryInfoWrapper.Verify(x => x.SetDirectory(It.IsAny<string>()), Times.Never());
        }
        #endregion
        #region AddArticle and ReadArticle
        [Test]
        public void AddArticle_CanGenerateArticleLinkForAjax_ReturnView()
        {
            //arrange
            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper.Setup(x => x.Action(It.IsAny<UrlActionContext>())).Returns("/test/post-article");
            _blogController.Url = mockUrlHelper.Object;

            //act
            var result = (_blogController.AddArticle() as ViewResult)?.ViewData.Model as string;

            //Assert
            Assert.That(result, Is.EqualTo("/test/post-article"));
        }
        [Test]
        [TestCase(0)]
        [TestCase(-1)]
        public async Task ReadArticle_ArticleIsLessOrEqualZero_ReturnClientInfo(int articleId)
        {
            //act
            var result = await _blogController.ReadArticle(articleId) as RedirectToPageResult;

            //arrange
            Assert.That(result?.PageName, Is.EqualTo("/ClientInfo"));

            _blogRepository.Verify(x => x.GetBlogArticleWithAuthorAndTags(It.IsAny<int>()), Times.Never());
        }
        [Test]
        public async Task ReadArticle_ArticleIsNull_ReturnClientInfo()
        {
            //arrange
            BlogArticle nullArticle = default!;
            _blogRepository.Setup(x => x.GetBlogArticleWithAuthorAndTags(1)).ReturnsAsync(nullArticle!);

            var mockUrlHelper = new Mock<IUrlHelper>();
            _blogController.Url = mockUrlHelper.Object;

            //act
            var result = await _blogController.ReadArticle(1) as RedirectToPageResult;

            //arrange
            Assert.That(result?.PageName, Is.EqualTo("/ClientInfo"));

            _blogRepository.Verify(x => x.GetBlogArticleWithAuthorAndTags(It.IsAny<int>()), Times.Once());
            mockUrlHelper.Verify(x => x.Action(It.IsAny<UrlActionContext>()), Times.Never());
        }
        [Test]
        public async Task ReadArticle_CanCreateTheLinkForLikeEndpointForTheAjaxAndReturnTheTheView_ReturnView()
        {
            BlogArticle article = new BlogArticle();
            _blogRepository.Setup(x => x.GetBlogArticleWithAuthorAndTags(1)).ReturnsAsync(article);

            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper.Setup(x => x.Action(It.IsAny<UrlActionContext>())).Returns("test/like-link");
            _blogController.Url = mockUrlHelper.Object;

            //act
            var result = await _blogController.ReadArticle(1) as ViewResult;
            var model = result?.ViewData.Model as BlogArticle ?? new();
            //arrange
            Assert.That(model, Is.Not.Null);
            Assert.That(result!.ViewData["UrlLikeLink"], Is.EqualTo("test/like-link"));

            _blogRepository.Verify(x => x.GetBlogArticleWithAuthorAndTags(It.IsAny<int>()), Times.Once());
            mockUrlHelper.Verify(x => x.Action(It.IsAny<UrlActionContext>()), Times.Once());
        }

        #endregion
        #region Like Article
        [Test]
        [TestCase(0)]
        [TestCase(-1)]
        public async Task LikeArticle_ArticleIdIsZeroOrLess_ReturnBadRequest(int articleId)
        {
            //act
            var result = await _blogController.LikeArticle(articleId,
                                                _sessionMock.Object,
                                                _emailSenderMock.Object,
                                                _configurationMock.Object) as BadRequestObjectResult;
            var model = result!.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(model.Error, Is.EqualTo("ArticleNotFound"));

            _blogRepository.Verify(x => x.LikeArticle(It.IsAny<int>()), Times.Never());
            _sessionMock.Verify(x => x.GetString(It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task LikeArticle_ArticleWasAlreadyLiked_ReturnBadRequest()
        {
            //arrange
            string testString = "SessionTest";
            _sessionMock.Setup(x => x.GetString(It.IsAny<string>())).ReturnsAsync(testString);
            //act
            var result = await _blogController.LikeArticle(1,
                                                _sessionMock.Object,
                                                _emailSenderMock.Object,
                                                _configurationMock.Object) as BadRequestObjectResult;
            var model = result!.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(model.Error, Is.EqualTo("ArticleAlreadyLiked"));

            _blogRepository.Verify(x => x.LikeArticle(It.IsAny<int>()), Times.Never());
        }
        [Test]
        public async Task LikeArticle_SomethingWentWrongWhenCallingLikeArticle_ReturnUnprocessableEntity()
        {
            //act
            var result = await _blogController.LikeArticle(1,
                                                _sessionMock.Object,
                                                _emailSenderMock.Object,
                                                _configurationMock.Object) as UnprocessableEntityObjectResult;
            var model = result!.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result.StatusCode, Is.EqualTo((int)HttpStatusCode.UnprocessableEntity));
            Assert.That(model.Error, Is.EqualTo("Unknown Error"));

            _blogRepository.Verify(x => x.LikeArticle(It.IsAny<int>()), Times.Once());
            _sessionMock.Verify(x => x.AddString(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }
        [Test]
        public async Task LikeArtic_ArticleBeenLiked_ReturnOk()
        {
            //arrange
            _blogRepository.Setup(x => x.LikeArticle(1)).ReturnsAsync(true);

            //act
            var result = await _blogController.LikeArticle(1,
                                                _sessionMock.Object,
                                                _emailSenderMock.Object,
                                                _configurationMock.Object) as OkObjectResult;
            var model = result!.Value as SuccesResponse ?? new();

            //assert
            Assert.That(result.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
            StringAssert.Contains("succesfully", model.Message);

            _blogRepository.Verify(x => x.LikeArticle(It.IsAny<int>()), Times.Once());
            _sessionMock.Verify(x => x.AddString(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }
        [Test]
        public async Task LikeArtic_SendingEmailTHrowsExceptionButWontBreakTheResponse_ReturnOk()
        {
            //arrange
            _blogRepository.Setup(x => x.LikeArticle(1)).ReturnsAsync(true);
            _configurationMock.Setup(x => x["PersonalEmail:Email"]).Returns("test@test.com");
            _emailSenderMock.Setup(x => x.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new Exception("error"));

            //act
            var result = await _blogController.LikeArticle(1,
                                                _sessionMock.Object,
                                                _emailSenderMock.Object,
                                                _configurationMock.Object) as OkObjectResult;
            var model = result!.Value as SuccesResponse ?? new();

            //assert
            Assert.That(result.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
            StringAssert.Contains("succesfully", model.Message);

            _blogRepository.Verify(x => x.LikeArticle(It.IsAny<int>()), Times.Once());
            _sessionMock.Verify(x => x.AddString(It.IsAny<string>(), It.IsAny<string>()), Times.Once());

            _configurationMock.Verify(x => x["PersonalEmail:Email"], Times.Once());
            _blogRepository.Verify(x => x.GetArticleTitle(1), Times.Once());
            _emailSenderMock.Verify(x => x.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }
        #endregion
        #region BlogPanel
        [Test]
        [TestCase(0)]
        [TestCase(-1)]
        public async Task BlogPanel_ArticlesPagesIsLessThanOrEqualToZero_ReturnRedirectToAction(int page)
        {
            //act
            var result = await _blogController.BlogPanel(_httpContextAccesor.Object, page) as RedirectToActionResult;

            //assert 

            Assert.That(result?.ActionName, Is.EqualTo("BlogPanel"));
            Assert.That(result?.ControllerName, Is.EqualTo("Blog"));
            _blogRepository.Verify(x => x.GetUserAuthor(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public async Task BlogPanel_CanCreateDeleteTagLinkAndAddTagLink()
        {
            //arrange
            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper.Setup(x => x.Action(It.IsAny<UrlActionContext>())).Returns("/test/test-link");
            _blogController.Url = mockUrlHelper.Object;
            _blogRepository.Setup(x => x.GetAllTags()).Returns(MockGetAllTags());

            //act
            var result = await _blogController.BlogPanel(_httpContextAccesor.Object, 1) as ViewResult;

            //assert
            Assert.That(result?.ViewData["DeleteTagLink"], Is.EqualTo("/test/test-link"));
            Assert.That(result?.ViewData["AddTagLink"], Is.EqualTo("/test/test-link"));

        }
        [Test]
        public async Task BlogPanel_CanCreateTheAbsoluteCurrentRequestUrl_ReturnViewResult()
        {
            //arrange
            var mockUrlHelper = new Mock<IUrlHelper>();
            _blogController.Url = mockUrlHelper.Object;
            _blogRepository.Setup(x => x.GetAllTags()).Returns(MockGetAllTags());

            //currentUrlSetup
            var mockHttpContext = new Mock<HttpContext>();
            var mockRequest = new Mock<HttpRequest>();
            _httpContextAccesor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
            mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
            mockRequest.Setup(x => x.Host).Returns(new HostString("test.com/test"));
            mockRequest.Setup(x => x.Scheme).Returns("http");

            //act
            var result = await _blogController.BlogPanel(_httpContextAccesor.Object, 1) as ViewResult;

            //assert
            Assert.That(result?.ViewData["CurrentUrl"], Is.EqualTo("http://test.com/test/"));
            _blogRepository.Verify(x => x.GetTagsCount(), Times.Once());

        }
        [Test]
        public async Task BlogPanel_CanGetAllTagsAndTagsCount_ReturnView()
        {
            //arrange
            var mockUrlHelper = new Mock<IUrlHelper>();
            _blogController.Url = mockUrlHelper.Object;
            _blogRepository.Setup(x => x.GetAllTags()).Returns(MockGetAllTags());
            _blogRepository.Setup(x => x.GetTagsCount()).ReturnsAsync(3);

            var result = (await _blogController.BlogPanel(_httpContextAccesor.Object, 1) as ViewResult)?.ViewData.Model as BlogPanelViewModel ?? new();
            var tags = result.Tags.ToList();


            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result.TagsCount, Is.EqualTo(3));
                Assert.That(tags[0].Id, Is.EqualTo(1));
                Assert.That(tags[0].Name, Is.EqualTo("Test"));
                Assert.That(tags[1].Id, Is.EqualTo(2));
                Assert.That(tags[1].Name, Is.EqualTo("Test2"));
            });
            _blogRepository.Verify(x => x.GetTagsCount(), Times.Once());
        }
        [Test]
        public async Task BlogPanel_AuthorIsNotFoundMeansItWasNotCreated_WorksSmoothlyWithout_ReturnView()
        {
            //arrange
            var mockUrlHelper = new Mock<IUrlHelper>();
            _blogController.Url = mockUrlHelper.Object;
            _blogRepository.Setup(x => x.GetAllTags()).Returns(MockGetAllTags());

            //act
            var result = (await _blogController.BlogPanel(_httpContextAccesor.Object, 1) as ViewResult)?.ViewData.Model as BlogPanelViewModel ?? new();
            var pagination = result.Pagination;

            //Assert
            Assert.That(result.ArticlesLikesCount, Is.EqualTo(0));
            Assert.That(pagination.TotalItems, Is.EqualTo(0));
            Assert.That(result.Articles.Count(), Is.EqualTo(0));

            _statusService.Verify(x => x.GetUserId(), Times.Once());
            _blogRepository.Verify(x => x.GetUserAuthor(It.IsAny<string>()), Times.Once());
        }
        [Test]
        public async Task BlogPanel_GetAuthorArticlesCountThrowException_ResponsIsNotBroken_ReturnView()
        {
            //arrange
            var mockUrlHelper = new Mock<IUrlHelper>();
            _blogController.Url = mockUrlHelper.Object;
            _blogRepository.Setup(x => x.GetAllTags()).Returns(MockGetAllTags());
            _blogRepository.Setup(x => x.GetAllAuthorArticlesCount(It.IsAny<int>())).ThrowsAsync(new Exception("test"));
            _blogRepository.Setup(x => x.GetUserAuthor(It.IsAny<string>())).ReturnsAsync(new BlogAuthor());

            //act
            var result = (await _blogController.BlogPanel(_httpContextAccesor.Object, 1) as ViewResult)?.ViewData.Model as BlogPanelViewModel ?? new();
            var pagination = result.Pagination;

            //Assert
            Assert.That(result.ArticlesLikesCount, Is.EqualTo(0));
            Assert.That(pagination.TotalItems, Is.EqualTo(0));
            Assert.That(result.Articles.Count(), Is.EqualTo(0));
            Assert.That(pagination.CurrentPage, Is.EqualTo(1));

            _blogRepository.Verify(x => x.GetAllAuthorArticlesCount(It.IsAny<int>()), Times.Once());
            _blogRepository.Verify(x => x.GetArticlesLikesCountPerAuthor(It.IsAny<int>()), Times.Never());
        }
        [Test]
        public async Task BlogPanel_GetArticlesLikesCountThrowException_ResponsIsNotBroken_ReturnView()
        {
            //arrange
            var mockUrlHelper = new Mock<IUrlHelper>();
            _blogController.Url = mockUrlHelper.Object;
            _blogRepository.Setup(x => x.GetAllTags()).Returns(MockGetAllTags());
            _blogRepository.Setup(x => x.GetAllAuthorArticlesCount(It.IsAny<int>())).ReturnsAsync(4);//returns 4
            _blogRepository.Setup(x => x.GetArticlesLikesCountPerAuthor(It.IsAny<int>())).ThrowsAsync(new Exception("test"));
            _blogRepository.Setup(x => x.GetUserAuthor(It.IsAny<string>())).ReturnsAsync(new BlogAuthor());

            //act
            var result = (await _blogController.BlogPanel(_httpContextAccesor.Object, 1) as ViewResult)?.ViewData.Model as BlogPanelViewModel ?? new();
            var pagination = result.Pagination;

            //Assert
            Assert.That(result.ArticlesLikesCount, Is.EqualTo(0));
            Assert.That(pagination.TotalItems, Is.EqualTo(4));//4 from Setup
            Assert.That(result.Articles.Count(), Is.EqualTo(0));
            Assert.That(pagination.CurrentPage, Is.EqualTo(1));

            _blogRepository.Verify(x => x.GetAllAuthorArticlesCount(It.IsAny<int>()), Times.Once());
            _blogRepository.Verify(x => x.GetArticlesLikesCountPerAuthor(It.IsAny<int>()), Times.Once());
            _blogRepository.Verify(x => x.GetAuthorArticlesByDescending(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never());

            _blogRepository.Verify(x => x.GetTagsCount(), Times.Once());
        }
        [Test]
        public async Task BlogPanel_GetAuthorArticlesByDescendingThrowsException_ResponsIsNotBroken_ReturnView()
        {
            //arrange
            var mockUrlHelper = new Mock<IUrlHelper>();
            _blogController.Url = mockUrlHelper.Object;
            _blogRepository.Setup(x => x.GetAllTags()).Returns(MockGetAllTags());
            _blogRepository.Setup(x => x.GetAllAuthorArticlesCount(It.IsAny<int>())).ReturnsAsync(4);//returns 4
            _blogRepository.Setup(x => x.GetArticlesLikesCountPerAuthor(It.IsAny<int>())).ReturnsAsync(22);//returns 22
            _blogRepository.Setup(x => x.GetAuthorArticlesByDescending(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).Throws(new Exception("test"));
            _blogRepository.Setup(x => x.GetUserAuthor(It.IsAny<string>())).ReturnsAsync(new BlogAuthor());

            //act
            var result = (await _blogController.BlogPanel(_httpContextAccesor.Object, 1) as ViewResult)?.ViewData.Model as BlogPanelViewModel ?? new();
            var pagination = result.Pagination;

            //Assert
            Assert.That(result.ArticlesLikesCount, Is.EqualTo(22));//22 from Setup
            Assert.That(pagination.TotalItems, Is.EqualTo(4));//4 from Setup
            Assert.That(result.Articles.Count(), Is.EqualTo(0));
            Assert.That(pagination.CurrentPage, Is.EqualTo(1));

            _blogRepository.Verify(x => x.GetAllAuthorArticlesCount(It.IsAny<int>()), Times.Once());
            _blogRepository.Verify(x => x.GetArticlesLikesCountPerAuthor(It.IsAny<int>()), Times.Once());
            _blogRepository.Verify(x => x.GetAuthorArticlesByDescending(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once());

            _blogRepository.Verify(x => x.GetTagsCount(), Times.Once());
        }
        [Test]
        public async Task BlogPanel_CanRetrieveArticlesTotalLikesCountTotalArticlesCount_ReturnView()
        {
            //arrange
            var mockUrlHelper = new Mock<IUrlHelper>();
            _blogController.Url = mockUrlHelper.Object;
            _blogRepository.Setup(x => x.GetAllTags()).Returns(MockGetAllTags());
            _blogRepository.Setup(x => x.GetAllAuthorArticlesCount(It.IsAny<int>())).ReturnsAsync(4);//returns 4
            _blogRepository.Setup(x => x.GetArticlesLikesCountPerAuthor(It.IsAny<int>())).ReturnsAsync(22);//returns 22
            _blogRepository.Setup(x => x.GetAuthorArticlesByDescending(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                           .Returns(MockGetUserBlogArticlesByDescending());
            _blogRepository.Setup(x => x.GetUserAuthor(It.IsAny<string>())).ReturnsAsync(new BlogAuthor());

            //act
            var result = (await _blogController.BlogPanel(_httpContextAccesor.Object, 1) as ViewResult)?.ViewData.Model as BlogPanelViewModel ?? new();
            var pagination = result.Pagination;
            var artclesList = result.Articles.ToList();

            //Assert
            Assert.That(result.ArticlesLikesCount, Is.EqualTo(22));//22 from Setup
            Assert.That(pagination.TotalItems, Is.EqualTo(4));//4 from Setup

            Assert.That(artclesList.Count(), Is.EqualTo(4));
            Assert.That(artclesList[0].Id, Is.EqualTo(1));
            Assert.That(artclesList[1].Id, Is.EqualTo(2));
            Assert.That(artclesList[2].Id, Is.EqualTo(3));
            Assert.That(artclesList[0].Title, Is.EqualTo("TestArticle1"));
            Assert.That(artclesList[1].Title, Is.EqualTo("TestArticle2"));
            Assert.That(artclesList[2].Title, Is.EqualTo("TestArticle3"));

            Assert.That(pagination.CurrentPage, Is.EqualTo(1));

            _blogRepository.Verify(x => x.GetAllAuthorArticlesCount(It.IsAny<int>()), Times.Once());
            _blogRepository.Verify(x => x.GetArticlesLikesCountPerAuthor(It.IsAny<int>()), Times.Once());
            _blogRepository.Verify(x => x.GetAuthorArticlesByDescending(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once());

            _blogRepository.Verify(x => x.GetTagsCount(), Times.Once());
        }
        #endregion
        #region Tags
        [Test]
        [TestCase(0)]
        [TestCase(-1)]
        public async Task DeleteTag_IsLessThenOrEqualToZero_ReturnBadRequest(int tagId)
        {
            //act
            var result = await _blogController.DeleteTag(tagId) as BadRequestObjectResult;
            var model = result?.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result?.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(model.Error, Is.EqualTo("InvalidTagId"));

            _blogRepository.Verify(x => x.DeleteTag(It.IsAny<int>()), Times.Never());
        }
        [Test]
        public async Task DeleteTag_ThrowInvalidOperationException_ReturnNotFound()
        {
            //arrange
            int tagId = 1;//pass the validation
            _blogRepository.Setup(x => x.DeleteTag(tagId)).ThrowsAsync(new InvalidOperationException("testError"));

            //act
            var result = await _blogController.DeleteTag(tagId) as NotFoundObjectResult;
            var model = result?.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result?.StatusCode, Is.EqualTo((int)HttpStatusCode.NotFound));
            Assert.That(model.Error, Is.EqualTo("TagNotFound"));
            _blogRepository.Verify(x => x.DeleteTag(tagId), Times.Once());
            _blogRepository.Verify(x => x.GetTagsCount(), Times.Never());

            Assert.ThrowsAsync<InvalidOperationException>(() => _blogRepository.Object.DeleteTag(tagId), "testError");
        }
        [Test]
        public async Task DeleteTag_ThrowsExeption_ReturnUnprocessableEntity()
        {
            //arrange
            int tagId = 1;//pass the validation
            _blogRepository.Setup(x => x.DeleteTag(tagId)).ThrowsAsync(new Exception("testError"));

            //act
            var result = await _blogController.DeleteTag(tagId) as UnprocessableEntityObjectResult;
            var model = result?.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result?.StatusCode, Is.EqualTo((int)HttpStatusCode.UnprocessableEntity));
            Assert.That(model.Error, Is.EqualTo("UnknownError"));
            _blogRepository.Verify(x => x.DeleteTag(tagId), Times.Once());
            _blogRepository.Verify(x => x.GetTagsCount(), Times.Never());

            Assert.ThrowsAsync<Exception>(() => _blogRepository.Object.DeleteTag(tagId), "testError");
        }
        [Test]
        public async Task DeleteTag_CanGetTagsCountAndReturnTheDEletedTagId_ReturnOk()
        {
            //arrange
            int tagId = 1;//pass the validation
            _blogRepository.Setup(x => x.GetTagsCount()).ReturnsAsync(4);//4 test

            //act
            var result = await _blogController.DeleteTag(tagId) as OkObjectResult;
            //reflection required for anonymous objects
            var model = result?.Value?.GetType().GetProperties();
            var returnedTagId = model?.FirstOrDefault(x => x.Name == "tagId")?.GetValue(result?.Value);
            var returnedTagsCount = model?.FirstOrDefault(x => x.Name == "tagsCount")?.GetValue(result?.Value);
            
            //assert
            Assert.That(result?.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
            Assert.That(returnedTagId, Is.EqualTo(tagId));
            Assert.That(returnedTagsCount, Is.EqualTo(4));
            _blogRepository.Verify(x => x.DeleteTag(tagId), Times.Once());
            _blogRepository.Verify(x => x.GetTagsCount(), Times.Once());
        }
        [Test]
        [TestCase("")]
        [TestCase(null)]
        public async Task AddTag_TagNameIsNullOrEmpty_ReturnBadRequest(string tagName)
        {
            //act
            var result = await _blogController.AddTag(tagName!) as BadRequestObjectResult;
            var model = result?.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result?.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(model.Error, Is.EqualTo("InvalidTagName"));

            _blogRepository.Verify(x => x.AddTag(It.IsAny<Tag>()), Times.Never());
        }
        [Test]
        public async Task AddTag_ThrowsInvalidOperationException_ReturnBadRequest()
        {
            //arrange
            _blogRepository.Setup(x => x.AddTag(It.IsAny<Tag>())).ThrowsAsync(new InvalidOperationException("testError"));

            //act
            var result = await _blogController.AddTag("testTag") as BadRequestObjectResult;
            var model = result?.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result?.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(model.Error, Is.EqualTo("TagNameExist"));

            _blogRepository.Verify(x => x.AddTag(It.IsAny<Tag>()), Times.Once());
            _blogRepository.Verify(x => x.GetTagByName(It.IsAny<string>()), Times.Never());
        }
        [Test]
        public async Task AddTag_ThrowsInvalidOperationExceptionAndGetTagByNameReturnDEfault_ReturnUnprocessableEntity()
        {
            //arrange
            _blogRepository.Setup(x => x.AddTag(It.IsAny<Tag>())).ThrowsAsync(new Exception("testError"));

            //act
            var result = await _blogController.AddTag("testTag") as UnprocessableEntityObjectResult;
            var model = result?.Value as ErrorResponse ?? new();

            //assert
            Assert.That(result?.StatusCode, Is.EqualTo((int)HttpStatusCode.UnprocessableEntity));
            Assert.That(model.Error, Is.EqualTo("UnprocessableEntity"));

            _blogRepository.Verify(x => x.AddTag(It.IsAny<Tag>()), Times.Once());
            _blogRepository.Verify(x => x.GetTagByName(It.IsAny<string>()), Times.Once());
        }
        [Test]
        public async Task AddTag_WorkSmothly_ReturnOk()
        {
            //arrange
            var tag = new Tag { Id = 1, Name = "testTag" };
            _blogRepository.Setup(x => x.GetTagByName("testTag")).ReturnsAsync(tag);
            _blogRepository.Setup(x => x.GetTagsCount()).ReturnsAsync(1);
            //act
            var result = await _blogController.AddTag("testTag") as OkObjectResult;
            
            //reflection required because of anonymous objects
            var model = result?.Value?.GetType().GetProperties();
            var returnedTag = model?.FirstOrDefault(x => x.Name == "tag")?.GetValue(result?.Value) as Tag ?? new();
            var returnedTagCount = (int)model?.FirstOrDefault(x => x.Name == "tagsCount")?.GetValue(result?.Value)!;

            //assert
            Assert.That(result?.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
            Assert.That(returnedTag.Id, Is.EqualTo(1));
            Assert.That(returnedTag.Name, Is.EqualTo("testTag"));
            Assert.That(returnedTagCount, Is.EqualTo(1));

            _blogRepository.Verify(x => x.AddTag(It.IsAny<Tag>()), Times.Once());
            _blogRepository.Verify(x => x.GetTagByName(It.IsAny<string>()), Times.Once());
        }

        #endregion

    }
}
