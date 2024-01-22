using Microsoft.AspNetCore.Mvc.ViewComponents;
using scriptbuster.dev.IdentityModels.Repository;
using scriptbuster.dev.IdentityModels.Tables;
using scriptbuster.dev.ViewComponents;
using scriptbuster.dev.ViewComponents.VCModels;
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
                },
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
            var result = await vc.InvokeAsync(new List<BlogArticleTags>()) as ViewViewComponentResult;
            var model = result?.ViewData?.Model as BlogArticlesTagsVCViewModel ?? new();
            var listTag = model.AllTags.ToList();

            //assert
            Assert.That(listTag.Count, Is.EqualTo(2));
            Assert.That(listTag?[0].Id, Is.EqualTo(1));
            Assert.That(listTag?[0].Name, Is.EqualTo("Test"));
            Assert.That(listTag?[1].Id, Is.EqualTo(2));
            Assert.That(listTag?[1].Name, Is.EqualTo("Test2"));

            mockBlog.Verify(x => x.GetAllTags(), Times.Once());
        }
        [Test]
        public async Task Invoke_CanGetAllBlogArticleTagsFromViewAndFilterTHemFromNulls()
        {
            //arrange
            var tagsFromView = new List<BlogArticleTags>
            {
                new BlogArticleTags
                {
                    BlogArticleId = 1,
                    TagId = 1,
                    Tag = new Tag
                          {
                            Id = 1,
                            Name="Test",
                          },
                },

                new BlogArticleTags
                {
                    BlogArticleId = 2,
                    TagId = 2,
                    Tag = new Tag
                          {
                            Id = 2,
                            Name="Test2",
                          },
                },
                new BlogArticleTags
                {
                    BlogArticleId = 3,
                    TagId = 3,
                }
            };

            var mockBlog = new Mock<IBlogRepository>();
            mockBlog.Setup(x => x.GetAllTags()).Returns(MockGetAllTags());
            var vc = new BlogArticleTagsVC(mockBlog.Object);

            //act
            var result = await vc.InvokeAsync(tagsFromView) as ViewViewComponentResult;
            var model = result?.ViewData?.Model as BlogArticlesTagsVCViewModel ?? new();
            var listTag = model?.ArticleTags?.ToList();

            //assert
            Assert.That(listTag?.Count, Is.EqualTo(2));
            Assert.That(listTag?[0].Id, Is.EqualTo(1));
            Assert.That(listTag?[0].Name, Is.EqualTo("Test"));
            Assert.That(listTag?[1].Id, Is.EqualTo(2));
            Assert.That(listTag?[1].Name, Is.EqualTo("Test2"));
        }
    }
}
