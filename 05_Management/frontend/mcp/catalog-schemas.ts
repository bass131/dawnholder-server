import { z } from 'zod';
import { RECORD_TYPES, SOURCE_KINDS, SOURCE_AVAILABILITY } from '../electron/catalog-contract.js';
import type { DevelopmentRecord, RecordSource, SystemRecord } from '../electron/catalog-contract.js';
import type { CheckoutInfo } from '../electron/checkout-contract.js';
import { MAX_SOURCE_SECTION_BYTES } from '../electron/source-section-contract.js';
import type { ImplementationDocument, SystemCard } from '../electron/system-guide-contract.js';
import { ERROR_MESSAGES } from './catalog-errors.js';

// Default Zod strict-object errors echo arbitrary property names. Keep every input
// error schema-local and fixed so the SDK's pre-handler refusals stay bounded.
const inputError = 'Invalid tool arguments.';
const textInput = (max: number) => z.string({ error: inputError }).max(max, { error: inputError });
const idInput = () => textInput(128).min(1, { error: inputError });
const hashInput = () => z.string({ error: inputError }).regex(/^[a-f0-9]{64}$/, { error: inputError }).optional();
const offsetInput = (max: number) => z.number({ error: inputError })
  .int({ error: inputError })
  .min(0, { error: inputError })
  .max(max, { error: inputError })
  .optional();
const pagingInput = {
  query: textInput(256).optional(),
  limit: z.number({ error: inputError }).int({ error: inputError }).min(1, { error: inputError }).max(50, { error: inputError }).optional(),
  offset: offsetInput(100000),
  expectedHash: hashInput(),
};
const detailInput = z.strictObject({ id: idInput(), expectedHash: hashInput() }, { error: inputError });
export const inputSchemas = {
  list_systems: z.strictObject({ ...pagingInput, area: textInput(128).optional() }, { error: inputError }),
  search_records: z.strictObject({
    ...pagingInput,
    area: textInput(128).optional(),
    type: z.enum(RECORD_TYPES, { error: inputError }).optional(),
    systemId: idInput().optional(),
  }, { error: inputError }),
  get_system: detailInput,
  get_record: detailInput,
  get_source: detailInput,
  read_source_section: z.strictObject({
    id: idInput(),
    offset: offsetInput(MAX_SOURCE_SECTION_BYTES),
    expectedSectionHash: hashInput(),
    expectedHash: hashInput(),
  }, { error: inputError }),
  list_guide_cards: z.strictObject(pagingInput, { error: inputError }),
  get_guide_card: detailInput,
};
export type CatalogToolName = keyof typeof inputSchemas;
export type GuideToolName = 'list_guide_cards' | 'get_guide_card';
export type RecordToolName = Exclude<CatalogToolName, GuideToolName | 'read_source_section'>;
export interface ToolArguments {
  query?: string | undefined;
  area?: string | undefined;
  type?: string | undefined;
  systemId?: string | undefined;
  id?: string | undefined;
  limit?: number | undefined;
  offset?: number | undefined;
  expectedHash?: string | undefined;
  expectedSectionHash?: string | undefined;
}

const text = z.string();
const strings = z.array(text);
const recordType = z.enum(RECORD_TYPES);
const hash = z.string().regex(/^[a-f0-9]{64}$/);
const snapshot = z.strictObject({ hash });
const system = z.strictObject({
  id: text,
  title: text,
  area: text,
  sourceIds: strings,
  relatedSystemIds: strings,
  recordIds: strings,
}) satisfies z.ZodType<SystemRecord>;
const record = z.strictObject({
  id: text,
  type: recordType,
  title: text,
  systemIds: strings,
  sourceIds: strings,
  pullRequests: z.array(z.strictObject({
    number: z.number().int().positive(),
    mergeCommit: z.string().regex(/^[a-f0-9]{40}$/),
  })),
}) satisfies z.ZodType<DevelopmentRecord>;
const source = z.strictObject({
  id: text,
  title: text,
  kind: z.enum(SOURCE_KINDS),
  locator: text,
  section: text.nullable(),
  availability: z.enum(SOURCE_AVAILABILITY),
}) satisfies z.ZodType<RecordSource>;
const paging = z.strictObject({
  total: z.number().int().nonnegative(), offset: z.number().int().nonnegative(),
  limit: z.number().int().min(1).max(50), returned: z.number().int().nonnegative(),
  nextOffset: z.number().int().nonnegative().nullable(),
});
const systemPreview = z.strictObject({
  id: text, title: text, area: text,
  lookupSupported: z.boolean(), truncatedFields: strings,
});
const recordPreview = z.strictObject({
  id: text, type: recordType, title: text,
  systemIds: strings, lookupSupported: z.boolean(), truncatedFields: strings,
});
const codeMappingFields = {
  path: text,
  kind: z.enum(['file', 'directory']),
  role: text,
};
const codeMapping = z.union([
  z.strictObject({ ...codeMappingFields, namespace: text }),
  z.strictObject(codeMappingFields),
]);
const guideCardFields = {
  id: text,
  title: text,
  summary: text,
  relatedSystemIds: strings,
};
// Keep omitted fields omitted, matching the app contract and exact optional
// property types rather than widening them to explicitly present undefined.
const guideCard = z.union([
  z.strictObject({ ...guideCardFields, parentId: z.null() }),
  z.strictObject({
    ...guideCardFields,
    parentId: text,
    status: z.literal('code-present'),
    documentId: text,
    codeReference: z.strictObject({ commitSha: text, mappings: z.array(codeMapping) }),
  }),
]) satisfies z.ZodType<SystemCard>;
const guideDocument = z.strictObject({
  id: text,
  cardId: text,
  title: text,
  sourceCommit: text,
  sourceRefs: z.array(z.strictObject({ path: text, symbol: text })),
  sections: z.array(z.strictObject({
    id: text,
    title: text,
    blocks: z.array(z.discriminatedUnion('type', [
      z.strictObject({ type: z.literal('paragraph'), text }),
      z.strictObject({
        type: z.literal('diagram'),
        id: text,
        format: z.literal('mermaid'),
        source: text,
        title: text,
        description: text,
      }),
    ])),
  })),
}) satisfies z.ZodType<ImplementationDocument>;
const guideCardPreview = z.strictObject({
  id: text,
  parentId: text.nullable(),
  title: text,
  relatedSystemIds: strings,
  documentId: text.nullable(),
  lookupSupported: z.boolean(),
  truncatedFields: strings,
});
const checkout = z.discriminatedUnion('state', [
  z.strictObject({
    state: z.literal('known'),
    branch: text.nullable(),
    head: text,
  }),
  z.strictObject({
    state: z.literal('unknown'),
    reason: z.enum([
      'no-git', 'link', 'pointer-invalid', 'backlink-mismatch', 'head-invalid',
      'ref-invalid', 'ref-missing', 'too-large', 'load',
    ]),
  }),
]) satisfies z.ZodType<CheckoutInfo>;
const sectionPaging = z.strictObject({
  offset: z.number().int().nonnegative(),
  returned: z.number().int().nonnegative(),
  total: z.number().int().nonnegative(),
  nextOffset: z.number().int().nonnegative().nullable(),
});
const errorCode = z.enum(Object.keys(ERROR_MESSAGES) as [keyof typeof ERROR_MESSAGES, ...(keyof typeof ERROR_MESSAGES)[]]);
const errorDetailValue = z.union([z.string(), z.number(), z.boolean(), z.array(z.string())]);
const errorDetails = z.record(z.string(), errorDetailValue);
const errorDescription = z.strictObject({
  code: errorCode, message: z.string().max(256), retryable: z.boolean(), details: errorDetails.optional(),
});
const error = z.strictObject({
  ok: z.literal(false), snapshot: snapshot.nullable(),
  error: errorDescription,
});
function output<T extends z.ZodType>(data: T) {
  return z.union([z.strictObject({ ok: z.literal(true), snapshot, data }), error]);
}
export const outputSchemas = {
  list_systems: output(z.strictObject({ items: z.array(systemPreview), paging })),
  search_records: output(z.strictObject({ items: z.array(recordPreview), paging })),
  get_system: output(z.strictObject({ system })),
  get_record: output(z.strictObject({ record })),
  get_source: output(z.strictObject({ source, evidenceRead: z.literal(false), availabilityVerified: z.literal(false) })),
  read_source_section: output(z.strictObject({
    source,
    path: text,
    heading: text.nullable(),
    text,
    sectionHash: hash,
    checkout,
    paging: sectionPaging,
  })),
  list_guide_cards: output(z.strictObject({ items: z.array(guideCardPreview), paging })),
  get_guide_card: output(z.strictObject({ card: guideCard, document: guideDocument.nullable() })),
} satisfies Record<CatalogToolName, z.ZodType>;
