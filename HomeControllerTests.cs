using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Moq;
using scriptbuster.dev.Controllers;
using scriptbuster.dev.Infrastructure.ViewModels.HomeController;
using scriptbuster.dev.Models.Repository;
using scriptbuster.dev.Models.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace scriptbuster.dev_UnitTests
{
    [TestFixture]
    internal class HomeControllerTests
    {
        private Mock<IRepositoryHomePage> _mockRepositoryHome;
        private HomeController _controller;
        [SetUp]
        public void SetUp() 
        {
            _mockRepositoryHome = new Mock<IRepositoryHomePage>();
            _controller = new HomeController(_mockRepositoryHome.Object);
        } 
        [Test]
        public async Task Index_CanGetHomePageData_ReturnItsView()
        {
            //arrange
            _mockRepositoryHome.Setup(x => x.GetHomePageDataAsync()).ReturnsAsync(new HomePage
            {
                Id= 1,
                FullName = "TestFullName",
                Profession = "TestProffesion",
                WebsiteIntro = "Welcome Test"
            });

            //act
            var result = (await _controller.Index() as ViewResult)?.ViewData.Model as HomePage ?? new();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(1));
                Assert.That(result.FullName, Is.EqualTo("TestFullName"));
                Assert.That(result.Profession, Is.EqualTo("TestProffesion"));
                Assert.That(result.WebsiteIntro, Is.EqualTo("Welcome Test"));
            });
        }

        [Test]
        public async Task HomePanel_CanGetHomePageData_ReturnItsView()
        {
            //arrange
            _mockRepositoryHome.Setup(x => x.GetHomePageDataAsync()).ReturnsAsync(new HomePage
            {
                Id = 1,
                FullName = "TestFullName",
                Profession = "TestProffesion",
                WebsiteIntro = "Welcome Test"
            });

            //act
            var result = (await _controller.HomePanel() as ViewResult)?.ViewData.Model as HomePanelViewModel ?? new();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result.HomePage.Id, Is.EqualTo(1));
                Assert.That(result.HomePage.FullName, Is.EqualTo("TestFullName"));
                Assert.That(result.HomePage.Profession, Is.EqualTo("TestProffesion"));
                Assert.That(result.HomePage.WebsiteIntro, Is.EqualTo("Welcome Test"));
            });
        }
        [Test]
        public async Task UpdateHomePage_ModelStateIsNotValid_ReturnHomePanelView()
        {
            //arrange
            _mockRepositoryHome.Setup(x => x.GetHomePageDataAsync()).ReturnsAsync(new HomePage
            {
                Id = 1,
                FullName = "TestName",
                Profession = "TestProffesion",
                WebsiteIntro = "Test Intro"
            });

            var fakeModel = new HomePanelViewModel
            {
                HomePage = new HomePage()
            };
            _controller.ModelState.AddModelError("error", "Some error");

            //act
            var result = await _controller.UpdateHomePage(fakeModel) as ViewResult;
            var model = result?.ViewData.Model as HomePanelViewModel ?? new();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result!.ViewName, Is.EqualTo("HomePanel"));
                Assert.That(result!.ViewData.ModelState.ContainsKey("error"), Is.True);
                Assert.That((bool)result.ViewData["DisplayErrors"]!, Is.True);

                Assert.That(model.HomePage.Id, Is.EqualTo(1));
                Assert.That(model.HomePage.FullName, Is.EqualTo("TestName"));
                Assert.That(model.HomePage.Profession, Is.EqualTo("TestProffesion"));
            });
        }
        [Test]
        public async Task UpdateHomePage_PictureIsNullButUpdateWorks_ReturnRedirectToPage()
        {
            //arrange
            var home = new HomePanelViewModel
            {
                HomePage = new HomePage
                {
                    Id = 1,
                    FullName = "TestName",
                    Profession = "TestProffesion",
                    WebsiteIntro = "Test Intro"
                }
            };

            //act
           var result =  await _controller.UpdateHomePage(home) as RedirectToActionResult;

            //assert
            Assert.That(result!.ActionName, Is.EqualTo("HomePanel"));
            _mockRepositoryHome.Verify(x => x.UpdateHomePageAsync(It.Is<HomePage>(args => args == home.HomePage)), Times.Once());
        }

        [Test]
        public async Task UpdateHomePage_PictureIsNotNullButItsContentTypeIsWrong_ReturnHomePanel()
        {
            //arrange
            _mockRepositoryHome.Setup(x => x.GetHomePageDataAsync()).ReturnsAsync(new HomePage
            {
                Id = 1,
                FullName = "TestName",
                Profession = "TestProffesion",
                WebsiteIntro = "Test Intro"
            });
          
            var moqForm = new Mock<IFormFile>();
            moqForm.Setup(x => x.ContentType).Returns("application/pdf");
            var home = new HomePanelViewModel
            {
                HomePage = new HomePage(),
                Picture = moqForm.Object
            };


            //act
           var result = await _controller.UpdateHomePage(home) as ViewResult;
           var model = result?.ViewData.Model as HomePanelViewModel ?? new();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result!.ViewName, Is.EqualTo("HomePanel"));
                Assert.That(result!.ViewData.ModelState.ContainsKey("Wrong Type"), Is.True);
                Assert.That((bool)result.ViewData["DisplayErrors"]!, Is.True);

                Assert.That(model.HomePage.Id, Is.EqualTo(1));
                Assert.That(model.HomePage.FullName, Is.EqualTo("TestName"));
                Assert.That(model.HomePage.Profession, Is.EqualTo("TestProffesion"));
            });
        }
        [Test]
        public async Task UpdateHomePage_PictureIsNotNullButSizeIsBiggerThanMaxSize_ReturnHomePanel()
        {
            //arrange
            _mockRepositoryHome.Setup(x => x.GetHomePageDataAsync()).ReturnsAsync(new HomePage
            {
                Id = 1,
                FullName = "TestName",
                Profession = "TestProffesion",
                WebsiteIntro = "Test Intro"
            });

            var moqForm = new Mock<IFormFile>();
            moqForm.Setup(x => x.ContentType).Returns("image/png");
            moqForm.Setup(x => x.Length).Returns(15 * 1024 * 1024);//15mb
            
            var home = new HomePanelViewModel
            {
                HomePage = new HomePage(),
                Picture = moqForm.Object
            };


            //act
            var result = await _controller.UpdateHomePage(home) as ViewResult;
            var model = result?.ViewData.Model as HomePanelViewModel ?? new();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result!.ViewName, Is.EqualTo("HomePanel"));
                Assert.That(result!.ViewData.ModelState.ContainsKey("Big Size"), Is.True);
                Assert.That((bool)result.ViewData["DisplayErrors"]!, Is.True);

                Assert.That(model.HomePage.Id, Is.EqualTo(1));
                Assert.That(model.HomePage.FullName, Is.EqualTo("TestName"));
                Assert.That(model.HomePage.Profession, Is.EqualTo("TestProffesion"));
            });
        }
        [Test]
        public async Task UpdateHomePage_ImageIsPngAndWorks_ReturHomePanelAction()
        {
            var moqForm = new Mock<IFormFile>();
            moqForm.Setup(x => x.ContentType).Returns("image/png");
            moqForm.Setup(x => x.Length).Returns(9 * 1024 * 1024);//9mb

            var home = new HomePanelViewModel
            {
                HomePage = new HomePage
                {
                    Id = 1,
                    FullName = "TestName",
                    Profession = "TestProffesion",
                    WebsiteIntro = "Test Intro"
                },
                Picture = moqForm.Object
            };


            //act
            var result = await _controller.UpdateHomePage(home) as RedirectToActionResult;

            //assert
            Assert.That(result!.ActionName, Is.EqualTo("HomePanel"));
            _mockRepositoryHome.Verify(x => x.UpdateHomePageAsync(It.Is<HomePage>(argument => argument == home.HomePage)), Times.Once());
        }
        [Test]
        public async Task UpdateHomePage_ImageIsJpegAndWorks_ReturHomePanelAction()
        {
            var moqForm = new Mock<IFormFile>();
            moqForm.Setup(x => x.ContentType).Returns("image/jpeg");
            moqForm.Setup(x => x.Length).Returns(9 * 1024 * 1024);//9mb

            var home = new HomePanelViewModel
            {
                HomePage = new HomePage
                {
                    Id = 1,
                    FullName = "TestName",
                    Profession = "TestProffesion",
                    WebsiteIntro = "Test Intro"
                },
                Picture = moqForm.Object
            };


            //act
            var result = await _controller.UpdateHomePage(home) as RedirectToActionResult;

            //assert
            Assert.That(result!.ActionName, Is.EqualTo("HomePanel"));
            _mockRepositoryHome.Verify(x => x.UpdateHomePageAsync(It.Is<HomePage>(argument => argument == home.HomePage)), Times.Once());
        }
    }
}
