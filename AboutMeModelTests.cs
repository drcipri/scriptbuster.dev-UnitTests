using Microsoft.AspNetCore.Mvc.ModelBinding;
using scriptbuster.dev.Models.Repository;
using scriptbuster.dev.Models.Tables;
using scriptbuster.dev.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace scriptbuster.dev_UnitTests
{
    [TestFixture]
    internal class AboutMeModelTests
    {
        [Test]
        public async Task OnGet_CanGetAboutMePageData()
        {
            //arrange
            var mock = new Mock<IRepositoryAboutMePage>();
            mock.Setup(x => x.GetAboutMePageDataAsync(It.IsAny<int>())).ReturnsAsync(new AboutMePage
            {
                Id = 1,
                Name = "Name",
                Proffesion = "Test",
                AboutMeDescription = "Test",
                SkilsDescription = "Test"
            });
            var modelMetadataProvider = new EmptyModelMetadataProvider();
            var modelState = new ModelStateDictionary();
            var page = new AboutMeModel(mock.Object);
            page.PageContext.ViewData = new ViewDataDictionary(modelMetadataProvider, modelState);
            
            //act
            await page.OnGet();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(page.AboutMe, Is.Not.Null);
                Assert.That(page.AboutMe?.Id, Is.EqualTo(1));
                Assert.That(page.AboutMe?.AboutMeDescription, Is.EqualTo("Test"));
                Assert.That(page.AboutMe?.Proffesion , Is.EqualTo("Test"));

                Assert.That(page.ViewData["CurrentPage"], Is.EqualTo("AboutMe"));
            });
        }
    }
}
