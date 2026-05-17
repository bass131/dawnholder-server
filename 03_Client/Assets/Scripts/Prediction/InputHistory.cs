#nullable enable
using System.Collections.Generic;

namespace Dawnholder.Client.Prediction
{
    // Phase 06 (M2): Input replay reconcile의 자료구조 기둥.
    //
    // **역할**: 클라가 보낸 입력 (clientTick, inputX) 페어를 시간순 보관. snapshot 도착 시
    //         "서버가 어디까지 ack 했는지"를 기준으로 미-ack 입력을 골라 replay에 넘김.
    //
    // **Codex 시니어 가이드 (2026-05-16 상의)**:
    //   - UnityEngine 의존 없는 순수 C# (sealed class, MonoBehaviour 금지)
    //   - 좁은 API — Push / EvictUpTo / ReplayFrom / Count 만
    //   - tick 비교 규칙은 Phase 06 uint 결정과 일관
    //   - 미래 재사용 신호 오면 04_ClientNet 이주가 아니라 별도 SharedPrediction dll 추출
    //     (위 신호 5개는 learning-journal/concepts/int-vs-uint-for-tick-counters.md 옆에 박힐 예정)
    //
    // **Invariant** (호출자 책임, 내부 검증 X):
    //   - Push의 clientTick은 strictly increasing. Step 4 송신 throttle이 50ms 간격으로
    //     보장 → 같거나 작은 tick으로 다시 들어올 일 없음.
    //   - 동시 접근 X. 메인 스레드에서만 사용 (UnityClientSession이 MainThreadDispatcher로
    //     마샬링 후 호출).
    //
    // **uint tick wrap-around 무관** (Phase 06 학습 노트 사실 3-(c) 참조):
    //   42억 tick × 50ms = ~6.8년 연속 실행. 실질 영향 0.
    public sealed class InputHistory
    {
        // 초기 용량 = 128. 5초(=100 tick) 보관 + 여유. 동적 확장 가능.
        private readonly List<InputRecord> _records = new List<InputRecord>(128);

        public int Count => _records.Count;

        // 새 입력 push. clientTick은 strictly increasing 가정 (Invariant 참조).
        // Phase 07: jumpPressed (D4 (a) 클라 에지 — 1tick만 true) 추가 — replay 시 점프 시점 재현.
        public void Push(uint clientTick, sbyte inputX, bool jumpPressed)
        {
            _records.Add(new InputRecord(clientTick, inputX, jumpPressed));
        }

        // ackedTick 이하의 입력을 모두 제거 (서버가 처리 완료한 입력은 더 이상 replay 대상 X).
        //
        // **함정** (Phase 06 정의 파일 #79~85): ack 받기 *전*에 비면 위험.
        //   정리는 항상 snapshot 도착 후, ackedTick 정확히 받은 시점에만.
        public void EvictUpTo(uint ackedTick)
        {
            // 시간순 보관 + monotonic invariant 덕에 prefix만 잘라내면 됨.
            // List<>.RemoveRange는 O(n) — 100 records 미만이라 무시 가능.
            int firstUnacked = 0;
            while (firstUnacked < _records.Count && _records[firstUnacked].ClientTick <= ackedTick)
            {
                firstUnacked++;
            }
            if (firstUnacked > 0)
            {
                _records.RemoveRange(0, firstUnacked);
            }
        }

        // ackedTick *초과*의 미-ack 입력을 push 순서대로 yield.
        // Phase 06 Step 5 reconcile: "서버 위치에서 출발해 미-ack 입력만 재실행".
        public IEnumerable<InputRecord> ReplayFrom(uint ackedTick)
        {
            for (int i = 0; i < _records.Count; i++)
            {
                if (_records[i].ClientTick > ackedTick)
                {
                    yield return _records[i];
                }
            }
        }

        // 테스트/리셋용. spawn 시점·disconnect 시 재초기화.
        public void Clear()
        {
            _records.Clear();
        }
    }

    // 단일 입력 기록. struct로 잡아 List<> 안에서 stack-friendly.
    // sbyte inputX — InputBits.Decode 결과 (PDL byte input 비트 0~1 디코드 후 값).
    // Phase 07: jumpPressed (PDL byte input 비트 2) 추가 — replay 시 점프 시도도 재현 (단 서버 OnGround
    //          검증으로 공중 점프 무시되는 게 정상 — 클라 reconcile도 같은 결과 보장).
    public readonly struct InputRecord
    {
        public readonly uint ClientTick;
        public readonly sbyte InputX;
        public readonly bool JumpPressed;

        public InputRecord(uint clientTick, sbyte inputX, bool jumpPressed)
        {
            ClientTick = clientTick;
            InputX = inputX;
            JumpPressed = jumpPressed;
        }
    }
}
