// What a barcode can be before it's worth a lookup: digits only, in one of the standard lengths
// (EAN-8, UPC-A, EAN-13, GTIN-14). Deliberately no checksum - a typo that happens to keep the length
// is cheap to find out from the lookup itself.
export function eanError(value: string): string | undefined {
  if (!value) return undefined; // nothing typed yet: not an error
  if (!/^\d+$/.test(value)) return "A barcode contains only digits";
  if (![8, 12, 13, 14].includes(value.length)) return "A barcode has 8, 12, 13 or 14 digits";
  return undefined;
}
