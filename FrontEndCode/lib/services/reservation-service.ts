import { api } from "@/lib/api";
import { toApiFormError } from "@/lib/api-error";

export type ReservationStatus =
  | "Pending" | "Confirmed" | "Seated" | "Completed" | "Cancelled" | "NoShow";

export type BanquetTableRef = {
  id: number;
  name: string;
  capacity: number;
  sectionId: number | null;
};

export type ReservationDto = {
  id: number;
  restaurantId: number;
  restaurantName: string;
  tableId: number | null;
  tableName: string | null;
  guestName: string;
  guestPhone: string;
  guestEmail: string | null;
  guestCount: number;
  reservationDate: string;   // ISO date
  reservationTime: string;   // "HH:mm"
  durationMinutes: number;
  status: ReservationStatus;
  note: string | null;
  confirmedAt: string | null;
  createdAtUtc: string;
  /** A banquet booking: several tables of a hall held together. */
  isBanquet?: boolean;
  banquetTables?: BanquetTableRef[];
};

/** A hall table as the banquet screen lists it: free to pick, or busy with the reason why not. */
export type BanquetTableOption = {
  id: number;
  name: string;
  capacity: number;
  sectionId: number | null;
  sectionName: string | null;
  isAvailable: boolean;
  busyReason: string | null;
};

export type CreateBanquetReservationInput = {
  restaurantId: number;
  tableIds: number[];
  guestName: string;
  guestPhone: string;
  guestCount: number;
  reservationDate: string;
  reservationTime: string;
  durationMinutes?: number;
  note?: string | null;
};

export type CreateReservationInput = {
  restaurantId: number;
  tableId?: number | null;
  guestName: string;
  guestPhone: string;
  guestEmail?: string | null;
  guestCount: number;
  reservationDate: string;
  reservationTime: string;
  durationMinutes?: number;
  note?: string | null;
};

export async function getReservations(date?: string): Promise<ReservationDto[]> {
  try {
    const params = date ? { date } : {};
    const res = await api.get<ReservationDto[]>("/Reservations", { params });
    return Array.isArray(res.data) ? res.data : [];
  } catch (e) {
    throw toApiFormError(e, "Rezervasiyalar yüklənmədi");
  }
}

export async function getBanquetTables(params: {
  restaurantId: number;
  sectionId?: number;
  date?: string;
  time?: string;
  durationMinutes?: number;
}): Promise<BanquetTableOption[]> {
  try {
    const res = await api.get<BanquetTableOption[]>("/Banquets/tables", { params });
    return Array.isArray(res.data) ? res.data : [];
  } catch (e) {
    throw toApiFormError(e, "Masalar yüklənmədi");
  }
}

export async function createBanquetReservation(data: CreateBanquetReservationInput): Promise<ReservationDto> {
  try {
    const res = await api.post<ReservationDto>("/Banquets/reservations", data);
    return res.data;
  } catch (e) {
    throw toApiFormError(e, "Banket rezervi yaradılmadı");
  }
}

export async function createReservation(data: CreateReservationInput): Promise<ReservationDto> {
  try {
    const res = await api.post<ReservationDto>("/Reservations", data);
    return res.data;
  } catch (e) {
    throw toApiFormError(e, "Rezervasiya yaradılmadı");
  }
}

export async function updateReservation(id: number, data: Partial<CreateReservationInput>): Promise<ReservationDto> {
  try {
    const res = await api.put<ReservationDto>(`/Reservations/${id}`, data);
    return res.data;
  } catch (e) {
    throw toApiFormError(e, "Rezervasiya yenilənmədi");
  }
}

export async function changeReservationStatus(
  id: number,
  action: "confirm" | "seat" | "complete" | "cancel" | "noshow",
): Promise<ReservationDto> {
  try {
    const res = await api.post<ReservationDto>(`/Reservations/${id}/status`, null, {
      params: { action },
    });
    return res.data;
  } catch (e) {
    throw toApiFormError(e, "Status dəyişdirilmədi");
  }
}

/** Holds the table longer: adds minutes to the reservation's duration. */
export async function extendReservation(id: number, minutes: number): Promise<ReservationDto> {
  try {
    const res = await api.post<ReservationDto>(`/Reservations/${id}/extend`, null, { params: { minutes } });
    return res.data;
  } catch (e) {
    throw toApiFormError(e, "Rezervasiyanın müddəti artırılmadı");
  }
}

/** Moves the reservation to another table of the same branch. */
export async function moveReservation(id: number, tableId: number): Promise<ReservationDto> {
  try {
    const res = await api.post<ReservationDto>(`/Reservations/${id}/move`, null, { params: { tableId } });
    return res.data;
  } catch (e) {
    throw toApiFormError(e, "Rezervasiya başqa masaya keçirilmədi");
  }
}

export async function deleteReservation(id: number): Promise<void> {
  try {
    await api.delete(`/Reservations/${id}`);
  } catch (e) {
    throw toApiFormError(e, "Rezervasiya silinmədi");
  }
}
