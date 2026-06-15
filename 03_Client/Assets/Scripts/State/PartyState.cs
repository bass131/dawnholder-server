#nullable enable
using System;
using UnityEngine;

namespace Dawnholder.Client.State
{
    // 클라 파티 미러 — 서버 S_PartyUpdate/S_PartyInviteRecv/S_PartyError 통보를 거울처럼 저장.
    // 클라이언트가 임의로 변경하지 않음 (헌법 §1). P2~P5 UI가 이벤트를 구독해 렌더.
    //
    // **싱글톤 패턴**: RemoteEntityRegistry와 동일하게 MonoBehaviour + Instance.
    //   NetworkService가 씬 시작 시 생성하거나, Bootstrap이 코드 주도로 주입.
    //   씬 간 이동 시 파티 상태는 세션 지속 동안 유효 → DontDestroyOnLoad 사용.
    //
    // **멤버 슬롯**: PDL 가변 list 미지원 → 정원 2 고정(member0/member1). 빈 슬롯 = entityId 0.
    [DisallowMultipleComponent]
    public class PartyState : MonoBehaviour
    {
        public static PartyState Instance { get; private set; } = null!;

        // ── 현재 파티 상태 ──────────────────────────────────────────────
        public int PartyId { get; private set; }
        public int LeaderEntityId { get; private set; }

        // 멤버 슬롯 (entityId 0 = 빈 슬롯)
        public int Member0EntityId { get; private set; }
        public int Member1EntityId { get; private set; }
        public byte Member0Class { get; private set; }
        public byte Member1Class { get; private set; }

        public bool InParty => PartyId != 0;

        // ── 대기 초대 (S_PartyInviteRecv) ───────────────────────────────
        public int PendingInviterEntityId { get; private set; }
        public byte PendingInviterClass { get; private set; }
        public bool HasPendingInvite { get; private set; }

        // ── 마지막 에러 코드 (S_PartyError) ─────────────────────────────
        // 0=상대없음 1=이미파티 2=자기자신 3=정원초과. -1=에러 없음.
        public int LastErrorReason { get; private set; } = -1;

        // ── 이벤트 (UI 구독용) ───────────────────────────────────────────
        // 파티 상태 갱신(가입/업데이트/해산) 시 발화.
        public event Action? OnPartyUpdated;

        // 초대 수신 시 발화 — P2 팝업 트리거.
        public event Action? OnInviteReceived;

        // 에러 수신 시 발화 — UI 피드백 트리거.
        public event Action<byte>? OnError;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null!;
        }

        // S_PartyUpdate 핸들러에서 메인 스레드로 호출.
        public void ApplyUpdate(int partyId, int leaderId, int m0Id, int m1Id, byte m0Class, byte m1Class)
        {
            PartyId         = partyId;
            LeaderEntityId  = leaderId;
            Member0EntityId = m0Id;
            Member1EntityId = m1Id;
            Member0Class    = m0Class;
            Member1Class    = m1Class;

            OnPartyUpdated?.Invoke();
        }

        // S_PartyUpdate partyId==0 — 파티 해산.
        public void Clear()
        {
            PartyId         = 0;
            LeaderEntityId  = 0;
            Member0EntityId = 0;
            Member1EntityId = 0;
            Member0Class    = 0;
            Member1Class    = 0;

            OnPartyUpdated?.Invoke();
        }

        // S_PartyInviteRecv 핸들러에서 메인 스레드로 호출.
        public void SetPendingInvite(int inviterEntityId, byte inviterClass)
        {
            PendingInviterEntityId = inviterEntityId;
            PendingInviterClass    = inviterClass;
            HasPendingInvite       = true;

            OnInviteReceived?.Invoke();
        }

        // P2 팝업이 초대 응답 후 소비.
        public void ClearPendingInvite()
        {
            HasPendingInvite       = false;
            PendingInviterEntityId = 0;
            PendingInviterClass    = 0;
        }

        // S_PartyError 핸들러에서 메인 스레드로 호출.
        public void SetLastError(byte reason)
        {
            LastErrorReason = reason;
            OnError?.Invoke(reason);
        }
    }
}
