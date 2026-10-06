import { useEffect, useState } from 'react';
import { sourceSectionFailure } from '../../electron/source-section-contract';
import type { SourceSectionResult } from '../../electron/source-section-contract';
import type { RecordSource } from '../recordCatalog';

export default function SourceSection({ source }: { source: RecordSource }) {
  const [result, setResult] = useState<SourceSectionResult | null>(null);
  useEffect(() => {
    let current = true;
    async function read() {
      let next: SourceSectionResult;
      try {
        next = window.systemRecords
          ? await window.systemRecords.readSection(source.id)
          : sourceSectionFailure('load');
      } catch {
        next = sourceSectionFailure('load');
      }
      if (current) setResult(next);
    }
    void read();
    // This component belongs to one source and refresh generation. Leaving it
    // invalidates the request, so an older result cannot replace another body.
    return () => { current = false; };
  }, [source.id]);

  const path = result?.ok ? result.path : source.locator;
  const heading = result?.ok ? result.heading : source.section;
  return (
    <div className="record-detail-body">
      <p className="record-detail-meta"><span className="record-path">{path}</span><span>{heading ?? '파일 처음부터'}</span></p>
      {result === null ? <p role="status">원문 구간을 읽는 중입니다.</p> : result.ok ? (
        <pre className="record-section-text" aria-label="원문 구간">{result.text}</pre>
      ) : (
        <p className="record-missing" role="alert">끊긴 링크: {result.message}</p>
      )}
    </div>
  );
}
