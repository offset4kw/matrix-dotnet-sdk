using System;
using NUnit.Framework;
using Matrix;
using Matrix.Client;
using Matrix.Structures;
using Moq;
namespace Matrix.Tests.Client
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
            var mock = Utils.MockApi();
            MatrixRoom room = new MatrixRoom((MatrixAPI)mock.Object, "!abc:localhost");
            var ev = new MatrixMRoomMember() {
                membership = EMatrixRoomMembership.Join
            };
            room.FeedEvent(Utils.MockEvent(ev, stateKey:"@foobar:localhost"));
            Assert.That(room.Members.ContainsKey("@foobar:localhost"), Is.True, "The member is in the room.");
            Assert.That(room.Members.ContainsValue(ev), Is.True, "The member is in the room.");
        }

        [Test]
        public void FeedEventRoomMemberNoFireEventsTest() {
            var mock = Utils.MockApi();
            MatrixRoom room = new MatrixRoom((MatrixAPI)mock.Object, "!abc:localhost");
            var ev = new MatrixMRoomMember() {
                membership = EMatrixRoomMembership.Join
            };
            mock.Setup(f => f.RunningInitialSync).Returns(true);
            bool didFire = false;
            room.OnUserJoined += (n, a) => didFire = true;
            room.FeedEvent(Utils.MockEvent(ev, stateKey:"@foobar:localhost"));
            Assert.That(didFire, Is.False);
            Assert.That(room.Members.ContainsKey("@foobar:localhost"), Is.True, "The member is in the room.");
            Assert.That(room.Members.ContainsValue(ev), Is.True, "The member is in the room.");
        }

        [Test]
        public void FeedEventRoomMemberFireEventsTest() {
            var mock = Utils.MockApi();
            MatrixRoom room = new MatrixRoom((MatrixAPI)mock.Object, "!abc:localhost");
            bool[] didFire = new bool[5];
            int fireCount = 0;
            room.OnUserJoined += (n, a) => didFire[0] = true;
            room.OnUserChange += (n, a) => didFire[1] = true;
            room.OnUserLeft += (n, a) => didFire[2] = true;
            room.OnUserInvited += (n, a) => didFire[3] = true;
            room.OnUserBanned += (n, a) => didFire[4] = true;
            room.OnEvent += (n,a) => fireCount++;

            var ev = new MatrixMRoomMember() {
                membership = EMatrixRoomMembership.Join
            };
            room.FeedEvent(Utils.MockEvent(ev, stateKey:"@foobar:localhost"));
            
            Assert.That(didFire[0], Is.True, "Procesed join");
            ev = new MatrixMRoomMember() {
                membership = EMatrixRoomMembership.Join,
                displayname = "Foobar!",
            };
            room.FeedEvent(Utils.MockEvent(ev, stateKey:"@foobar:localhost"));
            
            Assert.That(didFire[1], Is.True, "Processed change");
            ev = new MatrixMRoomMember() {
                membership = EMatrixRoomMembership.Leave,
            };
            room.FeedEvent(Utils.MockEvent(ev, stateKey:"@foobar:localhost"));
            Assert.That(didFire[2], Is.True, "Processed leave");

            ev = new MatrixMRoomMember() {
                membership = EMatrixRoomMembership.Invite,
            };
            room.FeedEvent(Utils.MockEvent(ev, stateKey:"@foobar:localhost"));
            Assert.That(didFire[3], Is.True, "Processed invite");

            ev = new MatrixMRoomMember() {
                membership = EMatrixRoomMembership.Ban,
            };
            room.FeedEvent(Utils.MockEvent(ev, stateKey:"@foobar:localhost"));
            Assert.That(didFire[4], Is.True, "Processed ban");
            Assert.That(fireCount, Is.EqualTo(5), "OnEvent should fire each time.");
        }

        [Test]
        public void FeedEventRoomMessageTest() {
            int fireCount = 0;
            bool didFire = false;
            MatrixRoom room = new MatrixRoom(null, "!abc:localhost");
            room.OnMessage += (n,a) => didFire = true;
            room.OnEvent += (n,a) => fireCount++;
            // NoAgeRestriction
            room.MessageMaximumAge = 0;
            var ev = new MatrixMRoomMessage();
            room.FeedEvent(Utils.MockEvent(ev, age: 5000));
            Assert.That(didFire, Is.True, "Message without age limit.");
            // AgeRestriction, Below Limit
            room.MessageMaximumAge = 5000;
            didFire = false;
            room.FeedEvent(Utils.MockEvent(ev, age: 2500));
            Assert.That(didFire, Is.True, "Message below age limit.");
            // AgeRestriction, Above Limit
            didFire = false;
            room.FeedEvent(Utils.MockEvent(ev, age: 5001));
            Assert.That(didFire, Is.False, "Message above age limit.");
            //Test Subclass
            didFire = false;
            ev = new MMessageText();
            room.FeedEvent(Utils.MockEvent(ev));
            Assert.That(didFire, Is.True, "Subclassed message accepted.");
            // OnEvent should fire each time
            Assert.That(fireCount, Is.EqualTo(4));
        }

        [Test]
        public void SetMemberDisplayNameNoMemberTest() {
            var mock = Utils.MockApi();
            MatrixRoom room = new MatrixRoom(mock.Object, "!abc:localhost");
            Assert.That(
                () => room.SetMemberDisplayName("@foobar:localhost"),
                Throws.TypeOf<MatrixException>()
                .With.Property("Message").EqualTo("Couldn't find the user's membership event")
            );        }

        [Test]
        public void SetMemberAvatarNoMemberTest() {
            var mock = Utils.MockApi();
            MatrixRoom room = new MatrixRoom(mock.Object, "!abc:localhost");
            Assert.That(
                () => room.SetMemberAvatar("@foobar:localhost"),
                Throws.TypeOf<MatrixException>()
                .With.Property("Message").EqualTo("Couldn't find the user's membership event")
            );
        }

        [Test]
        public void SetMemberDisplayNameTest() {
            var mock = Utils.MockApi();
            MatrixRoom room = new MatrixRoom(mock.Object, "!abc:localhost");
            var ev = new MatrixMRoomMember() {
                membership = EMatrixRoomMembership.Join,
            };
            room.FeedEvent(Utils.MockEvent(ev, "@foobar:localhost"));
            room.SetMemberDisplayName("@foobar:localhost");
        }

        [Test]
        public void SetMemberAvatarTest() {
            var mock = Utils.MockApi();
            MatrixRoom room = new MatrixRoom(mock.Object, "!abc:localhost");
            var ev = new MatrixMRoomMember() {
                membership = EMatrixRoomMembership.Join,
            };
            room.FeedEvent(Utils.MockEvent(ev, "@foobar:localhost"));
            room.SetMemberAvatar("@foobar:localhost");
        }
    }   
}
