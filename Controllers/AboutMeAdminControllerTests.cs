using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using scriptbuster.dev.Controllers;
using scriptbuster.dev.Infrastructure.ViewModels.AboutMeAdminController;
using scriptbuster.dev.Infrastructure.ViewModels.HomeController;
using scriptbuster.dev.Models.Repository;
using scriptbuster.dev.Models.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scriptbuster.dev_UnitTests.Controllers
{
    [TestFixture]
    internal class AboutMeAdminControllerTests
    {
        private Mock<IRepositoryAboutMePage> _mockRepo;
        private AboutMeAdminController _controller;
        [SetUp]
        public void SetUp()
        {
            _mockRepo = new Mock<IRepositoryAboutMePage>();
            _controller = new AboutMeAdminController(_mockRepo.Object);
        }
        [Test]
        public async Task AboutMePanel_CanGetAboutMePageData_ReturnItsView()
        {
            //arrange
            _mockRepo.Setup(x => x.GetAboutMePageDataAsync()).ReturnsAsync(new AboutMePage
            {
                Id = 1,
                Name = "TestFullName",
                Proffesion = "TestProffesion",
                AboutMeDescription = "Description Test",
                SkilsDescription = "Skills Test"
            });

            //act
            var result = (await _controller.AboutMePanel() as ViewResult)?.ViewData.Model as AboutMePanelViewModel ?? new();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result.AboutMe.Id, Is.EqualTo(1));
                Assert.That(result.AboutMe.Name, Is.EqualTo("TestFullName"));
                Assert.That(result.AboutMe.Proffesion, Is.EqualTo("TestProffesion"));
                Assert.That(result.AboutMe.AboutMeDescription, Is.EqualTo("Description Test"));
            });
        }
        [Test]
        public async Task UpdateAboutMe_ModelStateIsNotValid_ReturnAboutMeView()
        {
            //arrange
            _mockRepo.Setup(x => x.GetAboutMePageDataAsync()).ReturnsAsync(new AboutMePage
            {
                Id = 1,
                Name = "TestName",
                Proffesion = "TestProffesion",
                AboutMeDescription = "DescriptionTest"
            });

            var fakeModel = new AboutMePanelViewModel
            {
                AboutMe = new AboutMePage()
            };
            _controller.ModelState.AddModelError("error", "Some error");

            //act
            var result = await _controller.UpdateAboutMe(fakeModel) as ViewResult;
            var model = result?.ViewData.Model as AboutMePanelViewModel ?? new();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result!.ViewName, Is.EqualTo("AboutMePanel"));
                Assert.That(result!.ViewData.ModelState.ContainsKey("error"), Is.True);
                Assert.That((bool)result.ViewData["DisplayErrors"]!, Is.True);

                Assert.That(model.AboutMe.Id, Is.EqualTo(1));
                Assert.That(model.AboutMe.Name, Is.EqualTo("TestName"));
                Assert.That(model.AboutMe.Proffesion, Is.EqualTo("TestProffesion"));
            });
        }
        [Test]
        public async Task UpdateAboutMe_PictureIsNullButUpdateWorks_ReturnRedirectToPage()
        {
            //arrange
            var about = new AboutMePanelViewModel
            {
                AboutMe = new AboutMePage
                {
                    Id = 1,
                    Name = "TestName",
                    Proffesion = "TestProffesion",
                    AboutMeDescription = "DescriptionIntro"
                }
            };

            //act
            var result = await _controller.UpdateAboutMe(about) as RedirectToActionResult;

            //assert
            Assert.That(result!.ActionName, Is.EqualTo("AboutMePanel"));
            _mockRepo.Verify(x => x.UpdateAboutMePage(It.Is<AboutMePage>(args => args == about.AboutMe)), Times.Once());
        }

        [Test]
        public async Task UpdateAboutMePage_PictureIsNotNullButItsContentTypeIsWrong_ReturnAboutMePanel()
        {
            //arrange
            _mockRepo.Setup(x => x.GetAboutMePageDataAsync()).ReturnsAsync(new AboutMePage
            {
                Id = 1,
                Name = "TestName",
                Proffesion = "TestProffesion",
                AboutMeDescription = "DescriptionTest"
            });

            var moqForm = new Mock<IFormFile>();
            moqForm.Setup(x => x.ContentType).Returns("application/pdf");
            var about = new AboutMePanelViewModel
            {
                AboutMe = new AboutMePage(),
                Picture = moqForm.Object
            };


            //act
            var result = await _controller.UpdateAboutMe(about) as ViewResult;
            var model = result?.ViewData.Model as AboutMePanelViewModel ?? new();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result!.ViewName, Is.EqualTo("AboutMePanel"));
                Assert.That(result!.ViewData.ModelState.ContainsKey("Wrong Type"), Is.True);
                Assert.That((bool)result.ViewData["DisplayErrors"]!, Is.True);

                Assert.That(model.AboutMe.Id, Is.EqualTo(1));
                Assert.That(model.AboutMe.Name, Is.EqualTo("TestName"));
                Assert.That(model.AboutMe.Proffesion, Is.EqualTo("TestProffesion"));
            });
        }
        [Test]
        public async Task UpdateAbouMe_PictureIsNotNullButSizeIsBiggerThanMaxSize_ReturnAboutMePanel()
        {
            //arrange
            _mockRepo.Setup(x => x.GetAboutMePageDataAsync()).ReturnsAsync(new AboutMePage
            {
                Id = 1,
                Name = "TestName",
                Proffesion = "TestProffesion",
                AboutMeDescription = "DescriptionTest"
            });

            var moqForm = new Mock<IFormFile>();
            moqForm.Setup(x => x.ContentType).Returns("image/png");
            moqForm.Setup(x => x.Length).Returns(15 * 1024 * 1024);//15mb

            var about = new AboutMePanelViewModel
            {
                AboutMe = new AboutMePage(),
                Picture = moqForm.Object
            };


            //act
            var result = await _controller.UpdateAboutMe(about) as ViewResult;
            var model = result?.ViewData.Model as AboutMePanelViewModel ?? new();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result!.ViewName, Is.EqualTo("AboutMePanel"));
                Assert.That(result!.ViewData.ModelState.ContainsKey("Big Size"), Is.True);
                Assert.That((bool)result.ViewData["DisplayErrors"]!, Is.True);

                Assert.That(model.AboutMe.Id, Is.EqualTo(1));
                Assert.That(model.AboutMe.Name, Is.EqualTo("TestName"));
                Assert.That(model.AboutMe.Proffesion, Is.EqualTo("TestProffesion"));
            });
        }
        [Test]
        public async Task UpdateAboutMePage_ImageIsPngAndWorks_ReturAboutMePanelAction()
        {
            var moqForm = new Mock<IFormFile>();
            moqForm.Setup(x => x.ContentType).Returns("image/png");
            moqForm.Setup(x => x.Length).Returns(9 * 1024 * 1024);//9mb

            var about = new AboutMePanelViewModel
            {
                AboutMe = new AboutMePage
                {
                    Id = 1,
                    Name = "TestName",
                    Proffesion = "TestProffesion",
                    AboutMeDescription = "DescriptionIntro"
                },
                Picture = moqForm.Object
            };


            //act
            var result = await _controller.UpdateAboutMe(about) as RedirectToActionResult;

            //assert
            Assert.That(result!.ActionName, Is.EqualTo("AboutMePanel"));
            _mockRepo.Verify(x => x.UpdateAboutMePage(It.Is<AboutMePage>(argument => argument == about.AboutMe)), Times.Once());
        }
        [Test]
        public async Task UpdateAboutMePage_ImageIsJpegAndWorks_ReturnAboutMePanelAction()
        {
            var moqForm = new Mock<IFormFile>();
            moqForm.Setup(x => x.ContentType).Returns("image/jpeg");
            moqForm.Setup(x => x.Length).Returns(9 * 1024 * 1024);//9mb


            var about = new AboutMePanelViewModel
            {
                AboutMe = new AboutMePage
                {
                    Id = 1,
                    Name = "TestName",
                    Proffesion = "TestProffesion",
                    AboutMeDescription = "DescriptionIntro"
                },
                Picture = moqForm.Object
            };


            //act
            var result = await _controller.UpdateAboutMe(about) as RedirectToActionResult;

            //assert
            Assert.That(result!.ActionName, Is.EqualTo("AboutMePanel"));
            _mockRepo.Verify(x => x.UpdateAboutMePage(It.Is<AboutMePage>(argument => argument == about.AboutMe)), Times.Once());
        }
    }
}
