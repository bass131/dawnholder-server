import { useState, type ReactNode } from 'react';
import DevelopmentRecords from './DevelopmentRecords';
import SystemCardsView from './systemCards/SystemCardsView';
import ThemeImage from './theme/ThemeImage';

const sections = {
  operations: { title: '서버 운영', description: '서버 상태와 원본 로그를 확인합니다.' },
  users: { title: '유저 관리', description: '유저 정보와 GM 권한, 변경 근거를 확인합니다.' },
  development: { title: '개발 현황', description: '목표부터 변경 내역과 검증 결과까지 확인합니다.' },
} as const;
type Section = keyof typeof sections;
type IconName = Section | 'terminal' | 'arrow' | 'play' | 'stop' | 'document';
const navigation: Section[] = ['operations', 'users', 'development'];

function Icon({ name }: { name: IconName }) {
  const paths: Record<IconName, ReactNode> = {
    operations: <><rect x="4" y="4" width="16" height="6" rx="1.5" /><rect x="4" y="14" width="16" height="6" rx="1.5" /><path d="M8 7h.01M8 17h.01M12 7h5M12 17h5" /></>,
    users: <><circle cx="9" cy="8" r="3" /><path d="M3 20v-2a6 6 0 0 1 12 0v2M16 5a3 3 0 0 1 0 6M17 14a5 5 0 0 1 4 5v1" /></>,
    development: <><path d="m7 7-5 5 5 5M17 7l5 5-5 5M14 4l-4 16" /></>,
    terminal: <><rect x="3" y="4" width="18" height="16" rx="2" /><path d="m7 9 3 3-3 3M13 15h4" /></>,
    arrow: <path d="m9 6 6 6-6 6" />,
    play: <path d="m8 5 10 7-10 7z" />,
    stop: <rect x="6" y="6" width="12" height="12" rx="1" />,
    document: <><path d="M14 3H6a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9zM14 3v6h6M8 13h8M8 17h5" /></>,
  };
  return <svg className="icon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" focusable="false">{paths[name]}</svg>;
}

function Operations() {
  return (
    <>
      <dl className="summary-strip" aria-label="서버 관측 정보">
        {[['실행 상태', '상태 데이터 미수집'], ['운영 버전', '버전 데이터 미수집'], ['접속 현황', '접속 데이터 미수집']].map(([label, reason]) => (
          <div className="summary-item" key={label}><dt>{label}</dt><dd><span className="summary-value" aria-label="미수집">—</span><span className="summary-reason">{reason}</span></dd></div>
        ))}
      </dl>
      <div className="work-panel log-panel">
        <div className="panel-heading"><h3>서버 로그</h3><span>원본 기록</span></div>
        <div className="log-empty"><Icon name="terminal" /><h4>아직 읽은 로그가 없습니다.</h4><p>서버 연결 후 기록을 확인할 수 있습니다.</p></div>
        <div className="panel-footer">로그 · 복구 · 예약 이력은 연결 후 확인할 수 있습니다.</div>
      </div>
      <div className="operations-bar">
        <div><h3>운영 제어</h3><p id="control-explanation">운영 제어가 연결되면 사용할 수 있습니다.</p></div>
        <div className="control-buttons"><button type="button" disabled aria-describedby="control-explanation"><Icon name="play" />서버 시작</button><button type="button" disabled aria-describedby="control-explanation"><Icon name="stop" />서버 종료</button></div>
      </div>
    </>
  );
}

function Users() {
  return (
    <>
      <div className="work-panel user-panel">
        <div className="panel-heading"><h3>유저 및 권한</h3><span>데이터 미수집</span></div>
        <table className="user-table"><thead><tr><th scope="col">유저</th><th scope="col">GM 권한</th><th scope="col">권한 변경 이력</th></tr></thead><tbody><tr><td colSpan={3}><div className="table-empty"><Icon name="users" /><h4>아직 읽은 유저 정보가 없습니다.</h4><p>연결 후 유저와 GM 권한을 확인할 수 있습니다.</p></div></td></tr></tbody></table>
      </div>
      <div className="evidence-row"><span className="row-icon"><Icon name="document" /></span><div><h3>이상행동 이벤트 근거</h3><p>아직 읽은 이벤트 근거가 없습니다.</p></div><span className="row-state">미수집</span></div>
    </>
  );
}

export default function App() {
  const [activeSection, setActiveSection] = useState<Section>('operations');
  const [developmentView, setDevelopmentView] = useState<'cards' | 'records'>('cards');
  const section = sections[activeSection];
  return (
    <div className="management">
      <a className="skip-link" href="#management-section">본문으로 건너뛰기</a>
      <div className="sidebar">
        <ThemeImage name="ledger-head" className="ledger-brand-image" />
        <div className="brand"><span className="brand-mark" aria-hidden="true">D</span><h1><span>Dawnholder</span>{' '}<span className="brand-subtitle">Management</span></h1></div>
        <nav aria-label="관리 영역">{navigation.map((key, index) => (
          <button className="nav-item" key={key} type="button" aria-pressed={activeSection === key} aria-controls="management-section" onClick={() => setActiveSection(key)}><Icon name={key} /><span className="priority">{index + 1}</span>{' '}<span className="nav-title">{sections[key].title}</span></button>
        ))}</nav>
        <div className="sidebar-footer">개발 PC 전용</div>
      </div>
      <div className="workspace">
        <header className="toolbar"><div className="breadcrumb"><span>Management</span><Icon name="arrow" /><span>{section.title}</span></div><span className="toolbar-context">로컬 워크스페이스</span></header>
        <main>
          <section id="management-section" aria-labelledby="section-heading">
            {activeSection === 'development' && <h2 id="section-heading" className="visually-hidden">개발 현황</h2>}
            {activeSection !== 'development' && <div className="page-heading"><h2 id="section-heading">{section.title}</h2><p>{section.description}</p></div>}
            <aside className="connection-notice" aria-label="연결 상태"><strong>관리 기능 미연결</strong><p>{activeSection === 'development' ? '시스템 기록은 로컬 파일에서 읽습니다. 서버의 실시간 상태는 미연결입니다.' : '실제 상태와 이력은 확인할 수 없습니다.'}</p></aside>
            {activeSection === 'operations' && <Operations />}
            {activeSection === 'users' && <Users />}
            <div hidden={activeSection !== 'development'}>
              <div className="development-tabs" aria-label="개발 현황 보기"><button type="button" aria-pressed={developmentView === 'cards'} onClick={() => setDevelopmentView('cards')}>시스템 카드</button><button type="button" aria-pressed={developmentView === 'records'} onClick={() => setDevelopmentView('records')}>개발 기록 · 기록 편집</button></div>
              <div hidden={developmentView !== 'cards'}><SystemCardsView active={activeSection === 'development' && developmentView === 'cards'} openRecords={() => setDevelopmentView('records')} /></div>
              <div hidden={developmentView !== 'records'}><DevelopmentRecords /></div>
            </div>
          </section>
        </main>
      </div>
      <footer className="app-status"><span>관리 기능 미연결 · 서버 실시간 상태 미연결</span><span>로컬 자료 · 자동 갱신 안 됨</span></footer>
    </div>
  );
}
