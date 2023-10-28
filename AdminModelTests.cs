using scriptbuster.dev.Models.Repository;
using scriptbuster.dev.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scriptbuster.dev_UnitTests
{
    [TestFixture]
    internal class AdminModelTests
    {
        [Test]
        public async Task OnGet_CanGetAllMessagesAndFaqsInformation()
        {
            //assert
            var mockMessages = new Mock<IRepositoryMessage>();
            mockMessages.Setup(x => x.GetTotalMessages()).ReturnsAsync(5);
            mockMessages.Setup(x => x.GetUnreadMessages()).ReturnsAsync(1);
            var mockProjects = new Mock<IRepositoryProjectMessage>();
            mockProjects.Setup(x => x.GetUnreadMessages()).ReturnsAsync(1);
            mockProjects.Setup(x => x.GetTotalMessages()).ReturnsAsync(5);
            var mockFaq = new Mock<IRepositoryFAQ>();
            mockFaq.Setup(x => x.GetTotalFaqs()).ReturnsAsync(5);
         
            var model = new AdminModel(mockFaq.Object, mockMessages.Object, mockProjects.Object);
            //act
            await model.OnGet();

            //Assert
            Assert.That(model.ModelsInfo.UnreadMessages, Is.EqualTo(1));
            Assert.That(model.ModelsInfo.TotalMessages, Is.EqualTo(5));
            Assert.That(model.ModelsInfo.UnreadProjectMessages, Is.EqualTo(1));
            Assert.That(model.ModelsInfo.TotalProjectMessages, Is.EqualTo(5));
            Assert.That(model.ModelsInfo.TotalFaqs, Is.EqualTo(5));
        }
    }
}
