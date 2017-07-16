using System;
using NUnit.Framework;
using Matrix.Client;
using Matrix.Structures;
using Moq;
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

        [Test]
        public void FeedEventAliasesTest() {
            MatrixRoom room = new MatrixRoom(null, "!abc:localhost");
            string[] aliases =  new string[] {
                "#cookbook:resturant",
                "#menu:resturant"
            };
            var ev = new MatrixMRoomAliases() {
                aliases = aliases
            };
            room.FeedEvent(Utils.MockEvent(ev));
            Assert.That(room.Aliases, Is.EquivalentTo(aliases), "Aliases are correct.");
            aliases = new string[] {
                "#wok:resturant",
                "#fryingpan:resturant"
            };
            ev = new MatrixMRoomAliases() {
                aliases = aliases
            };
            room.FeedEvent(Utils.MockEvent(ev));
            Assert.That(room.Aliases, Is.EquivalentTo(aliases), "Changed aliases are correct.");
        }

        [Test]
        public void FeedEventCanonicalAliasTest() {
            MatrixRoom room = new MatrixRoom(null, "!abc:localhost");
            var ev = new MatrixMRoomCanonicalAlias() {
                alias = "#resturant:resturant"
            };
            room.FeedEvent(Utils.MockEvent(ev));
            Assert.That(room.CanonicalAlias, Is.EqualTo("#resturant:resturant"), "The canonical alias is correct.");
        }

        [Test]
        public void FeedEventJoinRuleTest() {
            MatrixRoom room = new MatrixRoom(null, "!abc:localhost");
            var ev = new MatrixMRoomJoinRules() {
                join_rule = EMatrixRoomJoinRules.Public
            };
            room.FeedEvent(Utils.MockEvent(ev));
            Assert.That(room.JoinRule, Is.EqualTo(EMatrixRoomJoinRules.Public), "The join rule is correct.");
        }

        [Test]
        public void FeedEventRoomMemberTest() {
            var mock = new Mock<MatrixAPI>();
            mock.Setup(f => f.RunningInitialSync).Returns(false);
            MatrixRoom room = new MatrixRoom((MatrixAPI)mock.Object, "!abc:localhost");
            var ev = new MatrixMRoomMember() {
                membership = EMatrixRoomMembership.Join
            };
            room.FeedEvent(Utils.MockEvent(ev, state_key:"@foobar:localhost"));
            Assert.That(room.Members.ContainsKey("@foobar:localhost"), Is.True, "The member is in the room.");
            Assert.That(room.Members.ContainsValue(ev), Is.True, "The member is in the room.");
        }

        [Test]
        public void FeedEventRoomMemberNoFireEventsTest() {
            var mock = new Mock<MatrixAPI>();
            mock.Setup(f => f.RunningInitialSync).Returns(true);
            MatrixRoom room = new MatrixRoom((MatrixAPI)mock.Object, "!abc:localhost");
            var ev = new MatrixMRoomMember() {
                membership = EMatrixRoomMembership.Join
            };
            bool did_fire = false;
            room.OnUserJoined += (n, a) => did_fire = true;
            room.FeedEvent(Utils.MockEvent(ev, state_key:"@foobar:localhost"));
            Assert.That(did_fire, Is.False);
            Assert.That(room.Members.ContainsKey("@foobar:localhost"), Is.True, "The member is in the room.");
            Assert.That(room.Members.ContainsValue(ev), Is.True, "The member is in the room.");
        }

        [Test]
        public void FeedEventRoomMemberFireEventsTest() {
            var mock = new Mock<MatrixAPI>();
            mock.Setup(f => f.RunningInitialSync).Returns(false);
            MatrixRoom room = new MatrixRoom((MatrixAPI)mock.Object, "!abc:localhost");
            bool[] did_fire = new bool[5];
            int fire_count = 0;
            room.OnUserJoined += (n, a) => did_fire[0] = true;
            room.OnUserChange += (n, a) => did_fire[1] = true;
            room.OnUserLeft += (n, a) => did_fire[2] = true;
            room.OnUserInvited += (n, a) => did_fire[3] = true;
            room.OnUserBanned += (n, a) => did_fire[4] = true;
            room.OnEvent += (n,a) => fire_count++;

            var ev = new MatrixMRoomMember() {
                membership = EMatrixRoomMembership.Join
            };
            room.FeedEvent(Utils.MockEvent(ev, state_key:"@foobar:localhost"));
            
            Assert.That(did_fire[0], Is.True, "Procesed join");
            ev = new MatrixMRoomMember() {
                membership = EMatrixRoomMembership.Join,
                displayname = "Foobar!",
            };
            room.FeedEvent(Utils.MockEvent(ev, state_key:"@foobar:localhost"));
            
            Assert.That(did_fire[1], Is.True, "Processed change");
            ev = new MatrixMRoomMember() {
                membership = EMatrixRoomMembership.Leave,
            };
            room.FeedEvent(Utils.MockEvent(ev, state_key:"@foobar:localhost"));
            Assert.That(did_fire[2], Is.True, "Processed leave");

            ev = new MatrixMRoomMember() {
                membership = EMatrixRoomMembership.Invite,
            };
            room.FeedEvent(Utils.MockEvent(ev, state_key:"@foobar:localhost"));
            Assert.That(did_fire[3], Is.True, "Processed invite");

            ev = new MatrixMRoomMember() {
                membership = EMatrixRoomMembership.Ban,
            };
            room.FeedEvent(Utils.MockEvent(ev, state_key:"@foobar:localhost"));
            Assert.That(did_fire[4], Is.True, "Processed ban");
            Assert.That(fire_count, Is.EqualTo(5), "OnEvent should fire each time.");
        }

        [Test]
        public void FeedEventRoomMessageTest() {
            int fire_count = 0;
            bool did_fire = false;
            MatrixRoom room = new MatrixRoom(null, "!abc:localhost");
            room.OnMessage += (n,a) => did_fire = true;
            room.OnEvent += (n,a) => fire_count++;
            // NoAgeRestriction
            room.MessageMaximumAge = 0;
            var ev = new MatrixMRoomMessage();
            room.FeedEvent(Utils.MockEvent(ev, age: 5000));
            Assert.That(did_fire, Is.True, "Message without age limit.");
            // AgeRestriction, Below Limit
            room.MessageMaximumAge = 5000;
            did_fire = false;
            room.FeedEvent(Utils.MockEvent(ev, age: 2500));
            Assert.That(did_fire, Is.True, "Message below age limit.");
            // AgeRestriction, Above Limit
            did_fire = false;
            room.FeedEvent(Utils.MockEvent(ev, age: 5001));
            Assert.That(did_fire, Is.False, "Message above age limit.");
            //Test Subclass
            did_fire = false;
            ev = new MMessageText();
            room.FeedEvent(Utils.MockEvent(ev));
            Assert.That(did_fire, Is.True, "Subclassed message accepted.");
            // OnEvent should fire each time
            Assert.That(fire_count, Is.EqualTo(4));
        }
    }   
}
