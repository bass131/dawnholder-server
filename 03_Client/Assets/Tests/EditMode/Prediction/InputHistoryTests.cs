using System.Collections.Generic;
using System.Linq;
using Dawnholder.Client.Prediction;
using NUnit.Framework;

namespace Dawnholder.Client.Tests.Prediction
{
    // Phase 06 (M2) Step 3: InputHistory 단위 테스트.
    //
    // **본 테스트의 목적** (Phase 06 정의 파일 #59~61):
    //   - "push N개 → ack k → 큐 길이 N-k 검증"
    //   - 메모리 위생 (ack로 정리됨 확인)
    //   - replay 순서 보존
    //
    // **Codex 5번째 규칙**: EditMode NUnit 테스트는 순수 자료구조 검증만. Unity 의존 0.
    public class InputHistoryTests
    {
        [Test]
        public void NewHistory_HasZeroCount()
        {
            var history = new InputHistory();

            Assert.AreEqual(0, history.Count);
        }

        [Test]
        public void Push_IncrementsCount()
        {
            var history = new InputHistory();

            history.Push(1, 1);
            history.Push(2, -1);
            history.Push(3, 0);

            Assert.AreEqual(3, history.Count);
        }

        [Test]
        public void EvictUpTo_RemovesAckedRecords_KeepsUnacked()
        {
            // Phase 06 정의 파일 명세: push N → ack k → 큐 길이 N-k.
            var history = new InputHistory();
            const int N = 10;
            for (uint t = 1; t <= N; t++)
            {
                history.Push(t, 1);
            }

            history.EvictUpTo(4); // ack tick 1, 2, 3, 4 처리 완료

            Assert.AreEqual(N - 4, history.Count, "ack된 입력만큼 큐가 줄어야 함");
        }

        [Test]
        public void EvictUpTo_AllAcked_EmptiesHistory()
        {
            var history = new InputHistory();
            for (uint t = 1; t <= 5; t++)
            {
                history.Push(t, 1);
            }

            history.EvictUpTo(5);

            Assert.AreEqual(0, history.Count);
        }

        [Test]
        public void EvictUpTo_NoMatch_NoOp()
        {
            var history = new InputHistory();
            history.Push(10, 1);
            history.Push(11, -1);

            history.EvictUpTo(5); // 5 이하의 tick 없음

            Assert.AreEqual(2, history.Count);
        }

        [Test]
        public void EvictUpTo_EmptyHistory_DoesNotThrow()
        {
            var history = new InputHistory();

            Assert.DoesNotThrow(() => history.EvictUpTo(100));
            Assert.AreEqual(0, history.Count);
        }

        [Test]
        public void ReplayFrom_ReturnsOnlyUnackedRecords()
        {
            // ackedTick *초과* 입력만 반환. 동일 ackedTick은 제외 (=ack 처리됐다고 봄).
            var history = new InputHistory();
            for (uint t = 1; t <= 10; t++)
            {
                history.Push(t, (sbyte)(t % 2 == 0 ? 1 : -1));
            }

            List<InputRecord> replay = history.ReplayFrom(7).ToList();

            Assert.AreEqual(3, replay.Count, "tick 8, 9, 10 = 3개");
            Assert.AreEqual(8u, replay[0].ClientTick);
            Assert.AreEqual(9u, replay[1].ClientTick);
            Assert.AreEqual(10u, replay[2].ClientTick);
        }

        [Test]
        public void ReplayFrom_PreservesPushOrder()
        {
            var history = new InputHistory();
            history.Push(1, 1);
            history.Push(2, -1);
            history.Push(3, 0);
            history.Push(4, 1);

            List<InputRecord> replay = history.ReplayFrom(0).ToList();

            Assert.AreEqual(4, replay.Count);
            Assert.AreEqual((sbyte)1, replay[0].InputX);
            Assert.AreEqual((sbyte)-1, replay[1].InputX);
            Assert.AreEqual((sbyte)0, replay[2].InputX);
            Assert.AreEqual((sbyte)1, replay[3].InputX);
        }

        [Test]
        public void ReplayFrom_AfterAck_ReturnsEmpty()
        {
            var history = new InputHistory();
            history.Push(1, 1);
            history.Push(2, -1);

            List<InputRecord> replay = history.ReplayFrom(2).ToList();

            Assert.AreEqual(0, replay.Count, "ackedTick 동일 → replay 없음");
        }

        [Test]
        public void PushThenEvictThenReplay_TypicalReconcileFlow()
        {
            // 실전 흐름 시뮬: 10개 push → ack 6까지 → replay 7..10 → evict로 정리
            var history = new InputHistory();
            for (uint t = 1; t <= 10; t++)
            {
                history.Push(t, 1);
            }

            List<InputRecord> replay = history.ReplayFrom(6).ToList();
            history.EvictUpTo(6);

            Assert.AreEqual(4, replay.Count, "tick 7, 8, 9, 10 replay");
            Assert.AreEqual(4, history.Count, "ack 후 큐 = 4 (tick 7..10 남음)");
        }

        [Test]
        public void Push_UintMaxValue_NoOverflow()
        {
            // uint wrap-around 무관 (학습 노트 사실 3-(c): 42억 tick = ~6.8년). 경계 핸들링 검증.
            var history = new InputHistory();
            history.Push(uint.MaxValue - 2, 1);
            history.Push(uint.MaxValue - 1, -1);
            history.Push(uint.MaxValue, 1);

            Assert.AreEqual(3, history.Count);
            List<InputRecord> replay = history.ReplayFrom(uint.MaxValue - 2).ToList();
            Assert.AreEqual(2, replay.Count);
        }

        [Test]
        public void Clear_ResetsHistory()
        {
            var history = new InputHistory();
            history.Push(1, 1);
            history.Push(2, -1);

            history.Clear();

            Assert.AreEqual(0, history.Count);
            Assert.AreEqual(0, history.ReplayFrom(0).Count());
        }
    }
}
