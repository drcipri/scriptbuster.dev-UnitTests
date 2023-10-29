using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using scriptbuster.dev.IdentityModels.Repository;
using scriptbuster.dev.IdentityModels.Tables;
using scriptbuster.dev.Infrastructure.ViewModels.BlogController;
using scriptbuster.dev.Services.AutheticationService;
using scriptbuster.dev.ViewComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scriptbuster.dev_UnitTests
{
    [TestFixture]
    internal class BlogAuhtorVCTests
    {
        private Mock<IBlogRepository> _blogRepository;
        private Mock<IAuthStatusService> _authStatus;
        private Mock<IHttpContextAccessor> _accesor;
        private Mock<IUrlHelper> _urlHelper;
        private BlogAuthorVC _blogAuthorVC;
        [SetUp] public void SetUp() 
        {
            _blogRepository = new Mock<IBlogRepository>();
            _authStatus = new Mock<IAuthStatusService>();
            _accesor = new Mock<IHttpContextAccessor>();
            _urlHelper = new Mock<IUrlHelper>();
            _blogAuthorVC = new BlogAuthorVC(_blogRepository.Object, _authStatus.Object, _accesor.Object);
            
            //since i need this in every test i put this in the setup
            _blogAuthorVC.Url = _urlHelper.Object;
            _urlHelper.SetupSequence(x => x.Action(It.IsAny<UrlActionContext>()))
                      .Returns("test.com/pathTest1")
                      .Returns("test.com/pathTest2");
        }
        [Test]
        public async Task InvokeAsync_AuthorExist_ViewBagAuthorExistIsTrue()
        {
            //arrange
            _blogRepository.Setup(x => x.GetUserAuthor(It.IsAny<string>()))
                           .ReturnsAsync(new BlogAuthor());

            //act
            await _blogAuthorVC.InvokeAsync();
            var authorExist = _blogAuthorVC.ViewBag.AuthorExist;

            //assert
            Assert.That(authorExist, Is.True);
        }
        [Test]
        public async Task InvokeAsync_AuthroDoesNotExist_ViewBagAuthorExistIsTrue()
        {
            //arrange
            BlogAuthor nullBlogAuthor = default!;
            _blogRepository.Setup(x => x.GetUserAuthor(It.IsAny<string>()))
                           .ReturnsAsync(nullBlogAuthor);

            //act
            await _blogAuthorVC.InvokeAsync();
            var authorExist = _blogAuthorVC.ViewBag.AuthorExist;

            //assert
            Assert.That(authorExist, Is.False);
        }
        [Test]
        public async Task InvokeAsync_CanCreateLinksForTheAjaxCall()
        {

            //act
            var viewDataDictionary =  (await _blogAuthorVC.InvokeAsync() as ViewViewComponentResult)?.ViewData;
            var links = viewDataDictionary!["AuthorLinks"] as AuthorLinks ?? new();
            

            //assert
            Assert.That(links.AddAuthorLink, Is.EqualTo("test.com/pathTest1"));
            Assert.That(links.UpdateAuthorLink, Is.EqualTo("test.com/pathTest2"));
        }
        [Test]
        public async Task InvokeAsync_CanReturnTheBlogAuthor()
        {
            //arrange
            _blogRepository.Setup(x => x.GetUserAuthor(It.IsAny<string>()))
                          .ReturnsAsync(new BlogAuthor
                          {
                              Id = 1,
                              FullName = "TestName",
                              Proffesion = "TestProffesion",
                              UserId = "TestUserId"
                          });

            //act
            var model = (await _blogAuthorVC.InvokeAsync() as ViewViewComponentResult)?.ViewData?.Model as BlogAuthorVCViewModel ?? new();
            var author = model.Author;

            //assert
            Assert.That(author.Id , Is.EqualTo(1));
            Assert.That(author.FullName, Is.EqualTo("TestName"));
            Assert.That(author.Proffesion, Is.EqualTo("TestProffesion"));
            Assert.That(author.UserId, Is.EqualTo("TestUserId"));
        }

    }
}
