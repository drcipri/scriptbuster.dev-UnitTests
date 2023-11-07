using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using scriptbuster.dev.Models.Repository;
using scriptbuster.dev.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace scriptbuster.dev_UnitTests.Pages
{
    [TestFixture]
    internal class FAQTests
    {
        public async IAsyncEnumerable<FAQ> MockGetAllFAQs()
        {
            var faqs = new List<FAQ>
            {
                  new FAQ
                    {
                        Id = 1,
                        Question = "TESTQuestion",
                        Answer = "TestAnswer",
                    },
                   new FAQ
                    {
                        Id = 2,
                        Question = "TESTQuestion2",
                        Answer = "TestAnswer",
                    },
                    new FAQ
                    {
                        Id = 3,
                        Question = "TESTQuestion",
                        Answer = "TestAnswer",
                    },
            };

            foreach (var faq in faqs)
            {
                yield return faq;
            }
            await Task.CompletedTask;
        }
        [Test]
        public async Task OnGet_CanGetAllFAQs()
        {
            //arrange
            var mock = new Mock<IRepositoryFAQ>();
            mock.Setup(x => x.GetAllFAQs()).Returns(MockGetAllFAQs());

            var modelMetadataProvider = new EmptyModelMetadataProvider();
            var modelState = new ModelStateDictionary();

            var page = new FAQModel(mock.Object);
            page.PageContext.ViewData = new ViewDataDictionary(modelMetadataProvider, modelState);

            //act
            await page.OnGet();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(page.FAQs, Has.Count.EqualTo(3));
                Assert.That(page.FAQs[0].Id, Is.EqualTo(1));
                Assert.That(page.FAQs[1].Question, Is.EqualTo("TESTQuestion2"));
                Assert.That(page.ViewData["CurrentPage"] as string, Is.EqualTo("FAQ"));
            });
        }
    }
}
