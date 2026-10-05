import { INSPECTION_STYLE as inspectionStyle, svgDocumentError, svgInputError } from './svg-contract';
import { inspectionSvg } from './svg-inspection';
export { svgCssError } from './svg-contract';

function inspectionDocument(svg: string): Document | null {
  // Chromium applies CSP to SVG styles even in a detached DOMParser document.
  // Rename only style names in the inspection copy; validate their original values
  // below and approve the untouched string. Reserved names cannot come from input.
  if (svg.includes(inspectionStyle)) return null;
  const inert = inspectionSvg(svg, inspectionStyle);
  return inert === null ? null : new DOMParser().parseFromString(inert, 'image/svg+xml');
}

export function svgError(svg: string): string | null {
  const inputError = svgInputError(svg);
  if (inputError) return inputError;
  const doc = inspectionDocument(svg);
  return doc ? svgDocumentError(doc, { inspection: true }) : '유효한 SVG 문서가 아닙니다.';
}
