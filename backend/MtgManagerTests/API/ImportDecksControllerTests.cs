using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MtgManager.API;
using Shouldly;
using Utils.Sqlite.ORM;

namespace MtgManager.Tests
{
    [TestClass]
    public class ImportDecksControllerTests
    {
        private Mock<ISqliteORM<ArchidektUser>> _mockOrm;
        private Mock<ILogger<ImportDecksController>> _mockLog;
        private ImportDecksController _controller;

        public ImportDecksControllerTests()
        {
            _mockOrm = new Mock<ISqliteORM<ArchidektUser>>();
            _mockLog = new Mock<ILogger<ImportDecksController>>();

            _controller = new ImportDecksController(_mockOrm.Object, _mockLog.Object);
        }

        [TestMethod]
        public void ArchidektUser_Get_ReturnsUsernameWhenExists()
        {
            var existingUser = new ArchidektUser { Id = 1, Username = "Gideon" };
            _mockOrm.Setup(x => x.Get("Id = 1", null))
                    .Returns(new List<ArchidektUser> { existingUser });

            var result = _controller.ArchidektUser();

            result.Value.ShouldBe("Gideon");
        }

        [TestMethod]
        public void ArchidektUser_Get_ReturnsNullWhenNoUserSaved()
        {
            _mockOrm.Setup(x => x.Get("Id = 1", null))
                    .Returns(Enumerable.Empty<ArchidektUser>());

            var result = _controller.ArchidektUser();

            result.Value.ShouldBeNull();
        }

        [TestMethod]
        public void ArchidektUser_Put_SavesUserSuccessfully()
        {
            string newUsername = "Liliana";

            var result = _controller.ArchidektUser(newUsername);

            _mockOrm.Verify(x => x.Upsert(It.Is<ArchidektUser>(r =>
                r.Id == 1 && r.Username == newUsername)), Times.Once);

            var jsonResult = result.ShouldBeOfType<JsonResult>();
            jsonResult.Value.ShouldNotBeNull();
            jsonResult.Value!.ToString()!.ShouldContain("success = True");
        }

        [TestMethod]
        public void ArchidektUser_Put_ReturnsErrorOnEmptyInput()
        {
            var result = _controller.ArchidektUser("");

            var jsonResult = result.ShouldBeOfType<JsonResult>();
            jsonResult.StatusCode.ShouldBe(400);
            _mockOrm.Verify(x => x.Upsert(It.IsAny<ArchidektUser>()), Times.Never);
        }

        [TestMethod]
        public void ArchidektUser_Get_HandlesExceptionGracefully()
        {
            _mockOrm.Setup(x => x.Get(It.IsAny<string>(), null))
                    .Throws(new Exception("Database connection failed"));

            var result = _controller.ArchidektUser();

            result.StatusCode.ShouldBe(500);
            result.Value.ShouldBeNull();

            _mockLog.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }
    }
}