using System;
using NUnit.Framework;
using Matrix.Client;
using Matrix.Structures;
namespace Matrix.Tests
{
    [TestFixture]
    public class MatrixRoomTests
    {
        [Test]
        public void CreateMatrixRoomTest()
        {
            MatrixRoom room = new MatrixRoom(null, "!abc:localhost");
            Assert.That(room.ID, Is.EqualTo("!abc:localhost"), "The Room ID must be correct.");
            Assert.That(room.Members, Is.Empty, "The Room must have no members.");
        }

        [Test]
        public void FeedEventCreatorTest() {
            MatrixRoom room = new MatrixRoom(null, "!abc:localhost");
            var creationEvent = new MatrixMRoomCreate() {
                creator = "@Half-Shot:localhost",
                mfederate = false,
            };
            room.FeedEvent(Utils.MockEvent(creationEvent));
            Assert.That(room.Creator, Is.EqualTo("@Half-Shot:localhost"), "Creator is correct.");
            Assert.That(room.ShouldFederate, Is.False, "Should not federate.");
        }

        [Test]
        public void FeedEventNameTest() {
            MatrixRoom room = new MatrixRoom(null, "!abc:localhost");
            var ev = new MatrixMRoomName() {
                name = "Snug Fox Party!"
            };
            room.FeedEvent(Utils.MockEvent(ev));
            Assert.That(room.Name, Is.EqualTo("Snug Fox Party!"), "Name is correct.");
        }

        [Test]
        public void FeedEventTopicTest() {
            MatrixRoom room = new MatrixRoom(null, "!abc:localhost");
            var ev = new MatrixMRoomTopic() {
                topic = "Foxes welcome!"
            };
            room.FeedEvent(Utils.MockEvent(ev));
            Assert.That(room.Topic, Is.EqualTo("Foxes welcome!"), "Topic is correct.");
        }
    }
}
