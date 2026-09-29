using System;
using System.Collections.Generic;
using Dawnholder.Client.UI;
using NUnit.Framework;

namespace Dawnholder.Client.Tests
{
    public sealed class SceneTransitionRequestTests
    {
        [Test]
        public void ActiveAThenBThenC_KeepsPhysicalSlot_AndStartsOnlyLatestPending()
        {
            var queue = new SceneLoadQueue();
            var results = new List<SceneLoadResult>();
            var a = queue.Request("Town", results.Add);
            Assert.IsTrue(queue.TryStartNext(out var startedA));
            Assert.AreSame(a, startedA);
            var b = queue.Request("HuntingGround", results.Add);
            var c = queue.Request("BossRoom", results.Add);
            Assert.AreSame(a, queue.Active);
            Assert.AreSame(c, queue.Pending);
            Assert.IsFalse(queue.TryStartNext(out _), "logical supersession cannot cancel Unity's physical operation");
            Assert.AreEqual(2, results.Count);
            AssertResult(results[0], a, SceneLoadStatus.Superseded);
            AssertResult(results[1], b, SceneLoadStatus.Superseded);
            Assert.IsTrue(queue.Complete(a.Id, SceneLoadStatus.Completed, 10));
            Assert.AreEqual(2, results.Count, "superseded A must not also publish domain success");
            Assert.IsTrue(queue.TryStartNext(out var startedC));
            Assert.AreSame(c, startedC);
            Assert.IsTrue(queue.Complete(c.Id, SceneLoadStatus.Completed, 30));
            Assert.AreEqual(3, results.Count);
            AssertResult(results[2], c, SceneLoadStatus.Completed, 30);
            Assert.IsNull(queue.Active);
            Assert.IsNull(queue.Pending);
            Assert.IsFalse(queue.TryStartNext(out _));
        }

        [Test]
        public void BeforePhysicalStart_NewRequestReplacesPendingWithoutLoadingOldScene()
        {
            var queue = new SceneLoadQueue();
            var results = new List<SceneLoadResult>();
            var first = queue.Request("Town", results.Add);
            var second = queue.Request("HuntingGround", results.Add);
            Assert.IsNull(queue.Active);
            Assert.IsTrue(first.IsFinished);
            AssertResult(results[0], first, SceneLoadStatus.Superseded);
            Assert.IsTrue(queue.TryStartNext(out var started));
            Assert.AreSame(second, started);
            Assert.IsFalse(queue.Complete(first.Id, SceneLoadStatus.Completed, 1));
            Assert.AreSame(second, queue.Active);
        }

        [Test]
        public void SameSceneRequests_HaveDistinctIds_AndOldOrDuplicateCompletionCannotFinishNewRequest()
        {
            var queue = new SceneLoadQueue();
            var results = new List<SceneLoadResult>();
            var first = queue.Request("Town", results.Add);
            queue.TryStartNext(out _);
            var second = queue.Request("Town", results.Add);
            Assert.AreNotEqual(first.Id, second.Id);
            queue.Complete(first.Id, SceneLoadStatus.Completed, 1);
            queue.TryStartNext(out _);
            Assert.IsFalse(queue.Complete(first.Id, SceneLoadStatus.Completed, 1));
            Assert.IsFalse(queue.Complete(second.Id + 100, SceneLoadStatus.Failed));
            Assert.AreSame(second, queue.Active);
            Assert.IsFalse(second.IsFinished);
            Assert.IsTrue(queue.Complete(second.Id, SceneLoadStatus.Completed, 2));
            Assert.IsFalse(queue.Complete(second.Id, SceneLoadStatus.Completed, 2));
            Assert.AreEqual(2, results.Count);
            AssertResult(results[0], first, SceneLoadStatus.Superseded);
            AssertResult(results[1], second, SceneLoadStatus.Completed, 2);
        }

        [Test]
        public void CancelPending_RemovesIt_AndLatePhysicalReportHasNoEffect()
        {
            var queue = new SceneLoadQueue();
            var results = new List<SceneLoadResult>();
            var request = queue.Request("Town", results.Add);
            Assert.IsTrue(queue.Cancel(request.Id));
            Assert.IsFalse(queue.Cancel(request.Id));
            Assert.IsFalse(queue.Cancel(request.Id + 100));
            Assert.IsFalse(queue.Complete(request.Id, SceneLoadStatus.Completed, 10));
            Assert.IsFalse(queue.TryStartNext(out _));
            Assert.AreEqual(1, results.Count);
            AssertResult(results[0], request, SceneLoadStatus.Canceled);
        }

        [TestCase(SceneLoadStatus.Completed)]
        [TestCase(SceneLoadStatus.Failed)]
        public void CancelActive_BlocksNextUntilPhysicalCompletion_AndNeverReplaysDomainResult(SceneLoadStatus physicalResult)
        {
            var queue = new SceneLoadQueue();
            var results = new List<SceneLoadResult>();
            var first = queue.Request("Town", results.Add);
            queue.TryStartNext(out _);
            Assert.IsTrue(queue.Cancel(first.Id));
            queue.Cancel(first.Id);
            var next = queue.Request("MainMenu", results.Add);
            Assert.IsFalse(queue.TryStartNext(out _));
            Assert.AreSame(first, queue.Active);
            Assert.AreEqual(1, results.Count);
            AssertResult(results[0], first, SceneLoadStatus.Canceled);
            Assert.IsTrue(queue.Complete(first.Id, physicalResult));
            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(queue.TryStartNext(out var started));
            Assert.AreSame(next, started);
        }

        [Test]
        public void CancelAll_DiscardsPending_ButRetainsActiveUntilActualCompletion()
        {
            var queue = new SceneLoadQueue();
            var results = new List<SceneLoadResult>();
            var active = queue.Request("Town", results.Add);
            queue.TryStartNext(out _);
            var pending = queue.Request("HuntingGround", results.Add);
            queue.CancelAll();
            queue.CancelAll();
            Assert.IsNull(queue.Pending);
            Assert.AreSame(active, queue.Active);
            Assert.AreEqual(2, results.Count);
            AssertResult(results[0], active, SceneLoadStatus.Superseded);
            AssertResult(results[1], pending, SceneLoadStatus.Canceled);
            queue.Complete(active.Id, SceneLoadStatus.Completed, 1);
            Assert.IsNull(queue.Active);
            Assert.IsFalse(queue.TryStartNext(out _));
            Assert.AreEqual(2, results.Count);
        }

        // The adapter owns LoadSceneAsync/null/throw detection. The queue receives
        // that failure and must free its slot without turning it into a success.
        [Test]
        public void PhysicalFailure_PreservesErrorOnce_AndAllowsNextRequest()
        {
            var queue = new SceneLoadQueue();
            var results = new List<SceneLoadResult>();
            var failed = queue.Request("MissingScene", results.Add);
            queue.TryStartNext(out _);
            var error = new InvalidOperationException("loader returned no operation");
            Assert.IsTrue(queue.Complete(failed.Id, SceneLoadStatus.Failed, error: error));
            Assert.IsFalse(queue.Complete(failed.Id, SceneLoadStatus.Completed, 5));
            Assert.AreEqual(1, results.Count);
            AssertResult(results[0], failed, SceneLoadStatus.Failed);
            Assert.AreSame(error, results[0].Error);
            var next = queue.Request("Town");
            Assert.IsTrue(queue.TryStartNext(out var started));
            Assert.AreSame(next, started);
        }

        [TestCase(SceneLoadStatus.Completed)]
        [TestCase(SceneLoadStatus.Failed)]
        [TestCase(SceneLoadStatus.Canceled)]
        [TestCase(SceneLoadStatus.Superseded)]
        public void CompletionObservers_BeforeAndAfterTerminalResult_SeeSameResultExactlyOnce(SceneLoadStatus status)
        {
            var queue = new SceneLoadQueue();
            var early = new List<SceneLoadResult>();
            var late = new List<SceneLoadResult>();
            var request = queue.Request("Town");
            request.ObserveCompletion(early.Add);
            queue.TryStartNext(out _);
            if (status == SceneLoadStatus.Canceled) queue.Cancel(request.Id);
            else if (status == SceneLoadStatus.Superseded) queue.Request("HuntingGround");
            else queue.Complete(request.Id, status);
            request.ObserveCompletion(late.Add);
            queue.Complete(request.Id, SceneLoadStatus.Completed);
            Assert.AreEqual(1, early.Count);
            Assert.AreEqual(1, late.Count);
            Assert.AreSame(early[0], late[0]);
            AssertResult(early[0], request, status);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void InvalidSceneName_DoesNotSupersedeAnAcceptedRequest(string sceneName)
        {
            var queue = new SceneLoadQueue();
            var existing = queue.Request("Town");
            Assert.Throws<ArgumentException>(() => queue.Request(sceneName));
            Assert.AreSame(existing, queue.Pending);
            Assert.IsFalse(existing.IsFinished);
        }

        [Test]
        public void SupersededPendingCallbackStartsReplacement_ReplacementStillCompletesNormally()
        {
            var queue = new SceneLoadQueue();
            var oldResults = new List<SceneLoadResult>();
            var currentResults = new List<SceneLoadResult>();
            SceneLoadRequest startedFromCallback = null;
            bool started = false;
            var previous = queue.Request("Town", result =>
            {
                oldResults.Add(result);
                started = queue.TryStartNext(out startedFromCallback);
            });
            var current = queue.Request("HuntingGround", currentResults.Add);
            Assert.IsTrue(started);
            Assert.AreSame(current, startedFromCallback);
            Assert.AreSame(current, queue.Active);
            Assert.IsFalse(current.IsFinished, "the new physical operation cannot supersede itself");
            Assert.IsEmpty(currentResults);
            AssertResult(oldResults[0], previous, SceneLoadStatus.Superseded);
            Assert.IsTrue(queue.Complete(current.Id, SceneLoadStatus.Completed, 27));
            Assert.AreEqual(1, currentResults.Count);
            AssertResult(currentResults[0], current, SceneLoadStatus.Completed, 27);
        }

        [TestCase(false, SceneLoadStatus.Completed)]
        [TestCase(true, SceneLoadStatus.Completed)]
        [TestCase(true, SceneLoadStatus.Failed)]
        public void ObserverThrows_LaterObserverReceivesOriginalResult_AndNextLoadCanStart(bool throwFirst, SceneLoadStatus status)
        {
            var queue = new SceneLoadQueue();
            var observerError = new InvalidOperationException("observer failed");
            var operationError = status == SceneLoadStatus.Failed ? new Exception("physical load failed") : null;
            var reports = new List<Exception>();
            var results = new List<SceneLoadResult>();
            queue.ObserverFailed += reports.Add;
            int firstCalls = 0;
            var request = queue.Request("Town", _ =>
            {
                firstCalls++;
                if (throwFirst) throw observerError;
            });
            SceneLoadRequest next = null;
            SceneLoadRequest started = null;
            bool couldStart = false;
            request.ObserveCompletion(result =>
            {
                results.Add(result);
                next = queue.Request("HuntingGround");
                couldStart = queue.TryStartNext(out started);
            });
            queue.TryStartNext(out _);
            Assert.DoesNotThrow(() => queue.Complete(request.Id, status, error: operationError));
            Assert.AreEqual(1, firstCalls);
            Assert.AreEqual(1, results.Count, "one observer cannot suppress another subscriber");
            AssertResult(results[0], request, status);
            Assert.AreSame(operationError, results[0].Error, "observer failure cannot change the physical result");
            Assert.IsTrue(couldStart);
            Assert.AreSame(next, started);
            Assert.AreSame(next, queue.Active);
            Assert.IsFalse(queue.Complete(request.Id, SceneLoadStatus.Completed));
            Assert.IsTrue(queue.Complete(next.Id, SceneLoadStatus.Completed, 6));
            if (throwFirst)
            {
                CollectionAssert.AreEqual(new[] { observerError }, reports);
                Assert.AreSame(observerError, queue.LastObserverError);
            }
            else
            {
                Assert.IsEmpty(reports);
                Assert.IsNull(queue.LastObserverError);
            }
        }

        [Test]
        public void SupersessionObserverThrows_RequestStillPublishesNewPending_AndAllSubscribersAreNotified()
        {
            var queue = new SceneLoadQueue();
            var error = new Exception("supersession observer failed");
            var observed = new List<SceneLoadResult>();
            var old = queue.Request("Town", _ => throw error);
            old.ObserveCompletion(observed.Add);
            queue.TryStartNext(out _);
            SceneLoadRequest next = null;
            Assert.DoesNotThrow(() => next = queue.Request("HuntingGround"));
            Assert.AreEqual(1, observed.Count);
            AssertResult(observed[0], old, SceneLoadStatus.Superseded);
            Assert.AreSame(error, queue.LastObserverError);
            Assert.AreSame(next, queue.Pending);
            Assert.AreSame(old, queue.Active);
            queue.Complete(old.Id, SceneLoadStatus.Completed);
            Assert.IsTrue(queue.TryStartNext(out var started));
            Assert.AreSame(next, started);
        }

        [Test]
        public void LateObserverThrows_TerminalResultRemainsAvailableToLaterObservers()
        {
            var queue = new SceneLoadQueue();
            var request = queue.Request("Town");
            queue.TryStartNext(out _);
            queue.Complete(request.Id, SceneLoadStatus.Completed, 4);
            var error = new Exception("late observer failed");
            Assert.DoesNotThrow(() => request.ObserveCompletion(_ => throw error));
            var results = new List<SceneLoadResult>();
            request.ObserveCompletion(results.Add);
            Assert.AreEqual(1, results.Count);
            AssertResult(results[0], request, SceneLoadStatus.Completed, 4);
            Assert.IsNull(results[0].Error);
            Assert.AreSame(error, queue.LastObserverError);
        }

        [Test]
        public void ErrorReporterAlsoThrows_StillNotifiesRemainingReportersAndCompletionObservers()
        {
            var queue = new SceneLoadQueue();
            var originalError = new Exception("completion observer failed");
            var reporterError = new Exception("error reporter failed");
            var reported = new List<Exception>();
            queue.ObserverFailed += _ => throw reporterError;
            queue.ObserverFailed += reported.Add;
            var results = new List<SceneLoadResult>();
            var request = queue.Request("Town", _ => throw originalError);
            request.ObserveCompletion(results.Add);
            queue.TryStartNext(out _);
            Assert.DoesNotThrow(() => queue.Complete(request.Id, SceneLoadStatus.Completed, 7));
            CollectionAssert.AreEqual(new[] { originalError }, reported);
            Assert.AreEqual(1, results.Count);
            AssertResult(results[0], request, SceneLoadStatus.Completed, 7);
            Assert.IsNull(results[0].Error);
            Assert.IsInstanceOf<AggregateException>(queue.LastObserverError);
            var errors = ((AggregateException)queue.LastObserverError).Flatten().InnerExceptions;
            CollectionAssert.Contains(errors, originalError);
            CollectionAssert.Contains(errors, reporterError);
            Assert.IsNull(queue.Active);
        }

        [Test]
        public void SceneIdentity_CompletionAndLateObserverPreserveAll64Bits()
        {
            var queue = new SceneLoadQueue();
            var results = new List<SceneLoadResult>();
            const ulong firstHandle = (1UL << 40) | 27;
            const ulong nextHandle = (2UL << 40) | 27;
            var first = queue.Request("Town", results.Add);
            queue.TryStartNext(out _);
            queue.Complete(first.Id, SceneLoadStatus.Completed, firstHandle);
            first.ObserveCompletion(result => results.Add(result));
            var next = queue.Request("Town", results.Add);
            queue.TryStartNext(out _);
            queue.Complete(next.Id, SceneLoadStatus.Completed, nextHandle);
            Assert.AreEqual(firstHandle, results[0].SceneHandle);
            Assert.AreEqual(firstHandle, results[1].SceneHandle);
            Assert.AreEqual(nextHandle, results[2].SceneHandle);
            Assert.AreNotEqual(results[0].SceneHandle, results[2].SceneHandle);
        }

        static void AssertResult(SceneLoadResult result, SceneLoadRequest request, SceneLoadStatus status, ulong handle = 0)
        {
            Assert.AreEqual(request.Id, result.RequestId);
            Assert.AreEqual(status, result.Status);
            Assert.AreEqual(handle, result.SceneHandle);
            Assert.IsTrue(request.IsFinished);
        }
    }
}
