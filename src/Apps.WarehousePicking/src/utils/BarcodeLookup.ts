import { Lab_productsService } from "@/generated/services/Lab_productsService";
import type { Product } from "@/generated/models/OpenFoodFactsModel";
import { servicePayload, serviceResult } from "@/utils/ServicePayload";

// Finds the Product record for a barcode, creating it from the catalog data when there is none yet.
// No alternate key on EAN (see docs/design/lab-5-external-data-and-background-processing.md) - a plain filter-then-create
// instead of an upsert, so scanning the same barcode twice reuses the existing Product record rather
// than duplicating it.
export async function ensureProductRecord(ean: string, product: Product): Promise<string | undefined> {
  const existing = await serviceResult(Lab_productsService.getAll({
    select: ["lab_productid"],
    filter: `lab_ean eq '${ean}'`,
    top: 1,
  }));
  let productId = existing.data?.[0]?.lab_productid;

  if (!productId) {
    // createRecordAsync's response shape for the new record's id isn't reliable across hosts -
    // re-query by the same EAN filter used above instead of trusting the create call's own return value.
    await serviceResult(Lab_productsService.create(servicePayload({
      lab_name: product.name || ean,
      lab_ean: ean,
      lab_brand: product.brand,
      lab_quantity: product.quantity,
      lab_imageurl: product.imageUrl,
      lab_lastsyncedon: new Date().toISOString(),
    })));

    const created = await serviceResult(Lab_productsService.getAll({
      select: ["lab_productid"],
      filter: `lab_ean eq '${ean}'`,
      top: 1,
    }));
    productId = created.data?.[0]?.lab_productid;
  }

  return productId;
}
