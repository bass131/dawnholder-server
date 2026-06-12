#nullable enable
using System;
using Dawnholder.Client.Network;
using Dawnholder.Client.Scenes;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Prediction
{
    // 로컬 플레이어 이동 + 예측 + 서버 응답 흡수 전담 컴포넌트.
    //
    // **흐름**:
    //   - **매 50ms 고정 서브스텝** (accumulator 기반): Predict + C_MoveIntent 송신 + InputHistory push 1:1.
    //     서버 "틱당 입력 1개 소비 → Step 1회" 의 정확한 거울 — dt-drift 0.
    //   - **시각 보간**: substep prev/curr 두 끝점을 lerp → 고fps에서도 부드러운 렌더링.
    //   - **OnJump 에지 검출**: "started" phase만 캡처 → 서브스텝 소비 시 클리어 (0-substep 프레임 유실 방지).
    //
    // **직업 이동값**: PlayerStats.ForClass 단일 출처 (헌법 #4).
    //   PlayerPrefs "SelectedCharacterClass" 읽어 ForClass에 전달.
    public class LocalPlayerMovement : MonoBehaviour
    {
        public static LocalPlayerMovement? Instance { get; private set; }

        PlayerPredictor _predictor = null!; // Awake에서 초기화

        bool _jumpEdgeThisTick; // 다음 서브스텝 소비까지 jump 에지 보관. 소비 시 즉시 클리어.

        uint _localTickCounter; // 송신 일련번호 (송신 시점에만 ++). replay reconcile 기준점.
        float _sendAccumulator; // 고정 서브스텝 accumulator (예측 박자 + 송신 throttle 겸용).

        sbyte _currentMoveX; // LocalPlayerInput이 SetMoveX로 갱신.

        // 시각 보간용 — 매 서브스텝 직전 위치(prev)와 직후 위치(curr).
        // reconcile/teleport 스냅 시 둘 다 새 위치로 리셋해 유령 미끄러짐 방지.
        Vector2 _prevPredictPos;
        Vector2 _currPredictPos;

        // 로컬 공격 commit window 예측 잔여 시간(초). 서브스텝 박자(TickDuration씩) 감쇠.
        // 서버 AttackState(이동 잠금)를 같은 98_Shared 상수로 클라가 선예측 → reconcile rubber-band 0.
        // LocalPlayerMotion이 Attack 선예측 판단에 읽음 (getter로만 노출 — 외부 쓰기 차단).
        float _commitWindowRemaining;

        // LocalPlayerMotion의 Attack 선예측 판단용 — 로컬 타이머 잔여 노출.
        public float CommitWindowRemaining => _commitWindowRemaining;

        // 현재 commit window가 스킬 시전(채널링)인지 평타인지 구분. NotifyChannel→true, NotifyAttack→false.
        // LocalPlayerMotion이 읽어 Attack 스윙 대신 Channeling 모션을 선예측. window 만료 시 자동 해제.
        bool _channelingWindow;
        public bool IsChannelingWindow => _channelingWindow && _commitWindowRemaining > 0f;

        // 공격 쿨다운(서버 rate-limit 거울) 잔여 — 0이면 재공격 가능. commit window(8틱)보다 길다(10틱).
        float _attackCooldownRemaining;
        public bool CanAttack => _attackCooldownRemaining <= 0f;

        // 스킬별 쿨다운(서버 쿨다운 거울) 잔여.
        // 각각 독립 — 한 스킬 쿨다운 중 다른 스킬은 사용 가능.
        float _thunderboltCooldownRemaining;
        float _dashCooldownRemaining;
        float _teleportCooldownRemaining;

        // 하위 호환 프로퍼티 — 기존 Thunderbolt 게이트 코드가 CanUseSkill을 직접 참조.
        public bool CanUseSkill => _thunderboltCooldownRemaining <= 0f;
        public bool CanUseDash => _dashCooldownRemaining <= 0f;
        public bool CanUseTeleport => _teleportCooldownRemaining <= 0f;

        // Teleport: 다음 S_Snapshot 수신 시 보간 없이 즉시 force-adopt 스냅 플래그.
        // SkillCastHandler(Teleport)가 세팅 → OnServerSnapshot에서 소비.
        bool _teleportSnapPending;

        // 텔레포트 도착 이펙트 콜백 — _teleportSnapPending 소비(새 위치 확정) 시 1회 발동 후 null.
        // 다음 시전 시 덮어쓰기 — 스냅샷 미도착 시 pending 영구 잔류해도 무해.
        Action? _teleportArriveCallback;

        // 피격 hit-bridge 게이트 잔여(초). S_EnemyAttack(피격 *즉시* 신호) 도착 시 세팅 →
        // animState==Hit 스냅샷이 도착하기 전 갭 동안 입력을 미리 잠가 onset 당김을 줄인다.
        // 짧게만 — 진짜 hitstun 길이는 서버 전용이라 serverAnimState==Hit가 곧 이어받아 잠금 연장.
        float _hitGateRemaining;

        // hit-bridge 지속(틱). S_EnemyAttack~animState==Hit 스냅샷 사이 갭(≤1스냅샷)을 메우는 *클라 휴리스틱*.
        // 게임플레이 규칙 아님(서버 hitstun과 별개) → 98_Shared 아닌 클라 로컬 상수.
        const int HitGateBridgeTicks = 3; // ~150ms

        // 프레임당 최대 서브스텝 횟수 cap. 초과분은 버리고 다음 reconcile에 위임.
        // spiral of death(긴 프레임 → 다수 substep → 더 긴 프레임) 방지.
        // 4 = 200ms = 4 × TickDuration — 저fps 환경에서도 실용적 상한.
        const int MaxSubstepsPerFrame = 4;

        // 서버 권위 animState 최신값(S_Snapshot). Hit/Death는 클라가 예측 불가 → 이 값으로 게이트.
        // Attack은 로컬 타이머가 선예측하되, 서버 window가 더 길면 이 값이 잠금을 연장(거울 보정).
        AnimState _serverAnimState = AnimState.Idle;

        void Awake()
        {
            Instance = this;

            MoveParams move = ResolveClassMoveParams();
            _predictor = new PlayerPredictor(move);

            // 맵 전환 후 pending spawn 좌표 소비. S_MapTransition 핸들러가 박아둔 spawn 좌표를 읽어 위치 설정.
            //
            // **Awake에서 소비하는 이유 (race 봉합)**:
            //   Start()에서 하면 서버의 첫 S_Snapshot이 Start()보다 먼저 처리되는 race가 생김.
            //   그 순간 predictor가 아직 (0,0)이라 서버 spawn 좌표로 reconcile snap이 발생 →
            //   맵 전환 직후 캐릭터가 튐. Awake는 Instantiate 즉시(같은 프레임) 호출 → 첫 snapshot
            //   처리보다 확실히 먼저 위치를 잡아 snap을 제거.
            if (UnityClientSession.HasPendingSpawn)
            {
                float x = UnityClientSession.PendingSpawnX;
                float y = UnityClientSession.PendingSpawnY;
                int mapId = UnityClientSession.PendingMapId;
                UnityClientSession.ConsumePendingSpawn();

                InjectTerrain(mapId);
                SetServerPosition(new Vector3(x, y, 0f));
                Debug.Log($"[LocalPlayer] spawn 적용: ({x:F2}, {y:F2}) mapId={mapId}");
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // terrain 주입 단일 경로 — Awake pending 소비 + EnterMapHandler instance 분기(ADR-027
        // 첫 진입 두 순서 모두 관측) 둘 다 여기로. fail loud: 파일 부재/CRC 실패 시 예외 전파
        // (이전 맵 terrain으로 예측하는 드리프트보다 시끄러운 실패 우선).
        public void InjectTerrain(int mapId)
        {
            try
            {
                MapTerrain terrain = ClientTerrainStore.Load(mapId);
                _predictor.SetTerrain(terrain);
                Debug.Log($"[LocalPlayer] terrain 주입 mapId={mapId} (solids={terrain.Solids.Length})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalPlayer] terrain 로드 실패 mapId={mapId}: {ex.Message}");
                throw;
            }
        }

        // LocalPlayerInput이 입력 번역 후 호출. sbyte(-1/0/1).
        public void SetMoveX(sbyte x) => _currentMoveX = x;

        // LocalPlayerInput이 점프 입력 시점 접지 게이트 통과 후 호출.
        // 다음 서브스텝이 소비할 때까지 latch 유지 — 0-substep 프레임에서 유실 방지.
        public void RequestJump() => _jumpEdgeThisTick = true;

        // LocalPlayerInput의 점프 게이트용. 입력 *시점* 접지 여부를 정확히 반영.
        public bool OnGround => _predictor.OnGround;

        // LocalPlayerInput이 공격 송신 성공 시 호출 — 로컬 commit window 예측 시작.
        // 지속 = 서버와 동일한 98_Shared 상수(AttackCommitWindowTicks × TickDuration 초).
        // 재공격 시 갱신(연장). 서버 AttackState 진입과 같은 규칙을 클라가 선예측 → rubber-band 0.
        public void NotifyAttack()
        {
            _commitWindowRemaining = Constants.AttackCommitWindowTicks * Constants.TickDuration;
            _attackCooldownRemaining = Constants.AttackCooldownTicks * Constants.TickDuration;
            _channelingWindow = false; // 평타 — Attack 스윙 모션.
        }

        // 스킬 시전 송신 성공 시 호출. 이동잠금 commit window는 평타와 공유하되, 쿨다운은 *스킬 독립*
        // (ThunderboltCooldownTicks 거울 — 평타 쿨다운 미소비). 이 window를 채널링으로 표시 →
        // LocalPlayerMotion이 Attack 대신 Channeling 모션을 선예측.
        public void NotifyChannel()
        {
            _commitWindowRemaining = Constants.AttackCommitWindowTicks * Constants.TickDuration;
            _thunderboltCooldownRemaining = Constants.ThunderboltCooldownTicks * Constants.TickDuration;
            _channelingWindow = true;
        }

        // Dash 송신 성공 시 호출. 쿨다운만 세팅 — 이동은 서버 force-adopt 경로(ShouldForceAdopt)가 흡수.
        // commit window / 채널링은 없음: Dash는 strikeDelayTicks=0이라 서버가 즉시 Attack 상태로 전환,
        // Attack + serverVx≠0 조건으로 해당 S_Snapshot이 위치를 흡수한다.
        public void NotifyDash()
        {
            _dashCooldownRemaining = Constants.DashCooldownTicks * Constants.TickDuration;
        }

        // Teleport 송신 성공 시 호출. 쿨다운 세팅 + 다음 snapshot을 즉시 스냅으로 처리하도록 플래그.
        // arriveCallback: 스냅 채택(새 위치 확정) 직후 main thread에서 1회 호출 — 도착 이펙트 스폰용.
        public void NotifyTeleport(Action? arriveCallback = null)
        {
            _teleportCooldownRemaining = Constants.TeleportCooldownTicks * Constants.TickDuration;
            _teleportSnapPending = true;
            _teleportArriveCallback = arriveCallback; // 이전 미소비 콜백은 덮어쓰기 (무해 — 다음 시전)
        }

        // EnemyAttackHandler가 본인 피격(S_EnemyAttack) 시 호출 — hit-bridge 게이트 시작.
        // animState==Hit 스냅샷이 도착하기 전까지 입력을 미리 잠가 onset 당김을 줄인다.
        public void NotifyHit()
        {
            _hitGateRemaining = HitGateBridgeTicks * Constants.TickDuration;
        }

        // 이동 잠금 판정 순수 함수 — 서버 AttackState/HitState/DeathState(LocksMovement)의 클라 거울.
        //   - localLockRemaining > 0: 로컬 잠금 타이머(공격 commit 선예측 OR 피격 hit-bridge) = 서버 확인 전 즉시 잠금.
        //   - serverAnimState Attack: 서버 window가 로컬 타이머보다 길면 잠금 연장(거울 보정).
        //   - serverAnimState Hit/Death: 클라가 예측 불가한 서버 전용 상태 → 서버 신뢰 잠금.
        public static bool IsMovementLocked(float localLockRemaining, AnimState serverAnimState)
        {
            if (localLockRemaining > 0f) return true;
            return serverAnimState == AnimState.Attack
                || serverAnimState == AnimState.Hit
                || serverAnimState == AnimState.Death;
        }

        // **source-gating 산출 순수 함수** — 잠금 시 이동/점프 입력을 *근원에서* 0으로 막는다.
        // 이 출력이 Predict / C_MoveIntent 송신 / NotifySent(replay) 세 곳에 *동일하게* 흘러가야
        // reconcile이 서버와 정확히 일치(rubber-band 0). 그 핵심 불변식을 회귀로부터 박기 위해
        // MonoBehaviour Update에서 분리해 단위 테스트 가능하게 추출.
        public static (sbyte moveX, bool jumpEdge) ResolveGatedInput(bool locked, sbyte rawMoveX, bool rawJumpEdge)
        {
            if (locked) return ((sbyte)0, false);
            return (rawMoveX, rawJumpEdge);
        }

        // force-adopt 판정 순수 함수 — 임계 이내여도 서버 위치를 즉시 채택할지 결정.
        //   - teleportSnap: Teleport 스킬 후 첫 snapshot — 무조건 채택.
        //   - Hit(넉백): 서버 권위 임펄스라 클라가 예측 불가 — 무조건 채택.
        //   - Attack + serverVx≠0: Dash/lunge처럼 서버가 전방 임펄스를 준 경우만 채택.
        //     Attack이지만 serverVx≈0인 평타(Mage 등)는 채택하지 않음 — rubber-band 밀림 봉합.
        public static bool ShouldForceAdopt(bool teleportSnap, AnimState serverAnimState, float serverVx)
        {
            if (teleportSnap) return true;
            if (serverAnimState == AnimState.Hit) return true;
            if (serverAnimState == AnimState.Attack)
                // 서버가 |임펄스 vx| < ExternalImpulseEpsilon 을 0 으로 클램프하므로 살아남은 vx 는 항상 >= ε — 이 게이트는 그 클램프의 보색.
                return Mathf.Abs(serverVx) >= Constants.ExternalImpulseEpsilon;
            return false;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            // UI·쿨다운 타이머는 frame dt 감쇠 — 송신 박자와 무관한 표시용.
            if (_attackCooldownRemaining > 0f)
                _attackCooldownRemaining = Mathf.Max(0f, _attackCooldownRemaining - dt);
            if (_thunderboltCooldownRemaining > 0f)
                _thunderboltCooldownRemaining = Mathf.Max(0f, _thunderboltCooldownRemaining - dt);
            if (_dashCooldownRemaining > 0f)
                _dashCooldownRemaining = Mathf.Max(0f, _dashCooldownRemaining - dt);
            if (_teleportCooldownRemaining > 0f)
                _teleportCooldownRemaining = Mathf.Max(0f, _teleportCooldownRemaining - dt);

            _sendAccumulator += dt;

            // 고정 서브스텝 루프 — spiral of death 방지를 위해 최대 MaxSubstepsPerFrame 회.
            // 서버 "틱당 입력 1개 소비 → Step 1회" 의 정확한 거울.
            int substeps = 0;
            while (_sendAccumulator >= Constants.TickDuration && substeps < MaxSubstepsPerFrame)
            {
                _sendAccumulator -= Constants.TickDuration;
                substeps++;

                // source-gating 타이머를 서브스텝 박자로 감쇠 — 게이트 깜빡임 방지.
                if (_commitWindowRemaining > 0f)
                    _commitWindowRemaining = Mathf.Max(0f, _commitWindowRemaining - Constants.TickDuration);
                if (_hitGateRemaining > 0f)
                    _hitGateRemaining = Mathf.Max(0f, _hitGateRemaining - Constants.TickDuration);

                // **source-gating** (헌법 #1 정합): 잠금 시 입력을 *근원에서* 0으로 막는다.
                // Predict / 송신(C_MoveIntent) / InputHistory(replay) 셋이 같은 gated 입력을 쓰므로
                // reconcile replay가 서버와 정확히 일치.
                float localLock = Mathf.Max(_commitWindowRemaining, _hitGateRemaining);
                bool locked = IsMovementLocked(localLock, _serverAnimState);
                (sbyte moveX, bool jumpEdge) = ResolveGatedInput(locked, _currentMoveX, _jumpEdgeThisTick);

                // 점프 latch 소비 — 이 서브스텝이 jumpEdge를 사용했으므로 클리어.
                // 고fps 서브스텝 0번 프레임 유실 방지: 다음 서브스텝 전까지 RequestJump가 재세팅 가능.
                if (_jumpEdgeThisTick) _jumpEdgeThisTick = false;

                // prev/curr 기록 후 Predict — 시각 보간의 두 끝점.
                _prevPredictPos = _predictor.Position;
                _predictor.Predict(moveX, jumpEdge);
                _currPredictPos = _predictor.Position;

                UnityClientSession? session = UnityClientSession.Instance;
                if (session == null) continue;

                _localTickCounter++;

                byte input = InputBits.Encode(moveX, jumpEdge);
                C_MoveIntent pkt = new C_MoveIntent
                {
                    input = input,
                    clientTick = _localTickCounter
                };
                session.SendIntent(pkt.Write());

                // 송신 직후 InputHistory push — gated 입력 그대로 박아 replay 일치.
                _predictor.NotifySent(_localTickCounter, moveX, jumpEdge);
            }

            // cap 도달 후 남은 통째 틱은 버림 — 서버 입력 큐가 drop-oldest(MaxInputQueue=6)라
            // backlog 추격 버스트는 어차피 버려지고 reconcile 대상만 늘린다. freeze 복구 = reconcile 담당.
            if (_sendAccumulator >= Constants.TickDuration)
                _sendAccumulator %= Constants.TickDuration;

            // 시각 보간 — substep 박자 사이를 부드럽게 렌더링.
            // accumulator / TickDuration = 현재 substep 구간 내 진행률(0..1).
            float alpha = _sendAccumulator / Constants.TickDuration;
            Vector2 renderPos = Vector2.Lerp(_prevPredictPos, _currPredictPos, alpha);
            transform.position = new Vector3(renderPos.x, renderPos.y, 0f);
        }

        // S_EnterMap → 서버가 정한 spawn 좌표 적용. predictor 초기화 — 다음 Update에서 transform 자동 동기.
        // 단 spawn 첫 frame 깜빡임 방지를 위해 즉시 transform도 한 번 설정.
        // 보간 버퍼(prev/curr)도 새 위치로 리셋 — 점프 위치를 가로질러 보간하는 유령 미끄러짐 방지.
        public void SetServerPosition(Vector3 worldPos)
        {
            _predictor.SetInitialPosition(new Vector2(worldPos.x, worldPos.y));
            _prevPredictPos = new Vector2(worldPos.x, worldPos.y);
            _currPredictPos = _prevPredictPos;
            transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
        }

        // 맵 전환 시 옛 LocalPlayer를 snapshot/Update에서 분리. HandleMapTransition이 씬 전환 시작 전 호출.
        //
        // 이 GameObject는 페이드 동안 아직 살아있어, 위치를 (0,0)으로 박으면 도착한 S_Snapshot이
        // 서버의 새 맵 좌표로 reconcile snap → 전환 직후 캐릭터가 튐. 새 맵 LocalPlayer는 별도 인스턴스 +
        // 깨끗한 predictor라 옛 버퍼 리셋 자체가 불필요. 위치는 건드리지 않고:
        //   1) Instance 등록 해제 — HandleSnapshot의 `Instance != null` 가드로 이후 snapshot이 drop.
        //   2) enabled=false — Update 정지 (predict/transform 갱신 중단).
        // 곧 씬 전환(LoadScene Single)이 이 GameObject를 파괴하고, 새 맵에서 새로 spawn됨.
        public void ResetPredictionForMapTransition()
        {
            if (Instance == this) Instance = null;
            enabled = false;
            Debug.Log("[LocalPlayer] 맵 전환 — 옛 LocalPlayer를 snapshot/Update에서 분리 (곧 파괴).");
        }

        // S_Snapshot → predictor의 reconcile 판단에 위임. Predictor가 X+Y 둘 다 비교.
        // animState = 서버 권위 시각 상태 — 이동 게이트(Hit/Death/Attack) + 넉백 force-adopt 판정용.
        public void OnServerSnapshot(float serverX, float serverY,
                                     float serverVx, float serverVy,
                                     int serverTick, uint ackedClientTick, byte animState)
        {
            _serverAnimState = (AnimState)animState;
            float prevX = _predictor.Position.x;
            float prevY = _predictor.Position.y;

            // Teleport 스냅: S_SkillCast(Teleport) 수신 후 최초 Snapshot을 즉시 force-adopt.
            // reconcile 임계 우연 의존 금지 — 명시적 플래그로 보간 없이 즉시 스냅(Phase 06 계획 확정).
            bool teleportSnap = _teleportSnapPending;
            Action? arriveCallback = null;
            if (_teleportSnapPending)
            {
                _teleportSnapPending = false;
                arriveCallback = _teleportArriveCallback;
                _teleportArriveCallback = null;
            }

            // 넉백(Hit)·전방 임펄스가 있는 공격(Dash/lunge)·Teleport는 서버 권위 임펄스라 클라가 예측 안 함
            //   → 임계 이내라도 서버 위치 채택(force-adopt)해 시각화 + sub-threshold offset 누적 방지.
            // Attack이지만 serverVx≈0(Mage 평타 등)은 force-adopt 제외 — 스냅샷마다 임계 이내 서버 위치를
            //   채택하면 rubber-band 밀림 발생.
            bool reconciled = _predictor.OnSnapshot(
                serverX, serverY, serverVx, serverVy, ackedClientTick,
                forceAdopt: ShouldForceAdopt(teleportSnap, _serverAnimState, serverVx));
            if (reconciled)
            {
                float dx = serverX - prevX;
                float dy = serverY - prevY;
                Debug.Log(
                    $"[Reconcile] d=({dx:F2}, {dy:F2}) at serverTick={serverTick} " +
                    $"ack={ackedClientTick} teleportSnap={teleportSnap} (count={_predictor.SnapCount})");

                // 보간 버퍼(prev/curr)를 reconcile 후 위치로 리셋 — 점프(teleport/snap)를
                // 가로질러 보간하면 유령 미끄러짐 발생. 소폭 정상 reconcile은 다음 서브스텝이 자연 흡수.
                _prevPredictPos = _predictor.Position;
                _currPredictPos = _predictor.Position;
            }

            // 텔레포트 도착 이펙트 — 스냅 채택으로 새 위치가 transform에 박힌 직후.
            // arriveCallback은 teleportSnap=true일 때만 non-null이므로 중복 발동 없음.
            arriveCallback?.Invoke();
        }

        // 선택 클래스 → MoveParams 변환 (ClassLoadout 경유 — process-local 캐시 우선).
        // PlayerStats.ForClass 단일 출처 (헌법 #4) — 직업 분기 책임은 ForClass에 위임.
        static MoveParams ResolveClassMoveParams()
        {
            int classValue = Bootstrap.ClassLoadout.GetSelectedClassValue(
                (int)Shared.Protocol.CharacterClass.Knight);

            PlayerStats stats = PlayerStats.ForClass((Shared.Protocol.CharacterClass)classValue);
            return new MoveParams(stats.MoveSpeed, stats.JumpVel);
        }
    }
}
