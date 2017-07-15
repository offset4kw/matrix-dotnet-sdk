using System;
using NUnit.Framework;
using Matrix.Client;
namespace Matrix.Tests
{
    [TestFixture]
    public class MatrixRoomTests
    {
      [Test, Description("{c} - {m}")]
      public void CreateMatrixRoom()
      {
          MatrixRoom room = new MatrixRoom(null, "!abc:localhost");
          Assert.That(room.ID, Is.EqualTo("!abc:localhost"));
        }
    }
}
