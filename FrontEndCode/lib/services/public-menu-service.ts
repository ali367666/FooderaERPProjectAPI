import { api } from "@/lib/api";

export type PublicMenuItem = {
  id: number;
  name: string;
  description: string | null;
  imageUrl: string | null;
  price: number;
  portion: string | null;
  isAvailable: boolean;
};

export type PublicMenuCategory = {
  id: number;
  name: string;
  imageUrl: string | null;
  items: PublicMenuItem[];
};

export type PublicMenu = {
  restaurantId: number;
  restaurantName: string;
  logoUrl: string | null;
  slogan: string | null;
  productColor: string | null;
  contactPhoneNumber: string | null;
  socialLinks: string | null;
  categories: PublicMenuCategory[];
};

function pick<T>(o: Record<string, unknown>, camel: string, pascal: string): T | undefined {
  if (o[camel] !== undefined) return o[camel] as T;
  if (o[pascal] !== undefined) return o[pascal] as T;
  return undefined;
}

function normalizeItem(raw: Record<string, unknown>): PublicMenuItem {
  return {
    id: Number(pick(raw, "id", "Id") ?? 0),
    name: String(pick(raw, "name", "Name") ?? ""),
    description: (pick(raw, "description", "Description") as string | null | undefined) ?? null,
    imageUrl: (pick(raw, "imageUrl", "ImageUrl") as string | null | undefined) ?? null,
    price: Number(pick(raw, "price", "Price") ?? 0),
    portion: (pick(raw, "portion", "Portion") as string | null | undefined) ?? null,
    isAvailable: Boolean(pick(raw, "isAvailable", "IsAvailable") ?? true),
  };
}

function normalizeCategory(raw: Record<string, unknown>): PublicMenuCategory {
  const items = pick<unknown[]>(raw, "items", "Items") ?? [];
  return {
    id: Number(pick(raw, "id", "Id") ?? 0),
    name: String(pick(raw, "name", "Name") ?? ""),
    imageUrl: (pick(raw, "imageUrl", "ImageUrl") as string | null | undefined) ?? null,
    items: items.map((x) => normalizeItem(x as Record<string, unknown>)),
  };
}

export async function getPublicMenu(restaurantId: number): Promise<PublicMenu> {
  const response = await api.get<unknown>(`/public-menu/${restaurantId}`);
  const raw = response.data as Record<string, unknown>;
  const categories = pick<unknown[]>(raw, "categories", "Categories") ?? [];
  return {
    restaurantId: Number(pick(raw, "restaurantId", "RestaurantId") ?? restaurantId),
    restaurantName: String(pick(raw, "restaurantName", "RestaurantName") ?? ""),
    logoUrl: (pick(raw, "logoUrl", "LogoUrl") as string | null | undefined) ?? null,
    slogan: (pick(raw, "slogan", "Slogan") as string | null | undefined) ?? null,
    productColor: (pick(raw, "productColor", "ProductColor") as string | null | undefined) ?? null,
    contactPhoneNumber: (pick(raw, "contactPhoneNumber", "ContactPhoneNumber") as string | null | undefined) ?? null,
    socialLinks: (pick(raw, "socialLinks", "SocialLinks") as string | null | undefined) ?? null,
    categories: categories.map((x) => normalizeCategory(x as Record<string, unknown>)),
  };
}
