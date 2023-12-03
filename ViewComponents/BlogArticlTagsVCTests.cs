using Microsoft.AspNetCore.Mvc.ViewComponents;
using scriptbuster.dev.IdentityModels.Repository;
using scriptbuster.dev.IdentityModels.Tables;
using scriptbuster.dev.ViewComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scriptbuster.dev_UnitTests.ViewComponents
{
    internal class BlogArticlTagsVCTests
    {
        public async IAsyncEnumerable<Tag> MockGetAllTags()
        {
            var list = new List<Tag>
            {
                new Tag
                {
                    Id = 1,
                    Name = "Test"
                },
                new Tag
                {
                    Id = 2,
                    Name = "Test2"
                }
            };
            foreach (var tag in list)
            {
                yield return tag;
            }

            await Task.CompletedTask;
        }

        [Test]
        public  async Task Invoke_CanGetAllTags_ReturnView()
        {
            //arrange
            var mockBlog = new Mock<IBlogRepository>();
            mockBlog.Setup(x => x.GetAllTags()).Returns(MockGetAllTags());

            var vc = new BlogArticleTagsVC(mockBlog.Object);

            //act
            var result = await vc.InvokeAsync() as ViewViewComponentResult;
            var model = result?.ViewData?.Model as IEnumerable<Tag>;
            var listTag = model?.ToList();

            //assert
            Assert.That(listTag?[0].Id, Is.EqualTo(1));
            Assert.That(listTag?[0].Name, Is.EqualTo("Test"));
            Assert.That(listTag?[1].Id, Is.EqualTo(2));
            Assert.That(listTag?[1].Name, Is.EqualTo("Test2"));

            mockBlog.Verify(x => x.GetAllTags(), Times.Once());
        }
    }
}
