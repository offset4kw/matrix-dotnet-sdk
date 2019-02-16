using System;
using Moq;
using Matrix.Structures;
using Matrix;
namespace Matrix.Tests
{
    public class Utils
    {
        public static MatrixEvent MockEvent(
            MatrixEventContent content,
            string stateKey = null,
            int age = 0)
        {
            MatrixEvent ev = new MatrixEvent();
            ev.content = content;
            if(stateKey != null) {
                ev.state_key = stateKey;
            }
            ev.age = age;
            return ev;
        }

        public static Mock<MatrixAPI> MockApi() {
            var mock = new Mock<MatrixAPI>("https://localhost");
            mock.Setup(f => f.UserId).Returns("@foobar:localhost");
            mock.Setup(f => f.BaseURL).Returns("https://localhost");
            mock.Setup(f => f.GetSyncToken()).Returns("AGoodSyncToken");
            mock.Setup(f => f.GetAccessToken()).Returns("AGoodAccessToken");
            mock.Setup(f => f.GetCurrentLogin()).Returns(new MatrixLoginResponse());
            mock.Setup(f => f.RunningInitialSync).Returns(false);
            mock.Setup(f => f.RoomStateSend(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MatrixRoomStateEvent>(),
                It.IsAny<string>())
            );
            return mock;
        }
    }
}
