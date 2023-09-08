using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using scriptbuster.dev.Controllers;
using scriptbuster.dev.Models.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using scriptbuster.dev.Infrastructure;

namespace scriptbuster.dev_UnitTests
{
    [TestFixture]
    internal class FAQAdminControllerTests
    {
        private Mock<ILogger<FaqAdminController>> _logger;
        private Mock<IRepositoryFAQ> _repFAQ;
        private FaqAdminController _controller;
        [SetUp]
        public void SetUp()
        {
            _logger = new Mock<ILogger<FaqAdminController>>();
            _repFAQ = new Mock<IRepositoryFAQ>();
            _controller = new FaqAdminController(_repFAQ.Object, _logger.Object);
        }

        public async IAsyncEnumerable<FAQ> MockGetAllFAQs()
        {
            IEnumerable<FAQ> list = new List<FAQ>
            {
                new FAQ
                {
                    Id = 1,
                    Question = "Test Question 1",
                    Answer = "Test Answer 1"
                },
                new FAQ
                {
                    Id = 2,
                    Question = "Test Question 2",
                    Answer = "Test Answer 2"
                },
                new FAQ
                {
                    Id = 3,
                    Question = "Test Question 3",
                    Answer = "Test Answer 3"
                }
            };
            foreach(var question in list)
            {
                yield return question;
            }
            await Task.CompletedTask;
        }

        [Test]
        public async Task FaqPanel_CanGetAllFAQS()
        {
            //arrange   
            _repFAQ.Setup(x => x.GetAllFAQs()).Returns(MockGetAllFAQs());

            //act
            var result =  (await _controller.FaqPanel() as ViewResult)?.ViewData.Model as List<FAQ> ?? new();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result[0].Id , Is.EqualTo(1));
                Assert.That(result[0].Question, Is.EqualTo("Test Question 1"));
                Assert.That(result[0].Answer, Is.EqualTo("Test Answer 1"));
                Assert.That(result[2].Id, Is.EqualTo(3));
                Assert.That(result[2].Question, Is.EqualTo("Test Question 3"));
                Assert.That(result[2].Answer, Is.EqualTo("Test Answer 3"));
            });
        }

        [Test]
        public async Task AddQuestion_ModelStateIsNotValid_ReturnFaqPanelView()
        {
            //arrange   
            _repFAQ.Setup(x => x.GetAllFAQs()).Returns(MockGetAllFAQs());
            var faq = new FAQBindingTarget();
            _controller.ModelState.AddModelError("error", "Test Error");
            //act
            var result = await _controller.AddQuestion(faq) as ViewResult;
            var model = result?.ViewData.Model as List<FAQ> ?? new();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result?.ViewName, Is.EqualTo("FaqPanel"));
                Assert.That((bool)result?.ViewData["DisplayErrors"]!, Is.True);
                Assert.That(result.ViewData.ModelState.ContainsKey("error"), Is.True);

                Assert.That(model[0].Id, Is.EqualTo(1));
                Assert.That(model[0].Question, Is.EqualTo("Test Question 1"));
                Assert.That(model[0].Answer, Is.EqualTo("Test Answer 1"));
                Assert.That(model[2].Id, Is.EqualTo(3));
                Assert.That(model[2].Question, Is.EqualTo("Test Question 3"));
                Assert.That(model[2].Answer, Is.EqualTo("Test Answer 3"));
            });
        }
        [Test]
        public async Task AddQuestion_Works_RedirectTOFaqPanelAction()
        {
            //arrange   
            var faq = new FAQBindingTarget 
            {
                Question = "Test Question",
                Answer = "Test Answer"
            };
            //act
            var result = await _controller.AddQuestion(faq) as RedirectToActionResult;

            var toFaq = faq.ToFAQ();

            //assert
            Assert.That(result?.ActionName, Is.EqualTo("FaqPanel"));
            _repFAQ.Verify(x => x.AddFaq(It.Is<FAQ>(model => model.Question == toFaq.Question && model.Answer == toFaq.Answer)), Times.Once());
        }

        [Test]
        public async Task EditQuestion_ModelStateIsNotValid_ReturnFaqPanelView()
        {
            //arrange   
            _repFAQ.Setup(x => x.GetAllFAQs()).Returns(MockGetAllFAQs());
            var faq = new FAQ();
            _controller.ModelState.AddModelError("error", "Test Error");
            //act
            var result = await _controller.EditQuestion(faq) as ViewResult;
            var model = result?.ViewData.Model as List<FAQ> ?? new();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result?.ViewName, Is.EqualTo("FaqPanel"));
                Assert.That((bool)result?.ViewData["DisplayErrors"]!, Is.True);
                Assert.That(result.ViewData.ModelState.ContainsKey("error"), Is.True);

                Assert.That(model[0].Id, Is.EqualTo(1));
                Assert.That(model[0].Question, Is.EqualTo("Test Question 1"));
                Assert.That(model[0].Answer, Is.EqualTo("Test Answer 1"));
                Assert.That(model[2].Id, Is.EqualTo(3));
                Assert.That(model[2].Question, Is.EqualTo("Test Question 3"));
                Assert.That(model[2].Answer, Is.EqualTo("Test Answer 3"));
            });
        }
        [Test]
        public async Task EditQuestion_Works_RedirectTOFaqPanelAction()
        {
            //arrange   
            var faq = new FAQ
            {
                Id= 1,
                Question = "Test Question",
                Answer = "Test Answer"
            };
            //act
            var result = await _controller.EditQuestion(faq) as RedirectToActionResult;

            //assert
            Assert.That(result?.ActionName, Is.EqualTo("FaqPanel"));
            _repFAQ.Verify(x => x.EditFaq(It.Is<FAQ>(model => model.Question == faq.Question && model.Answer == faq.Answer)), Times.Once());
        }

        [Test]
        public async Task DeleteQuestion_IdIsNotValid_ReturnFaqPanelView()
        {
            //arrange   
            _repFAQ.Setup(x => x.GetAllFAQs()).Returns(MockGetAllFAQs());
            _repFAQ.Setup(x => x.DeleteFaq(It.IsAny<int>())).ReturnsAsync(false);
            //act
            var result = await _controller.DeleteQuestion(1) as ViewResult;
            var model = result?.ViewData.Model as List<FAQ> ?? new();

            //assert
            Assert.Multiple(() =>
            {
                Assert.That(result?.ViewName, Is.EqualTo("FaqPanel"));
                Assert.That((bool)result?.ViewData["DisplayErrors"]!, Is.True);
                Assert.That(result.ViewData.ModelState.ContainsKey("Delete"), Is.True);

                Assert.That(model[0].Id, Is.EqualTo(1));
                Assert.That(model[0].Question, Is.EqualTo("Test Question 1"));
                Assert.That(model[0].Answer, Is.EqualTo("Test Answer 1"));
                Assert.That(model[2].Id, Is.EqualTo(3));
                Assert.That(model[2].Question, Is.EqualTo("Test Question 3"));
                Assert.That(model[2].Answer, Is.EqualTo("Test Answer 3"));
            });
        }
        [Test]
        public async Task DeleteQuestion_Works_RedirectTOFaqPanelAction()
        {
            //arrange   
            _repFAQ.Setup(x => x.DeleteFaq(It.IsAny<int>())).ReturnsAsync(true);
            //act
            var result = await _controller.DeleteQuestion(1) as RedirectToActionResult;

            //assert
            Assert.That(result?.ActionName, Is.EqualTo("FaqPanel"));
            _repFAQ.Verify(x => x.DeleteFaq(It.Is<int>(id => id == 1)), Times.Once());
        }
    }
}
