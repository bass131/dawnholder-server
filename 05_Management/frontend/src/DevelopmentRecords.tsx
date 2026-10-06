import { useEffect, useRef, useState } from 'react';
import type { CheckoutInfo } from '../electron/checkout-contract';
import type { RecordCatalog } from './recordCatalog';
import CatalogExplorer from './developmentRecords/CatalogExplorer';

export default function DevelopmentRecords({ active = true }: { active?: boolean }) {
  const [catalog, setCatalog] = useState<RecordCatalog | null>(null);
  const [version, setVersion] = useState('');
  const [checkout, setCheckout] = useState<CheckoutInfo | null>(null);
  const [generation, setGeneration] = useState(0);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState('기록을 읽는 중입니다.');
  const [failure, setFailure] = useState(false);
  const mounted = useRef(false);
  const requestSequence = useRef(0);
  const operation = useRef<number | null>(null);
  const hasSnapshot = useRef(false);

  async function reload() {
    if (operation.current !== null) return;
    const bridge = window.systemRecords;
    if (!bridge) {
      setFailure(true);
      setCheckout(null);
      setNotice('기록 연결을 사용할 수 없습니다. 데스크톱 앱에서 실행하세요.');
      return;
    }
    const request = ++requestSequence.current;
    operation.current = request;
    setBusy(true);
    setNotice('기록을 읽는 중입니다.');
    try {
      const [records, metadata] = await Promise.allSettled([bridge.readCatalog(), bridge.readCheckout()]);
      if (!mounted.current || request !== requestSequence.current) return;
      setCheckout(metadata.status === 'fulfilled' && metadata.value.ok ? metadata.value.checkout : null);
      // A refresh also invalidates an open section even if the index hash did
      // not change: its Markdown file may have changed independently.
      setGeneration(previous => previous + 1);
      if (records.status === 'fulfilled' && records.value.ok) {
        setCatalog(records.value.catalog);
        setVersion(records.value.version);
        hasSnapshot.current = true;
        setFailure(false);
        setNotice('기록 파일을 읽었습니다. 자동 갱신은 하지 않습니다.');
      } else {
        const message = records.status === 'fulfilled' && !records.value.ok
          ? records.value.message : '기록 연결에서 응답을 받지 못했습니다.';
        setFailure(true);
        setNotice(`${message}${hasSnapshot.current ? ' · 갱신 실패: 이전에 읽은 기록을 표시합니다.' : ''}`);
      }
    } catch {
      if (mounted.current && request === requestSequence.current) {
        setCheckout(null);
        setFailure(true);
        setNotice('기록 연결에서 응답을 받지 못했습니다.');
      }
    } finally {
      if (operation.current === request) operation.current = null;
      if (mounted.current && request === requestSequence.current) setBusy(false);
    }
  }

  useEffect(() => {
    mounted.current = true;
    void reload();
    return () => {
      // Cleanup owns the pending request, including a StrictMode remount. An
      // earlier request cannot update the new mount or hold its refresh lock.
      mounted.current = false;
      requestSequence.current += 1;
      operation.current = null;
    };
  }, []);

  const checkoutText = checkout?.state === 'known'
    ? `${checkout.branch ?? '분리된 HEAD'} · HEAD ${checkout.head.slice(0, 12)}`
    : '알 수 없음';
  return (
    <div>
      <div className="record-runtime-bar">
        <button className="record-link" type="button" disabled={busy} onClick={() => void reload()}>기록 새로고침</button>
      </div>
      <p className={failure ? 'record-runtime-notice record-missing' : 'record-runtime-notice'} role="status">{notice}</p>
      <p className="record-checkout">읽은 checkout: {checkoutText}</p>
      {catalog ? (
        <CatalogExplorer data={catalog} version={version} generation={generation} active={active} />
      ) : (
        <div className="record-empty"><h3>개발 기록을 읽을 수 없습니다.</h3><p>기록 새로고침을 시도하세요.</p></div>
      )}
    </div>
  );
}
