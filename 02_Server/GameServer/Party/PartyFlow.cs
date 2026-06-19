using Dawnholder.Server.GameServer.Loop;

namespace Dawnholder.Server.GameServer.Party;

// 파티 도메인 오케스트레이션 — 초대/응답/탈퇴/disconnect 정리의 비즈니스 로직 + tick 마샬링.
//
// **추출 근거 (M7.6 P03, 감사 #3)**: GameSession(세션 lifecycle + dispatch)에 파티 검증 규칙·결성·
//   해산·통보가 EnqueueJob 람다로 박혀 있던 것을 *순수 추출*. GameSession = auth 게이트 + 위임,
//   PartyFlow = 파티 규칙 소유. 동작 불변(검증 규칙·순서·통보 동치).
//
// **헌법 #3 (Trust Boundary) — 행위자 강제**: 초대자/응답자/탈퇴자 entityId는 *호출자(GameSession)*가
//   `_entityId`(이 소켓이 누구인지)에서 강제해 넘긴다 — 패킷엔 행위자 필드 없음(C_Attack 정합).
//   PartyFlow는 *검증된 entityId*를 가정(위장 차단은 세션 경계에서 이미 완료). claimedInviter는
//   여전히 패킷값(untrusted)이라 Respond에서 서버 기록과 일치 검증.
// **actor 경계**: 파티 상태 조작 + 송신은 world.Party.EnqueueJob 람다(tick thread)에서만.
//   여기선 마샬링만 — 직접 PartyRegistry 내부를 호출하지 않음(헌법 §5).
internal static class PartyFlow
{
    // 파티 초대 오케스트레이션. inviterEntityId는 세션이 강제한 행위자(_entityId).
    internal static void Invite(GameWorld world, int inviterEntityId, int targetEntityId)
    {
        int target = targetEntityId;
        world.Party.EnqueueJob(() =>
        {
            // ── 거절 4종 검증 (헌법 §3 — 모두 서버 판정. 행위자=inviterEntity(_entityId 강제)) ──
            //   RecordInvite 전에 fail-closed. 거절 통보는 초대자(행위자)에게 S_PartyError.

            // 2 = 자기 자신 초대: target == 초대자.
            if (target == inviterEntityId)
            {
                PartyNotifier.SendPartyError(world, inviterEntityId, PartyRegistry.ErrorSelfInvite);
                return;
            }

            // 0 = 상대 없음: target이 현재 어느 맵에도 없는 entityId(오프라인/유령 id).
            //   TryGetEntityClass = 어느 맵에든 존재하면 true(존재 확인 재활용).
            if (!world.TryGetEntityClass(target, out _))
            {
                PartyNotifier.SendPartyError(world, inviterEntityId, PartyRegistry.ErrorTargetMissing);
                return;
            }

            // 1 = 이미 파티 중: 초대자 또는 피초대자가 이미 파티 보유.
            //   2인 고정이라 정원초과(3)는 사실상 이 케이스로 수렴 — reason 3은 AddMember 경로(가변정원 대비) 보존.
            if (world.Party.GetPartyByEntity(inviterEntityId) != null
                || world.Party.GetPartyByEntity(target) != null)
            {
                PartyNotifier.SendPartyError(world, inviterEntityId, PartyRegistry.ErrorAlreadyInParty);
                return;
            }

            // happy: pending invite 기록(발급 tick = 만료 기준) + 피초대자에게 1:1 통보.
            world.Party.RecordInvite(inviterEntityId, target, world.CurrentTick);
            PartyNotifier.SendInviteRecv(world, inviterEntityId, target);
        });
    }

    // 초대 응답 오케스트레이션. responderEntityId는 세션이 강제한 행위자(_entityId).
    //   claimedInviter는 패킷값(untrusted) — 서버 기록(pendingInviter)과 일치 검증으로 위장 차단.
    internal static void Respond(GameWorld world, int responderEntityId, int claimedInviter, bool accepted)
    {
        world.Party.EnqueueJob(() =>
        {
            // 보류 초대 매칭(존재 확인). 없음/만료(Tick이 청소) → silent drop(에러 X — 위조/지연 응답).
            if (!world.Party.TryGetPendingInvite(responderEntityId, out int pendingInviter))
                return; // 보류 초대 없음/만료 — 응답 race silent

            // claimedInviter 일치 검증(헌법 §3 — 위장 차단): 패킷 주장과 서버 기록 불일치 → silent drop.
            //   서버 기록(pendingInviter)이 진실. 에러도 안 보냄(공격자에게 정보 노출 차단).
            if (claimedInviter != pendingInviter)
                return;

            world.Party.ConsumeInvite(responderEntityId);

            if (!accepted)
                return; // 거절: 초대 소비만(거절 측 통보 없음 — UX는 클라 timeout 처리)

            // 수락: 보류된 inviter로 파티 결성. CreateParty가 null = 그새 한쪽이 파티 보유(race) → silent.
            PartyState? party = world.Party.CreateParty(pendingInviter, responderEntityId);
            if (party == null) return;

            PartyNotifier.SendPartyUpdate(world, party);
        });
    }

    // 파티 탈퇴 오케스트레이션. leaverEntityId는 세션이 강제한 행위자(_entityId).
    internal static void Leave(GameWorld world, int leaverEntityId)
    {
        world.Party.EnqueueJob(() =>
        {
            PartyState? party = world.Party.GetPartyByEntity(leaverEntityId);
            if (party == null) return; // 파티 없음 — no-op

            // 해산 전 멤버 스냅샷 — Disband가 인덱스를 비우므로 통보 대상은 미리 캡처.
            List<int> formerMembers = new(party.Members);
            world.Party.Disband(party.PartyId);

            // 남은 멤버 전원에게 해산 통보(partyId=0). 탈퇴자 본인 포함 — 클라가 파티 UI 정리.
            PartyNotifier.SendDisband(world, formerMembers);
        });
    }

    // disconnect 시 파티/초대 정리. leaverEntityId는 세션이 강제한 행위자(_entityId).
    //
    // **actor 경계**: world.Party.EnqueueJob으로 마샬링 — PartyRegistry 내부 직접 호출 X(헌법 §5, race).
    // **멱등**: 파티 없으면 no-op, 초대 없으면 no-op. 끊긴 본인은 SendDisband 대상에서 제외(이미 소켓 닫힘).
    internal static void CleanupOnDisconnect(GameWorld world, int leaverEntityId)
    {
        world.Party.EnqueueJob(() =>
        {
            // 1) 끊긴 본인이 얽힌 보류 초대 양방향 제거(받은 초대 + 보낸 초대).
            world.Party.RemoveInvitesInvolving(leaverEntityId);

            // 2) 파티 보유 시 해산 + 남은 멤버에게 partyId=0 통보.
            PartyState? party = world.Party.GetPartyByEntity(leaverEntityId);
            if (party == null) return; // 파티 없음 — 초대 정리만으로 끝

            // 해산 전 멤버 스냅샷. 끊긴 본인은 통보 제외(소켓 닫힘 — SendToEntity가 silent skip하지만 명시 제외).
            List<int> remaining = party.Members.Where(id => id != leaverEntityId).ToList();
            world.Party.Disband(party.PartyId);

            PartyNotifier.SendDisband(world, remaining);
        });
    }
}
