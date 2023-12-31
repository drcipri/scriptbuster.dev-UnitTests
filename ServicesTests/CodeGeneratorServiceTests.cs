﻿using scriptbuster.dev.Services.CodeGeneratorService;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scriptbuster.dev_UnitTests.ServicesTests
{
    [TestFixture]
    internal class CodeGeneratorServiceTests
    {
        [Test]
        [TestCase(-1)]
        [TestCase(0)]

        public void GenerateCode_CodeLengthIsLessThenOrEqualToZero_THrowException(int codeLength)
        {
            //arrange
            var mock = new Mock<ILogger<CodeGenerator>>();
            var codeGenerator = new CodeGenerator(mock.Object);

            //assert
            Assert.Throws<ArgumentException>(() => codeGenerator.GenerateCode(codeLength));
        }
        [Test]
        [TestCase(11)]
        [TestCase(200)]

        public void GenerateCode_CodeLengthIsBiggerThan10_THrowException(int codeLength)
        {
            //arrange
            var mock = new Mock<ILogger<CodeGenerator>>();
            var codeGenerator = new CodeGenerator(mock.Object);

            //assert
            Assert.Throws<ArgumentException>(() => codeGenerator.GenerateCode(codeLength));
        }

        [Test]
        [TestCase(2)]
        [TestCase(8)]
        public void GenerateCode_CodeLengthIsBetween1And10_EverythingWOrks(int codeLength)
        {
            //arrange
            var mock = new Mock<ILogger<CodeGenerator>>();
            var codeGenerator = new CodeGenerator(mock.Object);

            //act
            var result = codeGenerator.GenerateCode(codeLength);

            //assert
            Assert.That(result.ToString($"D{codeLength}").Length, Is.EqualTo(codeLength));
            //ToString($"D{codeLength}") the code might come with 0 first digit and will be discarded if we dont preserve it and the Test will fail.
        }

        //generate API ke
        [Test]
        public void GenerateApiKey_ByteLengthIsLessThenZeor_ThrowException()
        {
            //arrange
            var mock = new Mock<ILogger<CodeGenerator>>();
            var codeGenerator = new CodeGenerator(mock.Object);

            //assert
            Assert.Throws<ArgumentException>(() => codeGenerator.GenerateApiKey(-1));
        }
        [Test]
        public void GeenerateApiKey_Works()
        {
            //arrange
            var mock = new Mock<ILogger<CodeGenerator>>();
            var codeGenerator = new CodeGenerator(mock.Object);

            //act
            var result = codeGenerator.GenerateApiKey();

            //assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.GreaterThan(0));
        }
    }
}
