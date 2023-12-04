using Microsoft.AspNetCore.Mvc;
using scriptbuster.dev.Controllers;
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
    internal class PoliciesTests
    {
        private Mock<IRepositoryPolicy> _policy;
        private PoliciesController _controller;
        [SetUp]
        public void SetUp()
        {
            _policy = new Mock<IRepositoryPolicy>();
            _controller = new PoliciesController(_policy.Object);
        }

        [Test]
        [TestCase(0)]
        [TestCase(-1)]
        public async Task ReadPolicy_IsLessThanOrEqualToZero_ReturnRedirectToPage(int policyId)
        {
            //ct
            var result = await _controller.ReadPolicy(policyId) as RedirectToPageResult;

            //Assert
            Assert.That(result?.PageName, Is.EqualTo("/ClientInfo"));
            _policy.Verify(x => x.GetPolicy(It.IsAny<int>()), Times.Never());
        }
        [Test]  
        public async Task ReadPolicy_PolicyIsNotFoundInTheDB_ReturnREdirect()
        {
            //arrange
            Policy nullPolicy = default!;
            _policy.Setup(x => x.GetPolicy(It.IsAny<int>())).ReturnsAsync(nullPolicy);

            //act
            var result = await _controller.ReadPolicy(1) as RedirectToPageResult;

            //asert
            //Assert
            Assert.That(result?.PageName, Is.EqualTo("/ClientInfo"));
            _policy.Verify(x => x.GetPolicy(It.IsAny<int>()), Times.Once());
        }

        [Test]
        public async Task ReadPolicy_PolicyIsFOund_ReturnView()
        {
            //arrange
            Policy policy = new Policy { Id = 1, Name = "TestPolicy"};
            _policy.Setup(x => x.GetPolicy(It.IsAny<int>())).ReturnsAsync(policy);

            //act
            var result = (await _controller.ReadPolicy(1) as ViewResult)?.ViewData?.Model as Policy ?? new();

            //asert
            //Assert
            Assert.That(result?.Id, Is.EqualTo(1));
            Assert.That(result?.Name, Is.EqualTo("TestPolicy"));
            _policy.Verify(x => x.GetPolicy(It.IsAny<int>()), Times.Once());
        }
        [Test]
        [TestCase(0)]
        [TestCase(-1)]
        public async Task EditPolicy_IsLessThanOrEqualToZero_ReturnRedirectToPage(int policyId)
        {
            //ct
            var result = await _controller.EditPolicy(policyId) as RedirectToPageResult;

            //Assert
            Assert.That(result?.PageName, Is.EqualTo("/ErrorInfo"));
            _policy.Verify(x => x.GetPolicy(It.IsAny<int>()), Times.Never());
        }
        [Test]
        public async Task EditPolicy_PolicyIsNotFoundInTheDB_ReturnREdirect()
        {
            //arrange
            Policy nullPolicy = default!;
            _policy.Setup(x => x.GetPolicy(It.IsAny<int>())).ReturnsAsync(nullPolicy);

            //act
            var result = await _controller.EditPolicy(1) as RedirectToPageResult;

            //asert
            //Assert
            Assert.That(result?.PageName, Is.EqualTo("/ErrorInfo"));
            _policy.Verify(x => x.GetPolicy(It.IsAny<int>()), Times.Once());
        }

        [Test]
        public async Task EditPolicy_PolicyIsFOund_ReturnView()
        {
            //arrange
            Policy policy = new Policy { Id = 1, Name = "TestPolicy" };
            _policy.Setup(x => x.GetPolicy(It.IsAny<int>())).ReturnsAsync(policy);

            //act
            var result = (await _controller.EditPolicy(1) as ViewResult)?.ViewData?.Model as Policy ?? new();

            //asert
            //Assert
            Assert.That(result?.Id, Is.EqualTo(1));
            Assert.That(result?.Name, Is.EqualTo("TestPolicy"));
            _policy.Verify(x => x.GetPolicy(It.IsAny<int>()), Times.Once());
        }
        [Test]
        public async Task UpdatePolicy_ModelStateIsNotValid_ReturnEditPolicyView()
        {
            //arrange
            _controller.ModelState.AddModelError("error", "test error");
            Policy policy = new Policy { Id = 1, Name = "TestPolicy" };
            _policy.Setup(x => x.GetPolicy(It.IsAny<int>())).ReturnsAsync(policy);

            //act
            var result = await _controller.UpdatePolicy(default!) as ViewResult;
            var model = result?.ViewData?.Model as Policy ?? new();

            //assert
            Assert.That(result?.ViewName, Is.EqualTo("EditPolicy"));
            Assert.That(model?.Id, Is.EqualTo(1));
            Assert.That(model?.Name, Is.EqualTo("TestPolicy"));
            _policy.Verify(x => x.GetPolicy(It.IsAny<int>()), Times.Once());
            Assert.That(result.ViewData["DisplayErrors"], Is.True);
        }
        [Test]
        public async Task UpdatePolicy_Works_ReturnEditPolicyView()
        {
            //arrange
       
            Policy policy = new Policy { Id = 1, Name = "TestPolicy" };

            //act
            var result = await _controller.UpdatePolicy(policy) as RedirectToActionResult;

            //assert
            Assert.That(result?.ActionName, Is.EqualTo("EditPolicy"));
            _policy.Verify(x => x.GetPolicy(It.IsAny<int>()), Times.Never());
            Assert.That(result?.RouteValues?["policyId"], Is.EqualTo(1));
        }
    }
}
