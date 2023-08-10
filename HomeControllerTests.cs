using Microsoft.AspNetCore.Mvc;
using Moq;
using scriptbuster.dev.Controllers;
using scriptbuster.dev.Models.Repository;
using scriptbuster.dev.Models.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scriptbuster.dev_UnitTests
{
    [TestFixture]
    internal class HomeControllerTests
    {
        [Test]
        public async Task Index_CanGetHomePageData_ReturnItsView()
        {
            //arrange
            var moqHome = new Mock<IRepositoryHomePage>();
            moqHome.Setup(x => x.GetHomePageDataAsync(It.IsAny<int>())).ReturnsAsync(new HomePage
            {
                Id= 1,
                FullName = "TestFullName",
                Profession = "TestProffesion",
                WebsiteIntro = "Welcome Test"
            });
            var controller = new HomeController(moqHome.Object);

            //act
            var result = (await controller.Index() as ViewResult)?.ViewData.Model as HomePage ?? new();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(1));
                Assert.That(result.FullName, Is.EqualTo("TestFullName"));
                Assert.That(result.Profession, Is.EqualTo("TestProffesion"));
                Assert.That(result.WebsiteIntro, Is.EqualTo("Welcome Test"));
            });
        }
    }
}
