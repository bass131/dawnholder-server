using Dawnholder.Client.Network;
using NUnit.Framework;
using Shared.GameData;

namespace Dawnholder.Client.Tests
{
    // Goal T10 (completion condition 8): after the handshake the client logs in first and selects a class only
    // after an Accepted result; a rejection raises LoginRejected and closes without a class select.
    public sealed class ClientLoginLifetimeTests
    {
        // T10: handshake success alone sends no C_CharacterSelect; the connection stays up for the login result.
        [Test]
        public void T10_HandshakeSuccess_SendsNoCharacterSelectBeforeLogin()
        {
            using var h = new LoginConnectionHarness();
            LoginRecordingSession session = h.Connect(selectedClass: 1);

            session.Handshake();
            h.Queue.Drain();

            CollectionAssert.IsEmpty(session.CharacterSelects, "a class was selected before any login result");
            Assert.IsTrue(h.Owner.IsConnected);
        }

        // T10: the login goes out once per connection, with the name given to Connect.
        [Test]
        public void T10_HandshakeSuccess_SendsLoginOnceWithTheConnectName()
        {
            using var h = new LoginConnectionHarness();
            LoginRecordingSession session = h.Connect(selectedClass: 1);

            session.Handshake();
            session.Handshake();
            h.Queue.Drain();

            CollectionAssert.AreEqual(new[] { LoginConnectionHarness.AccountName }, session.Logins);
        }

        // T10: an Accepted result sends the class captured at Connect once; a repeated result does not resend it.
        [Test]
        public void T10_AcceptedLogin_SendsCharacterSelectOnce()
        {
            using var h = new LoginConnectionHarness();
            LoginRecordingSession session = h.Connect(selectedClass: 1);
            session.Handshake();

            session.DeliverLoginResult(LoginResult.Accepted);
            session.DeliverLoginResult(LoginResult.Accepted);
            h.Queue.Drain();

            CollectionAssert.AreEqual(new byte[] { 1 }, session.CharacterSelects);
            CollectionAssert.IsEmpty(h.Rejections);
            Assert.IsTrue(h.Owner.IsConnected);
        }

        // T10: InvalidName or AlreadyOnline raises LoginRejected with that result, closes the connection and never
        // selects a class.
        [TestCase(LoginResult.InvalidName)]
        [TestCase(LoginResult.AlreadyOnline)]
        public void T10_RejectedLogin_RaisesLoginRejectedAndDisconnectsWithoutCharacterSelect(LoginResult result)
        {
            using var h = new LoginConnectionHarness();
            LoginRecordingSession session = h.Connect(selectedClass: 1);
            session.Handshake();

            session.DeliverLoginResult(result);
            h.Queue.Drain();

            CollectionAssert.AreEqual(new[] { result }, h.Rejections);
            CollectionAssert.IsEmpty(session.CharacterSelects);
            Assert.AreEqual(1, session.Disconnects);
            Assert.AreEqual(ClientConnectionState.Disconnected, h.Owner.State);
            Assert.IsNull(h.Owner.CurrentSession);
        }

        // T10: after a rejection the next explicit connection starts again from the login.
        [Test]
        public void T10_NewConnectionAfterRejection_LogsInAgainBeforeSelecting()
        {
            using var h = new LoginConnectionHarness();
            LoginRecordingSession rejected = h.Connect(selectedClass: 0);
            rejected.Handshake();
            rejected.DeliverLoginResult(LoginResult.AlreadyOnline);
            h.Queue.Drain();

            LoginRecordingSession next = h.Connect(selectedClass: 0);
            next.Handshake();
            h.Queue.Drain();

            CollectionAssert.AreEqual(new[] { LoginConnectionHarness.AccountName }, next.Logins);
            CollectionAssert.IsEmpty(next.CharacterSelects);
            next.DeliverLoginResult(LoginResult.Accepted);
            h.Queue.Drain();
            CollectionAssert.AreEqual(new byte[] { 0 }, next.CharacterSelects);
        }
    }
}
