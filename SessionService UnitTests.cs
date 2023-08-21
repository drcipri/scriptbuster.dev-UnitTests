using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using scriptbuster.dev.Services.SessionService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scriptbuster.dev_UnitTests
{
    public class TestClass
    {
        public string? TestProperty { get; set;}
    }
    [TestFixture]
    internal class SessionService_UnitTests
    {
        private Mock<IHttpContextAccessor> _contextAccessor;
        private Mock<ILogger<SessionService>> _logger;
        private Mock<HttpContext> _mockHttpContext;
        private Mock<ISession> _session;
        private SessionService _sessionService;
        [SetUp]
        public void SetUp() 
        {
            _contextAccessor= new Mock<IHttpContextAccessor>();
            _logger= new Mock<ILogger<SessionService>>();
            _session= new Mock<ISession>();

            _mockHttpContext = new Mock<HttpContext>();

            _contextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
            _contextAccessor.Setup(x => x.HttpContext!.Session).Returns(_session.Object);

            _sessionService = new SessionService(_contextAccessor.Object, _logger.Object);
        }
        #region AddObject
        [Test]
        public void AddObject_ObjIsNull_ThrowsException()
        {
            TestClass test = default!;
            //assert
            Assert.ThrowsAsync<ArgumentNullException>(() => _sessionService.AddObject("testKey", test));
        }
        [Test]
        [TestCase(null)]
        [TestCase("")]
        public void AddObject_KeyIsNullOrEmpty_ThrowsException(string key)
        {
            TestClass test = new TestClass();
            //assert
            Assert.ThrowsAsync<ArgumentNullException>(() => _sessionService.AddObject(key, test));
        }
        [Test]
        public void AddObject_ISessionIsNull_ThrowException()
        {
            //arrange
            _contextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
            _contextAccessor.Setup(x => x.HttpContext!.Session).Returns(() => default!);
            _sessionService = new SessionService(_contextAccessor.Object, _logger.Object);

            //assert
            Assert.ThrowsAsync<NullReferenceException>(() => _sessionService.AddObject<TestClass>("Test", new TestClass()));
        }
        [Test]
        public async Task AddObject_Works()
        {
            await _sessionService.AddObject("TestKey", new TestClass());

            _session.Verify(x => x.Set(It.Is<string>(x => x == "TestKey"), It.IsAny<byte[]>()),Times.Once()); 
        }
        #endregion

        #region AddInt32
        [Test]
        [TestCase(null)]
        [TestCase("")]
        public void AddInt32_KeyIsNullOrEmpty_ThrowsException(string key)
        {
            //assert
            Assert.ThrowsAsync<ArgumentNullException>(() => _sessionService.AddInt32(key,2));
        }
        [Test]
        public void AddInt32_ISessionIsNull_ThrowsException()
        {
            //arrange
            _contextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
            _contextAccessor.Setup(x => x.HttpContext!.Session).Returns(() => default!);
            _sessionService = new SessionService(_contextAccessor.Object, _logger.Object);

            //assert
            Assert.ThrowsAsync<NullReferenceException>(() => _sessionService.AddInt32("TestKey",2));
        }
        [Test]
        public async Task AddInt32_Works()
        {
            await _sessionService.AddInt32("TestKey", 2);

            _session.Verify(x => x.Set(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Once());
        }
        #endregion

        #region AddString
        [Test]
        [TestCase(null)]
        [TestCase("")]
        public void AddString_KeyIsNullOrEmpty_ThrowsException(string key)
        {
            //assert
            Assert.ThrowsAsync<ArgumentNullException>(() => _sessionService.AddString(key, "Test"));
        }
        [Test]
        public void AddString_ISessionIsNull_ThrowsException()
        {
            //arrange
            _contextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
            _contextAccessor.Setup(x => x.HttpContext!.Session).Returns(() => default!);
            _sessionService = new SessionService(_contextAccessor.Object, _logger.Object);

            //assert
            Assert.ThrowsAsync<NullReferenceException>(() => _sessionService.AddString("TestKey", "Test"));
        }
        [Test]
        public async Task AddString_Works()
        {
            await _sessionService.AddString("TestKey", "Test");

            _session.Verify(x => x.Set(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Once());
        }
        #endregion

        #region GetObject
        [Test]
        [TestCase(null)]
        [TestCase("")]
        public void GetObject_KeyIsNullOrEmpty_ThrowsException(string key)
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => _sessionService.GetObject<TestClass>(key));
        }
        [Test]
        public void GetObject_ISessionIsNull_ThrowsException()
        {
            //arrange
            _contextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
            _contextAccessor.Setup(x => x.HttpContext!.Session).Returns(() => default!);
            _sessionService = new SessionService(_contextAccessor.Object, _logger.Object);

            //assert
            Assert.ThrowsAsync<NullReferenceException>(() => _sessionService.GetObject<TestClass>("TestKey"));
        }
        [Test]
        public async Task GetObject_Works()
        {
             await _sessionService.GetObject<TestClass>("Test");
           
            byte[] outTest;

            //assert
            _session.Verify(x => x.TryGetValue(It.Is<string>(x => x == "Test"),out outTest!), Times.Once());
        }
        #endregion

        #region GetInt32
        [Test]
        [TestCase(null)]
        [TestCase("")]
        public void GetInt32_KeyIsNullOrEmpty_ThrowsException(string key)
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => _sessionService.GetInt32(key));
        }
        [Test]
        public void GetInt32_ISessionIsNull_ThrowsException()
        {
            //arrange
            _contextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
            _contextAccessor.Setup(x => x.HttpContext!.Session).Returns(() => default!);
            _sessionService = new SessionService(_contextAccessor.Object, _logger.Object);

            //assert
            Assert.ThrowsAsync<NullReferenceException>(() => _sessionService.GetInt32("TestKey"));
        }
        [Test]
        public async Task GetInt32_Works()
        {
            await _sessionService.GetInt32("Test");

            byte[] outTest;

            //assert
            _session.Verify(x => x.TryGetValue(It.Is<string>(x => x == "Test"), out outTest!), Times.Once());
        }
        #endregion

        #region GetString
        [Test]
        [TestCase(null)]
        [TestCase("")]
        public void GetString_KeyIsNullOrEmpty_ThrowsException(string key)
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => _sessionService.GetString(key));
        }
        [Test]
        public void GetString_ISessionIsNull_ThrowsException()
        {
            //arrange
            _contextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
            _contextAccessor.Setup(x => x.HttpContext!.Session).Returns(() => default!);
            _sessionService = new SessionService(_contextAccessor.Object, _logger.Object);

            //assert
            Assert.ThrowsAsync<NullReferenceException>(() => _sessionService.GetString("TestKey"));
        }
        [Test]
        public async Task GetString_Works()
        {
            await _sessionService.GetString("Test");

            byte[] outTest;

            //assert
            _session.Verify(x => x.TryGetValue(It.Is<string>(x => x == "Test"), out outTest!), Times.Once());
        }
        #endregion

        #region RemoveKey
        [Test]
        [TestCase(null)]
        [TestCase("")]
        public void RemoveKey_KeyIsNullOrEmpty_ThrowsException(string key)
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => _sessionService.RemoveKey(key));
        }
        [Test]
        public void RemoveKey_ISessionIsNull_ThrowsException()
        {
            //arrange
            _contextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
            _contextAccessor.Setup(x => x.HttpContext!.Session).Returns(() => default!);
            _sessionService = new SessionService(_contextAccessor.Object, _logger.Object);

            //assert
            Assert.ThrowsAsync<NullReferenceException>(() => _sessionService.RemoveKey("TestKey"));
        }
        [Test]
        public async Task RemoveKey_Works()
        {
            //act
            await _sessionService.RemoveKey("Test");

            //assert
            _session.Verify(x => x.Remove(It.Is<string>(x => x == "Test")), Times.Once());
        }
        #endregion

        #region ClearSession
        [Test]
        public void ClearSession_ISessionIsNull_ThrowsException()
        {
            //arrange
            _contextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
            _contextAccessor.Setup(x => x.HttpContext!.Session).Returns(() => default!);
            _sessionService = new SessionService(_contextAccessor.Object, _logger.Object);

            //assert
            Assert.Throws<NullReferenceException>(() => _sessionService.ClearSession());
        }
        [Test]
        public void ClearSession_Works()
        {
            //act
            _sessionService.ClearSession();
            
            //assert
            _session.Verify(x => x.Clear(), Times.Once());
        }
        #endregion
    }
}
