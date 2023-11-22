using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;
using scriptbuster.dev.Infrastructure.TagHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using scriptbuster.dev.Infrastructure;

namespace scriptbuster.dev_UnitTests.TagHelpers
{
    [TestFixture]
    public class PaginationTagHelperTests
    {
        private Mock<IUrlHelperFactory> _urlFactory;
        private PaginationTagHelper _paginationTagHelper;
        private Mock<IUrlHelper> _urlHelper;
        private Mock<ViewContext> _viewContext;
        private Mock<TagHelperContent> _tagHelperContent;

        [SetUp]
        public void SetUp()
        {
            _urlFactory = new Mock<IUrlHelperFactory>();
            _urlHelper = new Mock<IUrlHelper>();
            _viewContext = new Mock<ViewContext>();
            _tagHelperContent = new Mock<TagHelperContent>();
            _paginationTagHelper = new PaginationTagHelper(_urlFactory.Object);
        }

        [Test]  
        public void Process_ViewContextIsNull_LinksAreNotGenerated()
        {
            //arrange
            _paginationTagHelper.PaginationModel = new Pagination();
            _paginationTagHelper.EndpointParameterName = "articlePage";
            _paginationTagHelper.ViewContext = null;
            var context = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "");
            var output = new TagHelperOutput("div", new TagHelperAttributeList(), (c, e) => Task.FromResult(_tagHelperContent.Object));

            //act
            _paginationTagHelper.Process(context, output);

            //assert
            Assert.That(output.Content.GetContent(), Is.Empty);
        }
        [Test]
        [TestCase(null)]
        [TestCase("")]
        public void Process_EndpointParameterNameIsNullOrEmpty_LinksAreNotGenerated(string parameterName)
        {
            //arrange
            _paginationTagHelper.PaginationModel = new Pagination();
            _paginationTagHelper.EndpointParameterName = parameterName;
            _paginationTagHelper.ViewContext = _viewContext.Object;
            var context = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "");
            var output = new TagHelperOutput("div", new TagHelperAttributeList(), (c, e) => Task.FromResult(_tagHelperContent.Object));

            //act
            _paginationTagHelper.Process(context, output);

            //assert
            Assert.That(output.Content.GetContent(), Is.Empty);
        }
        [Test]
        public void Process_PaginationModelIsNull_LinksAreNotGenerated()
        {
            //arrange
            _paginationTagHelper.PaginationModel = null;
            _paginationTagHelper.EndpointParameterName = "articlePage";
            _paginationTagHelper.ViewContext = _viewContext.Object;
            var context = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "");
            var output = new TagHelperOutput("div", new TagHelperAttributeList(), (c, e) => Task.FromResult(_tagHelperContent.Object));

            //act
            _paginationTagHelper.Process(context, output);

            //assert
            Assert.That(output.Content.GetContent(), Is.Empty);
        }

        [Test]
        public void Process_CanGenerateLinksAndStyleTheContentAndCreateAndIdForTheLinksContainer()
        {
            //arrange
            _paginationTagHelper.PaginationModel = new Pagination
            {
                CurrentPage = 1,
                PageSize = 3,
                TotalItems = 9 //ensure Url helper is called 3 times. 9/3 = 3
            };
            _paginationTagHelper.EndpointParameterName = "articlePage";
            _paginationTagHelper.ViewContext = _viewContext.Object;

            _urlHelper.SetupSequence(x => x.Action(It.IsAny<UrlActionContext>())).Returns("/test/Page1")
                                                                                 .Returns("/test/Page2")
                                                                                 .Returns("/test/Page3");
            _urlFactory.Setup(x => x.GetUrlHelper(It.IsAny<ActionContext>()))
                       .Returns(_urlHelper.Object);

            _paginationTagHelper.ContainerCssClassCollection.Add("testCssContainerClass", "AddedContainerCssClass");
            _paginationTagHelper.LinkCssClassCollection.Add("testCssLinkClass", "AddedLinkCssClass");
            _paginationTagHelper.SetIdAttribute = "ContainerTestId";

            
            var context = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "");
            var output = new TagHelperOutput("div", new TagHelperAttributeList(), (c, e) => Task.FromResult(_tagHelperContent.Object));

            //act
            _paginationTagHelper.Process(context, output);

            var content = output.Content.GetContent();

            //assert
            Assert.That(content, Does.Contain("\"AddedLinkCssClass\" href=\"/test/Page1\""));
            Assert.That(content, Does.Contain("/test/Page1"));
            Assert.That(content, Does.Contain("/test/Page2"));
            Assert.That(content, Does.Contain("/test/Page3"));
            Assert.That(output.Attributes["id"].Value, Is.EqualTo("ContainerTestId"));
            Assert.That(output.Attributes["class"].Value as string, Does.Contain("AddedContainerCssClass"));

            _urlHelper.Verify(x => x.Action(It.IsAny<UrlActionContext>()), Times.AtLeast(3));
        }
    }
}
