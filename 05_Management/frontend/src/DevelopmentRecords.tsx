import { useEffect, useRef, useState } from 'react';
import { readCatalog, catalogReferenceErrors, MAX_CATALOG_BYTES, type RecordCatalog } from './recordCatalog';
import CatalogExplorer from './developmentRecords/CatalogExplorer';

export default function DevelopmentRecords({ active = true }: { active?: boolean }) {
  const [catalog, setCatalog] = useState<RecordCatalog | null>(null);
  const [version, setVersion] = useState<string | null>(null);
  const [draftVersion, setDraftVersion] = useState<string | null>(null);
  const [loadedText, setLoadedText] = useState('');
  const [draft, setDraft] = useState('');
  const [dirty, setDirty] = useState(false);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState('기록을 읽는 중입니다.');
  const [failure, setFailure] = useState(false);
  const [baselineAvailable, setBaselineAvailable] = useState(false);
  const [draftBaselineAvailable, setDraftBaselineAvailable] = useState(false);
  const [draftError, setDraftError] = useState('');
  const alive = useRef(true);
  const operation = useRef(false);
  const draftRef = useRef('');
  const dirtyRef = useRef(false);
  const inputRef = useRef<HTMLInputElement>(null);
  function updateDraft(text: string, changed: boolean) {
    draftRef.current = text; dirtyRef.current = changed;
    setDraft(text); setDirty(changed); setDraftError('');
  }
  async function reload() {
    if (operation.current) return;
    if (!window.systemRecords) { setNotice('기록 연결을 사용할 수 없습니다. 데스크톱 앱에서 실행하세요.'); setFailure(true); return; }
    operation.current = true; setBusy(true);
    try {
      const result = await window.systemRecords.readCatalog();
      if (!alive.current) return;
      if (result.ok) {
        setCatalog(result.catalog); setVersion(result.version); setLoadedText(result.text); setBaselineAvailable(true);
        if (!dirtyRef.current) {
          updateDraft(result.text, false);
          setDraftVersion(result.version); setDraftBaselineAvailable(true);
        }
        setFailure(false); setNotice(dirtyRef.current ? '최신 기록을 읽었습니다. 편집 초안은 보존했습니다. 저장 전에 변경 내용을 비교하세요.' : '기록 파일을 읽었습니다. 자동 갱신은 하지 않습니다.');
      } else {
        setFailure(true); setNotice(`${result.message}${catalog ? ' · 갱신 실패: 이전에 읽은 기록을 표시합니다.' : ''}`);
        if (result.code === 'missing' || result.version) {
          setVersion(result.version ?? null); setLoadedText(result.text ?? ''); setBaselineAvailable(true);
          if (!dirtyRef.current) {
            updateDraft(result.text ?? '', false);
            setDraftVersion(result.version ?? null); setDraftBaselineAvailable(true);
          }
        } else {
          setBaselineAvailable(false);
          if (!dirtyRef.current) setDraftBaselineAvailable(false);
        }
      }
    } catch { if (alive.current) { setFailure(true); setBaselineAvailable(false); if (!dirtyRef.current) setDraftBaselineAvailable(false); setNotice('기록 연결에서 응답을 받지 못했습니다. 초안은 보존합니다.'); } }
    finally { operation.current = false; if (alive.current) setBusy(false); }
  }
  useEffect(() => { alive.current = true; void reload(); return () => { alive.current = false; }; }, []);
  async function importFile(file: File | undefined) {
    if (!file || operation.current) return;
    if (dirtyRef.current) { setDraftError('편집 중인 초안이 있습니다. 초안을 보존하려면 먼저 복사하거나 저장하세요. 다른 파일을 불러오려면 편집 취소를 명시적으로 선택하세요.'); return; }
    if (file.size > MAX_CATALOG_BYTES) { setDraftError('불러올 파일은 2 MiB 이하이어야 합니다.'); return; }
    operation.current = true; setBusy(true);
    try {
      const text = await file.text();
      if (!alive.current) return;
      updateDraft(text, true); setDraftVersion(version); setDraftBaselineAvailable(baselineAvailable);
      setNotice(`${file.name}을 초안으로 불러왔습니다. 아직 기록 파일에 저장하지 않았습니다.`);
    } catch { setDraftError('선택한 파일을 읽지 못했습니다.'); }
    finally { operation.current = false; if (alive.current) setBusy(false); }
  }
  function validateDraft(): boolean {
    if (new TextEncoder().encode(draftRef.current).length > MAX_CATALOG_BYTES) { setDraftError('초안은 2 MiB 이하이어야 합니다.'); return false; }
    try {
      const parsed = readCatalog(JSON.parse(draftRef.current));
      if (!parsed) { setDraftError('카탈로그 스키마 또는 중복 ID를 확인하세요.'); return false; }
      const errors = catalogReferenceErrors(parsed);
      if (errors.length) { setDraftError(errors.slice(0, 5).join('\n')); return false; }
      setDraftError(''); setNotice('초안 형식과 ID 참조를 확인했습니다. 저장하면 기록 파일을 갱신합니다.'); return true;
    } catch { setDraftError('초안이 유효한 JSON이 아닙니다.'); return false; }
  }
  async function save() {
    if (operation.current || !draftBaselineAvailable || !window.systemRecords || !validateDraft()) return;
    operation.current = true; setBusy(true);
    try {
      const result = await window.systemRecords.saveCatalog({ text: draftRef.current, expectedVersion: draftVersion });
      if (!alive.current) return;
      if (result.ok) {
        setCatalog(result.catalog); setVersion(result.version); setLoadedText(result.text); updateDraft(result.text, false);
        setDraftVersion(result.version); setDraftBaselineAvailable(true); setBaselineAvailable(true);
        setFailure(false); setNotice('기록을 저장하고 화면에 반영했습니다. 재빌드는 필요하지 않습니다.');
      } else { setFailure(true); setNotice(`${result.message} · 편집 초안은 보존했습니다.`); }
    } catch { setFailure(true); setNotice('저장 응답을 받지 못했습니다. 초안을 보존합니다. 새로고침으로 기록을 확인하세요.'); }
    finally { operation.current = false; if (alive.current) setBusy(false); }
  }
  return <div>
    <div className="record-runtime-bar"><button className="record-link" type="button" disabled={busy} onClick={() => void reload()}>기록 새로고침</button><label className="record-file-label">JSON 파일 불러오기<input ref={inputRef} type="file" accept=".json,application/json" disabled={busy || dirty} onChange={event => { const file = event.target.files?.[0]; void importFile(file); event.target.value = ''; }} /></label><span>{dirty ? '저장하지 않은 편집 초안 있음' : '실행 중 읽기·편집 가능'}</span></div>
    <p className={failure ? 'record-runtime-notice record-missing' : 'record-runtime-notice'} role="status">{notice}</p>
    {catalog ? <CatalogExplorer data={catalog} active={active} /> : <div className="record-empty"><h3>시스템 기록을 읽을 수 없습니다.</h3><p>기록 새로고침을 시도하거나 아래 기록 편집에서 JSON을 불러와 저장하세요.</p></div>}
    <details className="record-editor"><summary>기록 편집 {dirty ? '· 미저장 초안' : ''}</summary><p>현재 화면은 마지막으로 읽은 기록입니다. JSON 파일 불러오기는 초안만 바꿉니다. 명시적으로 저장할 때 05_Management/records/catalog.json을 갱신합니다. 자동 갱신은 하지 않습니다. 저장한 내용은 같은 파일을 읽는 공동 조회 MCP의 다음 요청에 반영됩니다.</p><label>카탈로그 JSON<textarea aria-describedby="record-editor-help" value={draft} disabled={busy} spellCheck={false} onChange={event => updateDraft(event.target.value, event.target.value !== loadedText)} /></label><p id="record-editor-help">새로고침은 편집 초안을 보존합니다. 변경을 버리려면 편집 취소를 선택하세요. 직전 정상 기록 백업은 복구용이며 변경 근거가 아닙니다.</p>{draftError && <p className="record-missing" role="alert">{draftError}</p>}<div className="record-editor-actions"><button className="record-link" type="button" disabled={busy || !draft} onClick={validateDraft}>초안 검증</button><button className="record-link" type="button" disabled={busy || !dirty || !draftBaselineAvailable || !window.systemRecords} onClick={() => void save()}>검증 후 기록 저장</button><button className="record-link" type="button" disabled={busy || !dirty} onClick={() => { updateDraft(loadedText, false); setDraftVersion(version); setDraftBaselineAvailable(baselineAvailable); setNotice('편집 초안을 취소하고 마지막으로 읽은 원문으로 돌아갔습니다.'); }}>편집 취소 · 초안 버리기</button></div></details>
  </div>;
}
