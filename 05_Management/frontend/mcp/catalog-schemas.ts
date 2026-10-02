import { z } from 'zod';
import { RECORD_TYPES, SOURCE_KINDS, SOURCE_AVAILABILITY, type DevelopmentRecord, type RecordSource, type SystemRecord } from '../electron/catalog-contract.js';
import { ERROR_MESSAGES } from './catalog-errors.js';

// Default Zod strict-object errors echo arbitrary property names. Keep every input
// error schema-local and fixed so the SDK's pre-handler refusals stay bounded.
const inputError = 'Invalid tool arguments.';
const textInput = (max: number) => z.string({ error: inputError }).max(max, { error: inputError });
const idInput = () => textInput(128).min(1, { error: inputError });
const hashInput = () => z.string({ error: inputError }).regex(/^[a-f0-9]{64}$/, { error: inputError }).optional();
const pagingInput = {
  query: textInput(256).optional(), area: textInput(128).optional(),
  limit: z.number({ error: inputError }).int({ error: inputError }).min(1, { error: inputError }).max(50, { error: inputError }).optional(),
  offset: z.number({ error: inputError }).int({ error: inputError }).min(0, { error: inputError }).max(100000, { error: inputError }).optional(),
  expectedHash: hashInput(),
};
const detailInput = z.strictObject({ id: idInput(), expectedHash: hashInput() }, { error: inputError });
export const inputSchemas = {
  list_systems: z.strictObject(pagingInput, { error: inputError }),
  search_records: z.strictObject({ ...pagingInput, type: z.enum(RECORD_TYPES, { error: inputError }).optional(), systemId: idInput().optional() }, { error: inputError }),
  get_system: detailInput, get_record: detailInput, get_source: detailInput,
};
export type CatalogToolName = keyof typeof inputSchemas;
export interface ToolArguments {
  query?: string | undefined; area?: string | undefined; type?: string | undefined; systemId?: string | undefined; id?: string | undefined;
  limit?: number | undefined; offset?: number | undefined; expectedHash?: string | undefined;
}

const text = z.string();
const strings = z.array(text);
const recordType = z.enum(RECORD_TYPES);
const snapshot = z.strictObject({ hash: z.string().regex(/^[a-f0-9]{64}$/), revision: text, asOf: text, sourceCommit: text });
const system = z.strictObject({
  id: text, title: text, area: text, summary: text, responsibility: text, behavior: strings,
  implementationStatus: text, integrationStatus: text, verificationStatus: text,
  limitations: strings, nextSteps: strings, sourceIds: strings, relatedSystemIds: strings, recordIds: strings,
}) satisfies z.ZodType<SystemRecord>;
const record = z.strictObject({
  id: text, type: recordType, title: text, summary: text, reason: text, status: text,
  systemIds: strings, sourceIds: strings, details: strings, limitations: strings, nextSteps: strings,
}) satisfies z.ZodType<DevelopmentRecord>;
const source = z.strictObject({
  id: text, title: text, kind: z.enum(SOURCE_KINDS), locator: text,
  revision: text, section: text, availability: z.enum(SOURCE_AVAILABILITY), note: text,
}) satisfies z.ZodType<RecordSource>;
const paging = z.strictObject({
  total: z.number().int().nonnegative(), offset: z.number().int().nonnegative(),
  limit: z.number().int().min(1).max(50), returned: z.number().int().nonnegative(),
  nextOffset: z.number().int().nonnegative().nullable(),
});
const systemPreview = z.strictObject({
  id: text, title: text, area: text, summary: text,
  implementationStatus: text, integrationStatus: text, verificationStatus: text,
  lookupSupported: z.boolean(), truncatedFields: strings,
});
const recordPreview = z.strictObject({
  id: text, type: recordType, title: text, summary: text, status: text,
  systemIds: strings, lookupSupported: z.boolean(), truncatedFields: strings,
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
} satisfies Record<CatalogToolName, z.ZodType>;
