#nullable enable
using System.Collections.Generic;

namespace Dawnholder.Client.Prediction
{
    // Input replay reconcile의 자료구조: 클라가 보낸 입력을 시간순 보관, snapshot의 ackedTick
    // 기준으로 미-ack 입력을 골라 replay에 넘김.
    //
    // **Invariant** (호출자 책임, 내부 검증 X):
    //   - Push의 clientTick은 strictly increasing. 송신 throttle이 50ms 간격으로
    //     보장 → 같거나 작은 tick으로 다시 들어올 일 없음.
    //   - 동시 접근 X. 메인 스레드에서만 사용 (UnityClientSession이 MainThreadDispatcher로
    //     마샬링 후 호출).
    public sealed class InputHistory
    {
        private readonly List<InputRecord> _records = new List<InputRecord>(128);

        public int Count => _records.Count;

        // 새 입력 push. clientTick은 strictly increasing 가정 (Invariant 참조).
        // 기존 3-arg 시그니처 — externalVelX=0 위임 (임펄스 없는 평지 입력).
        public void Push(uint clientTick, sbyte inputX, bool jumpPressed)
        {
            _records.Add(new InputRecord(clientTick, inputX, jumpPressed));
        }

        // 임펄스(대쉬/lunge) 활성 틱용 4-arg. externalVelX = *그 서브스텝 live Predict가 실제 쓴 vx*
        // (재계산 금지 — 하이브리드 함정 방지). replay가 이 저장값을 그대로 PhysicsInput에 재생.
        public void Push(uint clientTick, sbyte inputX, bool jumpPressed, float externalVelX)
        {
            _records.Add(new InputRecord(clientTick, inputX, jumpPressed, externalVelX));
        }

        // ackedTick 이하의 입력을 모두 제거 (서버가 처리 완료한 입력은 더 이상 replay 대상 X).
        //
        // **함정**: ack 받기 *전*에 비면 위험. 정리는 항상 snapshot 도착 후,
        //   ackedTick 정확히 받은 시점에만.
        public void EvictUpTo(uint ackedTick)
        {
            // 시간순 보관 + monotonic invariant 덕에 prefix만 잘라내면 됨.
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
        // reconcile: "서버 위치에서 출발해 미-ack 입력만 재실행".
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

    public readonly struct InputRecord
    {
        public readonly uint ClientTick;
        public readonly sbyte InputX;
        public readonly bool JumpPressed;

        // 그 틱 live Predict가 Physics.Step ExternalVelX로 실제 주입한 임펄스 vx.
        // replay는 이 저장값을 4-arg PhysicsInput으로 그대로 재생 → 재계산 X (결정성 뿌리).
        // 임펄스 없는 평지 입력은 0 (기존 3-arg ctor 위임).
        public readonly float ExternalVelX;

        // 기존 3-arg ctor — ExternalVelX=0 위임. 기존 호출자 전부 불변.
        public InputRecord(uint clientTick, sbyte inputX, bool jumpPressed)
            : this(clientTick, inputX, jumpPressed, 0f) { }

        public InputRecord(uint clientTick, sbyte inputX, bool jumpPressed, float externalVelX)
        {
            ClientTick = clientTick;
            InputX = inputX;
            JumpPressed = jumpPressed;
            ExternalVelX = externalVelX;
        }
    }
}
