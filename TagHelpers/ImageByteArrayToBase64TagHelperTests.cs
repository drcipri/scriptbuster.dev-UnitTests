using Microsoft.AspNetCore.Razor.TagHelpers;
using scriptbuster.dev.Infrastructure.TagHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scriptbuster.dev_UnitTests.TagHelpers
{
    [TestFixture]
    internal class ImageByteArrayToBase64TagHelperTests
    {
        private Mock<TagHelperContent> _content;
        private ImageByteArrayToBase64TagHelper _helper;
        private TagHelperContext _tagHelperContext;
        private TagHelperOutput _tagHelperOutput;

        [SetUp]
        public void SetUp()
        {
            _content = new Mock<TagHelperContent>();
            _helper = new ImageByteArrayToBase64TagHelper();
            _tagHelperContext = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "");
            _tagHelperOutput = new TagHelperOutput("img", new TagHelperAttributeList(), (c, e) => Task.FromResult(_content.Object));
        }

        [Test]
        public void Process_ByteArrayIsNull_ReturnEmptySourceImg()
        {
            _helper.ByteImage = default;

            //act
            _helper.Process(_tagHelperContext, _tagHelperOutput);

            //assert
            Assert.That(_tagHelperOutput.Attributes["src"], Is.Null);
        }
        [Test]
        public void Process_ByteArrayImgIsNotNull_ReturnSourceImg()
        {
            _helper.ByteImage = new byte[] {1,23,23,210};

            //act
            _helper.Process(_tagHelperContext, _tagHelperOutput);

            //assert
            Assert.That(_tagHelperOutput.Attributes["src"], Is.Not.Null);
        }
        [Test]
        public void Process_IdAndAltAttributesAreNull_ReturnEmptyAttributes()
        {
            _helper.ImgAlt = default;
            _helper.SetId = default;

            //act
            _helper.Process(_tagHelperContext, _tagHelperOutput);

            //assert
            Assert.That(_tagHelperOutput.Attributes["id"].Value, Is.Null);
            Assert.That(_tagHelperOutput.Attributes["alt"].Value, Is.Null);
        }
        [Test]
        public void Process_IdAndAltAttributesAreSet_ReturnImgElementWithAttributes()
        {
            _helper.ImgAlt = "Test image";
            _helper.SetId = "Test-Id";

            //act
            _helper.Process(_tagHelperContext, _tagHelperOutput);

            //assert
            Assert.That(_tagHelperOutput.Attributes["id"].Value, Is.EqualTo("Test-Id"));
            Assert.That(_tagHelperOutput.Attributes["alt"].Value, Is.EqualTo("Test image"));
        }
    }
}
