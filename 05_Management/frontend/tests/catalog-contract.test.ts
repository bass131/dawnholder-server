// @vitest-environment node
// Requirement: goal 「만들 것」 1 (catalog keeps only names, kinds, links and locations) and
// index-v2-design.md 「색인 형식」. readCatalog accepts schemaVersion 2 only and rejects any key
// outside the fixed set, so removed narrative/status fields cannot come back. Locator shape and
// path safety are judged per source later (원문 읽기 경계·색인 검사), not by readCatalog.
// Reference breaks are checked through the store in records-store.test.ts (Astra msg_28ce06a18c04 Q7).
import { describe, expect, it } from 'vitest';
import { readCatalog } from '../electron/catalog-contract';
import {
  gitSource, handoffSource, linkedCatalog, localSource, recordFixture, systemFixture, MERGE_COMMIT, type CatalogFixture,
} from './record-sources/catalog-v2-fixture';

function withChange(change: (catalog: CatalogFixture & Record<string, unknown>) => void): unknown {
  const catalog = structuredClone(linkedCatalog()) as CatalogFixture & Record<string, unknown>;
  change(catalog);
  return catalog;
}

// A rejection only means something when the unchanged linked index is accepted, so both are
// asserted together; otherwise an implementation that rejects everything would pass.
function expectRejectedAgainstAcceptedBaseline(catalog: unknown, label?: string) {
  const baselineAccepted = readCatalog(linkedCatalog()) !== null;
  const changedRejected = readCatalog(catalog) === null;
  expect({ baselineAccepted, changedRejected }, label).toEqual({ baselineAccepted: true, changedRejected: true });
}

function firstSource(catalog: CatalogFixture): Record<string, unknown> {
  return catalog.sources[0] as unknown as Record<string, unknown>;
}
function firstSystem(catalog: CatalogFixture): Record<string, unknown> {
  return catalog.systems[0] as unknown as Record<string, unknown>;
}
function firstRecord(catalog: CatalogFixture): Record<string, unknown> {
  return catalog.records[0] as unknown as Record<string, unknown>;
}

describe('record index v2 acceptance', () => {
  it('accepts a linked v2 index and returns its content unchanged', () => {
    const catalog = linkedCatalog();
    expect(readCatalog(catalog)).toEqual(catalog);
  });

  it('accepts locators whose shape the per-source reader and the index check judge later', () => {
    const catalog = withChange(value => {
      value.sources.push(
        gitSource('parent-path', '../outside.md', null),
        gitSource('drive-path', 'C:/outside.md', null),
        gitSource('absolute-path', '/outside.md', null),
        gitSource('backslash-path', 'docs\\guide.md', null),
        gitSource('code-file-with-section', 'src/Program.cs', 'Main'),
        localSource('old-absolute-local', 'C:/Dev/DawnHolder_Project/.backups/a.md'),
        handoffSource('not-a-message-id', 'message:msg_0123456789ab'),
      );
    });
    const accepted = readCatalog(catalog);
    expect(accepted).not.toBeNull();
  });

  it('accepts an empty PR list, a null section and the same PR number in two different records', () => {
    const catalog = withChange(value => {
      value.sources.push(gitSource('whole-file', 'docs/whole.md', null));
      value.records.push(
        recordFixture({ id: 'plan-without-pr', type: '계획', pullRequests: [] }),
        recordFixture({ id: 'same-pr-elsewhere', type: '검증', pullRequests: [{ number: 196, mergeCommit: MERGE_COMMIT }] }),
      );
    });
    expect(readCatalog(catalog)).not.toBeNull();
  });

  it('accepts a 128-character ID and the same ID used by a source and a system', () => {
    const longId = 'a'.repeat(128);
    const catalog = withChange(value => {
      value.sources.push(gitSource(longId, 'docs/long.md', null), gitSource('shared-name', 'docs/shared.md', null));
      value.systems.push(systemFixture({ id: 'shared-name' }));
    });
    expect(readCatalog(catalog)).not.toBeNull();
  });

  it('accepts locators of exactly 512 characters', () => {
    const catalog = withChange(value => {
      value.sources.push(gitSource('long-locator', `${'d/'.repeat(253)}aaa.md`, null));
    });
    expect((catalog as CatalogFixture).sources.at(-1)?.locator).toHaveLength(512);
    expect(readCatalog(catalog)).not.toBeNull();
  });
});

describe('record index v2 rejection of the old version and of keys outside the fixed set', () => {
  it('rejects schemaVersion 1 and a missing or other version', () => {
    for (const schemaVersion of [1, 3, '2', undefined]) {
      expectRejectedAgainstAcceptedBaseline({ ...linkedCatalog(), schemaVersion }, String(schemaVersion));
    }
  });

  it('rejects a complete v1-shaped catalog', () => {
    const v1 = {
      schemaVersion: 1, revision: 'r1', asOf: '2026-10-01', sourceCommit: MERGE_COMMIT, scopeNote: '범위',
      sources: [{ id: 's', title: 't', kind: 'git', locator: 'a.md', revision: MERGE_COMMIT, section: 'x', availability: 'versioned', note: 'n' }],
      systems: [], records: [],
    };
    expectRejectedAgainstAcceptedBaseline(v1);
  });

  // goal 「만들 것」 1 뺄 필드, one at a time.
  const removedFields: [string, string, (catalog: CatalogFixture) => Record<string, unknown>, unknown][] = [
    ['top level', 'scopeNote', catalog => catalog as unknown as Record<string, unknown>, '범위 메모'],
    ['top level', 'revision', catalog => catalog as unknown as Record<string, unknown>, 'manual-r1'],
    ['top level', 'asOf', catalog => catalog as unknown as Record<string, unknown>, '2026-10-06'],
    ['top level', 'sourceCommit', catalog => catalog as unknown as Record<string, unknown>, MERGE_COMMIT],
    ['system', 'summary', firstSystem, '요약'],
    ['system', 'responsibility', firstSystem, '책임'],
    ['system', 'behavior', firstSystem, ['동작']],
    ['system', 'implementationStatus', firstSystem, '구현 상태'],
    ['system', 'integrationStatus', firstSystem, '통합 상태'],
    ['system', 'verificationStatus', firstSystem, '검증 상태'],
    ['system', 'limitations', firstSystem, ['한계']],
    ['system', 'nextSteps', firstSystem, ['다음 일']],
    ['record', 'summary', firstRecord, '요약'],
    ['record', 'reason', firstRecord, '이유'],
    ['record', 'status', firstRecord, '상태'],
    ['record', 'details', firstRecord, ['상세']],
    ['record', 'limitations', firstRecord, ['한계']],
    ['record', 'nextSteps', firstRecord, ['다음 일']],
    ['source', 'note', firstSource, '메모'],
    ['source', 'revision', firstSource, MERGE_COMMIT],
  ];
  it.each(removedFields)('rejects the removed %s field %s', (_owner, key, target, removedValue) => {
    const catalog = withChange(value => { target(value)[key] = removedValue; });
    expectRejectedAgainstAcceptedBaseline(catalog);
  });

  it('rejects any other key outside the fixed set on every object', () => {
    const extraKeys: [string, (catalog: CatalogFixture) => Record<string, unknown>][] = [
      ['top level', catalog => catalog as unknown as Record<string, unknown>],
      ['source', firstSource],
      ['system', firstSystem],
      ['record', firstRecord],
      ['pull request', catalog => catalog.records[0]!.pullRequests[0] as unknown as Record<string, unknown>],
    ];
    for (const [owner, target] of extraKeys) {
      const catalog = withChange(value => { target(value).extra = 'x'; });
      expectRejectedAgainstAcceptedBaseline(catalog, owner);
    }
  });
});

describe('record index v2 field rules', () => {
  it('requires kind and availability to pair: git is versioned, local and handoff are local-only', () => {
    const pairs: [string, string][] = [['git', 'local-only'], ['local', 'versioned'], ['handoff', 'versioned'], ['url', 'versioned']];
    for (const [kind, availability] of pairs) {
      const catalog = withChange(value => { Object.assign(firstSource(value), { kind, availability }); });
      expectRejectedAgainstAcceptedBaseline(catalog, `${kind}/${availability}`);
    }
  });

  it('rejects ID shapes outside lowercase words joined by single hyphens, up to 128 characters', () => {
    const badIds = ['Upper', 'under_score', 'a--b', '-a', 'a-', 'a.b', 'a b', '', 'a'.repeat(129), 7];
    for (const id of badIds) {
      const asSource = withChange(value => { value.sources.push({ ...gitSource('x', 'docs/x.md', null), id: id as string }); });
      const asSystem = withChange(value => { value.systems.push({ ...systemFixture({ id: 'x' }), id: id as string }); });
      const asRecord = withChange(value => { value.records.push({ ...recordFixture({ id: 'x' }), id: id as string }); });
      expectRejectedAgainstAcceptedBaseline(asSource, `source ${String(id)}`);
      expectRejectedAgainstAcceptedBaseline(asSystem, `system ${String(id)}`);
      expectRejectedAgainstAcceptedBaseline(asRecord, `record ${String(id)}`);
    }
  });

  it('rejects a duplicate ID within one kind', () => {
    const duplicates = [
      withChange(value => { value.sources.push(gitSource('guide-overview', 'docs/other.md', null)); }),
      withChange(value => { value.systems.push(systemFixture({ id: 'records-view' })); }),
      withChange(value => { value.records.push(recordFixture({ id: 'index-change' })); }),
    ];
    for (const catalog of duplicates) expectRejectedAgainstAcceptedBaseline(catalog);
  });

  it('rejects empty, padded or non-string titles and areas', () => {
    for (const bad of ['', '   ', ' 앞 공백', '뒤 공백 ', 3, null]) {
      const titles = [
        withChange(value => { firstSource(value).title = bad; }),
        withChange(value => { firstSystem(value).title = bad; }),
        withChange(value => { firstRecord(value).title = bad; }),
      ];
      for (const catalog of titles) expectRejectedAgainstAcceptedBaseline(catalog, `title ${String(bad)}`);
      expectRejectedAgainstAcceptedBaseline(withChange(value => { firstSystem(value).area = bad; }), `area ${String(bad)}`);
    }
  });

  it('rejects a record type outside 변경·결정·검증·계획', () => {
    expectRejectedAgainstAcceptedBaseline(withChange(value => { firstRecord(value).type = '기타'; }));
  });

  it('requires PR numbers to be positive integers and merge commits to be 40 lowercase hex', () => {
    const badNumbers = [0, -1, 1.5, '196', null];
    for (const number of badNumbers) {
      const catalog = withChange(value => { Object.assign(value.records[0]!.pullRequests[0]!, { number }); });
      expectRejectedAgainstAcceptedBaseline(catalog, `number ${String(number)}`);
    }
    const badCommits = [MERGE_COMMIT.toUpperCase(), MERGE_COMMIT.slice(1), `${MERGE_COMMIT}a`, 'g'.repeat(40), ''];
    for (const mergeCommit of badCommits) {
      const catalog = withChange(value => { Object.assign(value.records[0]!.pullRequests[0]!, { mergeCommit }); });
      expectRejectedAgainstAcceptedBaseline(catalog, `mergeCommit ${mergeCommit}`);
    }
  });

  it('rejects a PR number repeated inside one record and a missing PR list', () => {
    const repeated = withChange(value => {
      value.records[0]!.pullRequests.push({ number: 196, mergeCommit: 'b'.repeat(40) });
    });
    expectRejectedAgainstAcceptedBaseline(repeated);
    expectRejectedAgainstAcceptedBaseline(withChange(value => { delete firstRecord(value).pullRequests; }));
    expectRejectedAgainstAcceptedBaseline(withChange(value => { firstRecord(value).pullRequests = {}; }));
  });

  it('rejects empty, over-512-character and control-character locators', () => {
    const badLocators = ['', `${'d/'.repeat(253)}aaaa.md`, 'docs/a\u0000.md', 'docs/a\n.md', 'docs/a\t.md', 'docs/a\u001f.md', 'docs/a\u007f.md', 5];
    for (const locator of badLocators) {
      const catalog = withChange(value => { firstSource(value).locator = locator; });
      expectRejectedAgainstAcceptedBaseline(catalog, JSON.stringify(locator));
    }
  });

  it('requires section to be present as a string or null', () => {
    for (const section of [1, [], {}]) {
      expectRejectedAgainstAcceptedBaseline(withChange(value => { firstSource(value).section = section; }), JSON.stringify(section));
    }
    expectRejectedAgainstAcceptedBaseline(withChange(value => { delete firstSource(value).section; }));
  });

  it('requires reference lists to be arrays of strings', () => {
    const badLists = [
      withChange(value => { firstSystem(value).sourceIds = 'guide-overview'; }),
      withChange(value => { firstSystem(value).relatedSystemIds = [1]; }),
      withChange(value => { firstSystem(value).recordIds = null; }),
      withChange(value => { firstRecord(value).systemIds = {}; }),
      withChange(value => { firstRecord(value).sourceIds = [null]; }),
    ];
    for (const catalog of badLists) expectRejectedAgainstAcceptedBaseline(catalog);
  });

  it('rejects non-object input', () => {
    for (const value of [null, [], 'catalog', 2]) expectRejectedAgainstAcceptedBaseline(value);
  });
});
