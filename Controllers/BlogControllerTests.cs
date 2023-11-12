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

            _blogController = new BlogController(_statusService.Object,
                                                 _blogRepository.Object,
                                                 _logger.Object,
                                                 _webHost.Object,
                                                 _htmlDocumentService.Object,
                                                 _fileSystemService.Object,
                                                 _directoryInfoWrapper.Object);
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
                                                      
            _formFileArray = new IFormFile[] {mockSecondFormFile.Object,  _formFile.Object };

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
           
            var tagsCollection = new List<int> { 1,2,3 };

            //act
            var result = await _blogController.PostArticle(default!, article, default!,tagsCollection) as BadRequestObjectResult;
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

    }
}
